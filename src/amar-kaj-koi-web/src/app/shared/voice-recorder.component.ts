import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  NgZone,
  OnDestroy,
  Output,
  inject,
  signal,
} from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { Subscription } from 'rxjs';
import { ApiService } from '../core/api.service';
import { OfflineVoiceService } from '../core/offline-voice.service';
import { ToastService } from '../core/toast.service';

/**
 * No browser's MediaRecorder can produce mp3, so the recorder asks for the best
 * container each engine actually supports: Opus in WebM on Chrome/Edge/Firefox
 * and Android, AAC in MP4 on Safari and iOS. Both play back everywhere the app
 * runs and stay far smaller than mp3 would over a factory mobile connection.
 */
const PREFERRED_MIME_TYPES = ['audio/webm;codecs=opus', 'audio/webm', 'audio/mp4'];

/** Extension for the container the recorder ended up using. */
const EXTENSIONS: Record<string, string> = {
  'audio/webm': '.webm',
  'audio/mp4': '.m4a',
  'audio/ogg': '.ogg',
  'audio/mpeg': '.mp3',
  'audio/wav': '.wav',
};

@Component({
  selector: 'app-voice-recorder',
  standalone: true,
  imports: [ButtonModule, TagModule],
  templateUrl: './voice-recorder.component.html',
  styleUrls: ['./voice-recorder.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VoiceRecorderComponent implements OnDestroy {
  @Input() public purpose: 'Target' | 'CommitmentVoice' | 'ExtendReason' = 'Target';
  @Output() public readonly uploadedChange = new EventEmitter<{
    voiceFileId: string;
    durationSecs: number;
  } | null>();

  /**
   * True while the recorder holds something the surrounding form must not be
   * submitted with: a recording still running, one under the FR-01 3 second
   * minimum, one still uploading, or one the server refused. Forms where the
   * voice is optional need this — a too-short take never produces a
   * voiceFileId, so without it the take would be silently dropped on save.
   */
  @Output() public readonly blockedChange = new EventEmitter<boolean>();

  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  public readonly offline = inject(OfflineVoiceService);

  // MediaRecorder, the interval tick and IndexedDB all call back outside Angular's
  // zone, so nothing here would schedule change detection on its own — zone.run
  // does that, and the signals below are what mark this OnPush view dirty.
  private readonly zone = inject(NgZone);

  private mediaRecorder: MediaRecorder | null = null;
  private chunks: Blob[] = [];
  private stream: MediaStream | null = null;
  private startTime = 0;
  private tickHandle: any;
  private readonly syncSub: Subscription;
  private pendingId: string | null = null;

  /** Container MediaRecorder actually produced, without the codecs parameter. */
  private recordedMime = 'audio/webm';

  public readonly recording = signal(false);
  public readonly elapsed = signal(0);
  public readonly recordedBlob = signal<Blob | null>(null);
  public readonly recordedUrl = signal<string | null>(null);
  public readonly uploading = signal(false);
  public readonly uploaded = signal(false);

  /** FR-38: set while this recording sits in the offline queue. */
  public readonly queued = signal(false);
  /** Set when the server refused the upload, so a retry can be offered. */
  public readonly failed = signal(false);

  constructor() {
    // When the queued recording finally reaches the server, adopt its
    // voiceFileId so the surrounding form can be submitted as normal.
    this.syncSub = this.offline.synced$.subscribe((s) => {
      if (!this.pendingId || s.pendingId !== this.pendingId) return;
      this.zone.run(() => {
        this.pendingId = null;
        this.queued.set(false);
        this.uploaded.set(true);
        this.uploadedChange.emit({ voiceFileId: s.voiceFileId, durationSecs: s.durationSecs });
        this.emitBlocked();
      });
    });
  }

  public ngOnDestroy(): void {
    this.syncSub.unsubscribe();
    clearInterval(this.tickHandle);
    this.stream?.getTracks().forEach((t) => t.stop());
  }

  public async start(): Promise<void> {
    try {
      this.stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const mimeType = PREFERRED_MIME_TYPES.find((t) => MediaRecorder.isTypeSupported(t));
      // With no match, let the engine pick its own default rather than throwing
      // NotSupportedError — onStop reads back whatever it chose either way.
      this.mediaRecorder = mimeType
        ? new MediaRecorder(this.stream, { mimeType })
        : new MediaRecorder(this.stream);
      this.chunks = [];
      this.mediaRecorder.ondataavailable = (e) => {
        if (e.data.size > 0) this.chunks.push(e.data);
      };
      this.mediaRecorder.onstop = () => this.onStop();
      this.mediaRecorder.start();
      this.recording.set(true);
      this.startTime = Date.now();
      this.elapsed.set(0);
      this.emitBlocked();
      this.tickHandle = setInterval(
        () => this.elapsed.set(Math.round((Date.now() - this.startTime) / 1000)),
        200,
      );
    } catch {
      this.toast.error(
        'Your browser did not allow microphone access. Enable it for this site in the address-bar permissions, then try recording again.',
        'Microphone blocked',
      );
    }
  }

  public stop(): void {
    if (!this.recording() || !this.mediaRecorder) return;
    this.mediaRecorder.stop();
    this.stream?.getTracks().forEach((t) => t.stop());
    clearInterval(this.tickHandle);
    this.recording.set(false);
  }

  private onStop(): void {
    // Runs from a MediaRecorder event, which zone.js does not patch — without
    // zone.run no change detection cycle would be scheduled at all.
    this.zone.run(() => {
      // Label the blob with what was recorded, not an assumption: on Safari the
      // chunks are AAC/MP4, and calling them webm would send the server a file
      // whose extension contradicts its bytes, so playback would fail later.
      this.recordedMime = (this.mediaRecorder?.mimeType || 'audio/webm').split(';')[0].trim();
      const blob = new Blob(this.chunks, { type: this.recordedMime });
      this.recordedBlob.set(blob);
      this.recordedUrl.set(URL.createObjectURL(blob));

      // FR-02 only gives the user Play, Re-record, Save as Draft and Post — storing
      // the audio is not a step they perform, so it happens as soon as recording ends.
      // FR-01: anything under 3 seconds is not offered to the server at all.
      if (this.elapsed() >= 3) this.upload();
      this.emitBlocked();
    });
  }

  public reset(): void {
    if (this.pendingId) void this.offline.discard(this.pendingId);
    this.pendingId = null;
    this.recordedBlob.set(null);
    this.recordedUrl.set(null);
    this.uploaded.set(false);
    this.queued.set(false);
    this.failed.set(false);
    this.elapsed.set(0);
    this.uploadedChange.emit(null);
    this.emitBlocked();
  }

  /** Nothing recorded is fine; a recording that cannot become a voiceFileId is not. */
  private emitBlocked(): void {
    const unusable = !!this.recordedBlob() && !this.uploaded() && !this.queued();
    this.blockedChange.emit(this.recording() || unusable);
  }

  public upload(): void {
    const blob = this.recordedBlob();
    if (!blob) return;
    this.failed.set(false);
    const fname = `voice-${Date.now()}${EXTENSIONS[this.recordedMime] ?? '.webm'}`;

    // FR-38: with no connection, park the recording locally instead of failing.
    if (!navigator.onLine) {
      void this.queueOffline(fname);
      return;
    }

    this.uploading.set(true);
    this.api.uploadVoice(blob, fname, this.purpose, this.elapsed()).subscribe({
      next: (resp) => {
        this.uploading.set(false);
        this.uploaded.set(true);
        this.uploadedChange.emit({
          voiceFileId: resp.voiceFileId,
          durationSecs: resp.durationSecs,
        });
        this.emitBlocked();
      },
      error: (err: { status?: number }) => {
        this.uploading.set(false);
        // status 0 means the request never reached the API — treat it as offline.
        if ((err?.status ?? 0) === 0) void this.queueOffline(fname);
        else {
          this.failed.set(true);
          this.emitBlocked();
        }
      },
    });
  }

  private async queueOffline(fileName: string): Promise<void> {
    const blob = this.recordedBlob();
    if (!blob) return;
    const id = await this.offline.enqueue(blob, fileName, this.purpose, this.elapsed());
    this.zone.run(() => {
      this.pendingId = id;
      this.queued.set(true);
      this.toast.info(
        `Your ${this.elapsed()}-second recording is stored on this device because there is no connection. It uploads by itself once you are back online, and you can submit the form after that.`,
        'Saved on this device',
      );
      this.emitBlocked();
    });
  }
}
