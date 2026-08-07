import {
  ChangeDetectionStrategy,
  Component,
  Input,
  OnChanges,
  OnDestroy,
  inject,
  signal,
} from '@angular/core';
import { ApiService } from '../core/api.service';

/**
 * Plays a stored voice file. The download endpoint requires the bearer token,
 * which only the HTTP interceptor adds, so the audio is fetched as a blob and
 * played from an object URL instead of being linked directly.
 */
@Component({
  selector: 'app-voice-player',
  standalone: true,
  imports: [],
  template: `
    @if (url()) {
      <audio [src]="url()" controls preload="metadata"></audio>
    }
    @if (loading()) {
      <span class="muted small"><i class="pi pi-spin pi-spinner"></i> Loading voice…</span>
    }
    @if (failed()) {
      <span class="muted small">Voice could not be loaded.</span>
    }
  `,
  styleUrls: ['./voice-player.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VoicePlayerComponent implements OnChanges, OnDestroy {
  @Input() public voiceFileId?: string | null;

  private readonly api = inject(ApiService);

  public readonly url = signal<string | null>(null);
  public readonly loading = signal(false);
  public readonly failed = signal(false);

  public ngOnChanges(): void {
    this.load();
  }

  public ngOnDestroy(): void {
    this.release();
  }

  private load(): void {
    this.release();
    this.failed.set(false);
    this.loading.set(false);

    const id = this.voiceFileId;
    if (!id) return;

    this.loading.set(true);
    this.api.voiceBlob(id).subscribe({
      next: (blob) => {
        // The input can change while a fetch is in flight; a stale response must
        // not overwrite the player that the newer one is loading into.
        if (this.voiceFileId !== id) return;
        this.loading.set(false);
        this.url.set(URL.createObjectURL(blob));
      },
      error: () => {
        if (this.voiceFileId !== id) return;
        this.loading.set(false);
        this.failed.set(true);
      },
    });
  }

  /** An object URL pins its blob in memory until it is revoked. */
  private release(): void {
    const current = this.url();
    if (current) {
      URL.revokeObjectURL(current);
      this.url.set(null);
    }
  }
}
