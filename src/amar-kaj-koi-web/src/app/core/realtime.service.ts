import { Injectable, NgZone, inject, signal } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

/**
 * Live link to the notifications hub.
 *
 * The server owns the number: every push carries the authoritative unread total
 * for the signed-in user, so the badge is never derived from local guesswork and
 * stays right across tabs.
 */
@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private readonly auth = inject(AuthService);
  private readonly zone = inject(NgZone);

  private connection: HubConnection | null = null;

  /** Unread notification total; drives the sidebar badge. */
  public readonly unreadCount = signal(0);

  /** Emits on every server push, so an open notification list can reload itself. */
  public readonly changed$ = new Subject<number>();

  public async start(): Promise<void> {
    if (this.connection || !this.auth.token()) {
      return;
    }

    const conn = new HubConnectionBuilder()
      .withUrl(`${environment.hubUrl}/notifications`, {
        // Read lazily: a reconnect after a re-login must pick up the newer token.
        accessTokenFactory: () => this.auth.token() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    conn.on('unreadCount', (count: number) => this.apply(count));

    // Pushes sent while the socket was down are simply missed, so re-ask on return.
    conn.onreconnected(() => void this.resync());

    this.connection = conn;

    try {
      await conn.start();
    } catch {
      // No live channel is a degraded state, not a broken app: the count still
      // loads over REST, it just stops updating on its own.
      this.connection = null;
    }
  }

  public async stop(): Promise<void> {
    const conn = this.connection;
    this.connection = null;
    this.apply(0);
    if (conn) {
      try {
        await conn.stop();
      } catch {
        /* already gone */
      }
    }
  }

  /** Seeds the badge before the socket is up, and after any REST call that changes it. */
  public setCount(count: number): void {
    this.apply(count);
  }

  private async resync(): Promise<void> {
    if (this.connection?.state !== HubConnectionState.Connected) return;
    try {
      this.apply(await this.connection.invoke<number>('GetUnreadCount'));
    } catch {
      /* next push will correct it */
    }
  }

  /**
   * SignalR callbacks arrive from a socket event. zone.js patches WebSocket, but
   * the connection can fall back to SSE or long polling, so the update is run
   * inside the zone explicitly rather than depending on which transport won.
   */
  private apply(count: number): void {
    this.zone.run(() => {
      this.unreadCount.set(count);
      this.changed$.next(count);
    });
  }
}