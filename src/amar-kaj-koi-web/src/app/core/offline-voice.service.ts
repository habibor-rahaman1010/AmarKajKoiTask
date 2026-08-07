import { Injectable, inject, signal } from '@angular/core';
import { Subject } from 'rxjs';
import { ApiService } from './api.service';
import { ToastService } from './toast.service';

/** A recording captured while the device had no connection (FR-38). */
export interface PendingVoice {
  id: string;
  blob: Blob;
  fileName: string;
  purpose: string;
  durationSecs: number;
  createdAt: number;
}

/** Emitted once a queued recording has reached the server. */
export interface SyncedVoice {
  pendingId: string;
  voiceFileId: string;
  durationSecs: number;
}

const DB_NAME = 'amar-kaj-koi-offline';
const DB_VERSION = 1;
const STORE = 'voiceQueue';
const RETRY_INTERVAL_MS = 60_000;

/**
 * FR-38: voice can be recorded with no connection. The audio blob is parked in
 * IndexedDB (which, unlike localStorage, stores binary) and pushed to the server
 * as soon as the browser reports it is online again. Callers listen on
 * {@link synced$} to learn the voiceFileId their queued recording ended up with.
 */
@Injectable({ providedIn: 'root' })
export class OfflineVoiceService {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);

  /** Number of recordings still waiting to reach the server. */
  public readonly pendingCount = signal(0);
  /** Mirrors navigator.onLine so templates can react to it. */
  public readonly online = signal(typeof navigator === 'undefined' ? true : navigator.onLine);
  /** Fires once per recording that finishes syncing. */
  public readonly synced$ = new Subject<SyncedVoice>();

  private db: Promise<IDBDatabase> | null = null;
  private syncing = false;

  constructor() {
    if (typeof window === 'undefined') return;

    window.addEventListener('online', () => {
      this.online.set(true);
      void this.sync();
    });
    window.addEventListener('offline', () => this.online.set(false));

    // A browser can report "online" while the API is still unreachable, so keep
    // retrying on a timer instead of trusting the online event alone.
    setInterval(() => {
      if (this.pendingCount() > 0) void this.sync();
    }, RETRY_INTERVAL_MS);

    void this.refreshCount().then(() => {
      if (this.pendingCount() > 0) void this.sync();
    });
  }

  /** Parks a recording locally and returns the id used to match the later sync. */
  public async enqueue(
    blob: Blob,
    fileName: string,
    purpose: string,
    durationSecs: number,
  ): Promise<string> {
    const item: PendingVoice = {
      id: `pv_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`,
      blob,
      fileName,
      purpose,
      durationSecs,
      createdAt: Date.now(),
    };
    await this.put(item);
    await this.refreshCount();
    return item.id;
  }

  /** Uploads everything in the queue, oldest first. Safe to call repeatedly. */
  public async sync(): Promise<void> {
    if (this.syncing) return;
    if (typeof navigator !== 'undefined' && !navigator.onLine) return;

    this.syncing = true;
    try {
      const items = (await this.all()).sort((a, b) => a.createdAt - b.createdAt);
      let uploaded = 0;

      for (const item of items) {
        const ok = await this.uploadOne(item);
        // A failure here means the server is still unreachable; leave the rest
        // queued rather than burning through them with the same error.
        if (!ok) break;
        uploaded++;
      }

      if (uploaded > 0) {
        this.toast.success(
          `${uploaded} recording${uploaded === 1 ? '' : 's'} saved on this device ${uploaded === 1 ? 'has' : 'have'} now reached the server. The forms waiting on them can be submitted.`,
          'Offline recordings synced',
        );
      }
    } finally {
      this.syncing = false;
      await this.refreshCount();
    }
  }

  public async discard(id: string): Promise<void> {
    await this.remove(id);
    await this.refreshCount();
  }

  public async list(): Promise<PendingVoice[]> {
    return (await this.all()).sort((a, b) => a.createdAt - b.createdAt);
  }

  private uploadOne(item: PendingVoice): Promise<boolean> {
    return new Promise<boolean>((resolve) => {
      this.api.uploadVoice(item.blob, item.fileName, item.purpose, item.durationSecs).subscribe({
        next: async (resp) => {
          await this.remove(item.id);
          this.synced$.next({
            pendingId: item.id,
            voiceFileId: resp.voiceFileId,
            durationSecs: resp.durationSecs,
          });
          resolve(true);
        },
        error: async (err: { status?: number }) => {
          // 4xx means the server rejected the recording itself (too short, bad
          // session). Retrying forever would keep the queue stuck, so drop it.
          const status = err?.status ?? 0;
          if (status >= 400 && status < 500 && status !== 401) {
            await this.remove(item.id);
            this.toast.error(
              `The recording "${item.fileName}" (${item.durationSecs}s) was refused by the server, so it has been removed from the queue. Please record it again.`,
              'Offline recording discarded',
            );
          }
          resolve(false);
        },
      });
    });
  }

  // ---- IndexedDB plumbing ----
  private openDb(): Promise<IDBDatabase> {
    if (!this.db) {
      this.db = new Promise<IDBDatabase>((resolve, reject) => {
        const req = indexedDB.open(DB_NAME, DB_VERSION);
        req.onupgradeneeded = () => {
          const db = req.result;
          if (!db.objectStoreNames.contains(STORE)) db.createObjectStore(STORE, { keyPath: 'id' });
        };
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
      });
    }
    return this.db;
  }

  private async put(item: PendingVoice): Promise<void> {
    const db = await this.openDb();
    await new Promise<void>((resolve, reject) => {
      const tx = db.transaction(STORE, 'readwrite');
      tx.objectStore(STORE).put(item);
      tx.oncomplete = () => resolve();
      tx.onerror = () => reject(tx.error);
    });
  }

  private async remove(id: string): Promise<void> {
    const db = await this.openDb();
    await new Promise<void>((resolve, reject) => {
      const tx = db.transaction(STORE, 'readwrite');
      tx.objectStore(STORE).delete(id);
      tx.oncomplete = () => resolve();
      tx.onerror = () => reject(tx.error);
    });
  }

  private async all(): Promise<PendingVoice[]> {
    const db = await this.openDb();
    return new Promise<PendingVoice[]>((resolve, reject) => {
      const tx = db.transaction(STORE, 'readonly');
      const req = tx.objectStore(STORE).getAll();
      req.onsuccess = () => resolve((req.result ?? []) as PendingVoice[]);
      req.onerror = () => reject(req.error);
    });
  }

  private async refreshCount(): Promise<void> {
    try {
      this.pendingCount.set((await this.all()).length);
    } catch {
      this.pendingCount.set(0);
    }
  }
}
