import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { TaskListItem } from '../../core/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, CardModule, TableModule, ButtonModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent implements OnInit {
  // Public because the template reads auth.user() directly.
  public readonly auth = inject(AuthService);
  private readonly api = inject(ApiService);

  public readonly tasks = signal<TaskListItem[]>([]);
  public readonly loading = signal(true);

  /** The eight most recent tasks, recomputed only when the list itself changes. */
  public readonly recentTasks = computed(() => this.tasks().slice(0, 8));

  public ngOnInit(): void {
    this.api.listTasks({}).subscribe({
      next: (rows) => {
        this.tasks.set(rows);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }

  public count(code: string): number {
    return this.tasks().filter((t) => t.statusCode === code).length;
  }

  public countAwaiting(): number {
    const role = this.auth.role();
    if (role === 'TopManagement') {
      return this.count('PendingManagementApproval') + this.count('RequestToExtendRevise');
    }
    if (role === 'VoiceReviewer') {
      return this.count('PendingVoiceReview');
    }
    if (role === 'Employee') {
      return this.count('Open') + this.count('Overdue');
    }
    return 0;
  }
}