import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { BadgeModule } from 'primeng/badge';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { Subscription } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { NotificationDto } from '../../core/models';
import { RealtimeService } from '../../core/realtime.service';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    CardModule,
    ButtonModule,
    BadgeModule,
    PaginatorModule,
  ],
  templateUrl: './notifications.component.html',
  styleUrls: ['./notifications.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly realtime = inject(RealtimeService);
  private sub?: Subscription;

  // Signals rather than plain fields: both are written from a subscribe callback,
  // which under OnPush would otherwise never mark this view for re-render.
  public readonly notifications = signal<NotificationDto[]>([]);
  public readonly loading = signal(true);

  // Client-side paging: the API hands back the whole list in one go.
  public readonly first = signal(0);
  public readonly rows = signal(10);
  public readonly pagedNotifications = computed(() =>
    this.notifications().slice(this.first(), this.first() + this.rows()),
  );

  public ngOnInit(): void {
    this.load();
    // Anything the server pushes while this page is open means the list changed too.
    this.sub = this.realtime.changed$.subscribe(() => this.load());
  }

  public ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  public load(): void {
    this.loading.set(true);
    this.api.notifications(false).subscribe((n) => {
      this.notifications.set(n);
      // A realtime refresh can shrink the list out from under the current page.
      if (this.first() >= n.length) {
        this.first.set(0);
      }
      this.loading.set(false);
    });
  }

  public onPage(e: PaginatorState): void {
    this.first.set(e.first ?? 0);
    this.rows.set(e.rows ?? 10);
  }

  public mark(id: string): void {
    this.api.markNotifRead(id).subscribe(() => this.load());
  }

  public markAll(): void {
    this.api.markAllNotifRead().subscribe(() => this.load());
  }
}
