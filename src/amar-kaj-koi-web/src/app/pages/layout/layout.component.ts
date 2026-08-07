import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { AvatarModule } from 'primeng/avatar';
import { BadgeModule } from 'primeng/badge';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { MenuModule } from 'primeng/menu';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { filter } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { OfflineVoiceService } from '../../core/offline-voice.service';
import { RealtimeService } from '../../core/realtime.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    MenuModule,
    AvatarModule,
    TagModule,
    ButtonModule,
    TooltipModule,
    DrawerModule,
    BadgeModule,
  ],
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LayoutComponent implements OnInit, OnDestroy {
  // Public: the template reads user() and role() off these directly.
  public readonly auth = inject(AuthService);
  /** FR-38: drives the "recordings waiting to sync" strip. */
  public readonly offline = inject(OfflineVoiceService);
  public readonly realtime = inject(RealtimeService);

  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  /** A signal because router events close the drawer from outside any template event. */
  public readonly mobileMenuOpen = signal(false);

  private readonly mainScroll = viewChild<ElementRef<HTMLElement>>('mainScroll');

  constructor() {
    this.router.events.pipe(filter((e) => e instanceof NavigationEnd)).subscribe(() => {
      // Close drawer whenever navigation happens
      this.mobileMenuOpen.set(false);
      // The router's scrollPositionRestoration moves the window, but content scrolls
      // inside <main> now, so a new page would otherwise open mid-scroll.
      const el = this.mainScroll()?.nativeElement;
      if (el) el.scrollTop = 0;
    });
  }

  public ngOnInit(): void {
    // The layout only exists behind the auth guard, so this is the natural place to
    // open and close the live channel: signing out unmounts it.
    this.api.unreadNotifCount().subscribe((r) => this.realtime.setCount(r.count));
    void this.realtime.start();
  }

  public ngOnDestroy(): void {
    void this.realtime.stop();
  }

  private go(url: string): void {
    // Explicit router navigation guarantees change detection triggers on the new route.
    this.router.navigateByUrl(url);
  }

  // Computed, not a one-off array: the Notifications badge has to re-render every
  // time the live count changes.
  public readonly menuItems = computed<MenuItem[]>(() => {
    const role = this.auth.role();
    const unread = this.realtime.unreadCount();
    const items: MenuItem[] = [
      { label: 'Dashboard', icon: 'pi pi-home', command: () => this.go('/dashboard') },
    ];
    if (role === 'Employee') {
      items.push({
        label: 'New Commitment',
        icon: 'pi pi-plus-circle',
        command: () => this.go('/tasks/new-commitment'),
      });
    }
    if (role === 'TopManagement') {
      items.push({
        label: 'New Voice Target',
        icon: 'pi pi-microphone',
        command: () => this.go('/tasks/new-target'),
      });
    }
    items.push({ label: 'Tasks', icon: 'pi pi-list', command: () => this.go('/tasks') });
    if (role === 'Employee') {
      items.push({
        label: 'My Commitments',
        icon: 'pi pi-user-edit',
        command: () => this.go('/my-commitments'),
      });
    }
    items.push({
      label: 'Event-wise Tasks',
      icon: 'pi pi-calendar',
      command: () => this.go('/event-tasks'),
    });
    if (role === 'VoiceReviewer') {
      items.push({
        label: 'Voice Review',
        icon: 'pi pi-headphones',
        command: () => this.go('/voice-review'),
      });
    }
    if (role === 'TopManagement') {
      items.push({
        label: 'Approvals',
        icon: 'pi pi-check-circle',
        command: () => this.go('/approvals'),
      });
      items.push({
        label: 'Extend Requests',
        icon: 'pi pi-hourglass',
        command: () => this.go('/extend-requests'),
      });
      items.push({
        label: 'Finished (P/F/C)',
        icon: 'pi pi-flag',
        command: () => this.go('/final-tasks'),
      });
      items.push({
        label: 'Team Performance',
        icon: 'pi pi-chart-bar',
        command: () => this.go('/performance-all'),
      });
    }
    if (role === 'Employee') {
      items.push({
        label: 'My Performance',
        icon: 'pi pi-chart-line',
        command: () => this.go('/performance'),
      });
    }
    items.push({
      label: 'Notifications',
      icon: 'pi pi-bell',
      // PrimeNG hides the badge when this is undefined, which is what we want at zero.
      badge: unread > 0 ? String(unread) : undefined,
      badgeStyleClass: 'notif-badge',
      command: () => this.go('/notifications'),
    });
    if (role === 'TopManagement' || role === 'SystemAdmin') {
      items.push({ label: 'Users', icon: 'pi pi-users', command: () => this.go('/admin/users') });
    }
    return items;
  });

  public initial(name: string): string {
    return (name || '?').charAt(0).toUpperCase();
  }

  public logout(): void {
    this.auth.logout();
  }
}
