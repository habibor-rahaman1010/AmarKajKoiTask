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
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { TaskListItem } from '../../core/models';

interface EventGroup {
  channel: string;
  tasks: TaskListItem[];
}

@Component({
  selector: 'app-event-tasks',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    CardModule,
    TableModule,
    ButtonModule,
    TagModule,
    PaginatorModule,
  ],
  templateUrl: './event-tasks.component.html',
  styleUrls: ['./event-tasks.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EventTasksComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);

  public readonly groups = signal<EventGroup[]>([]);
  public readonly loading = signal(true);

  // Client-side paging over the event cards; the API hands back every task in one go.
  public readonly rowsPerPageOptions = [5, 10, 20];
  public readonly first = signal(0);
  public readonly rows = signal(this.rowsPerPageOptions[0]);
  public readonly pagedGroups = computed(() =>
    this.groups().slice(this.first(), this.first() + this.rows()),
  );
  // Keyed off the smallest page size, not the current one — otherwise picking a large
  // page size hides the paginator and strands the user with no way back.
  public readonly showPaginator = computed(
    () => this.groups().length > this.rowsPerPageOptions[0],
  );

  public ngOnInit(): void {
    // Use role-appropriate endpoint. Employee → tasks visible to them; others → all tasks.
    const request$ = this.auth.hasAnyRole(['TopManagement', 'SystemAdmin'])
      ? this.api.listAllTasks({})
      : this.api.listTasks({});

    request$.subscribe((rows) => {
      const withChannel = rows.filter((r) => !!r.eventChannel);
      const map = new Map<string, TaskListItem[]>();
      withChannel.forEach((r) => {
        const key = r.eventChannel!;
        if (!map.has(key)) map.set(key, []);
        map.get(key)!.push(r);
      });
      this.groups.set(
        Array.from(map.entries())
          .map(([channel, tasks]) => ({ channel, tasks: tasks.sort(this.byDueDate) }))
          .sort((a, b) => a.channel.localeCompare(b.channel)),
      );
      this.first.set(0);
      this.loading.set(false);
    });
  }

  public onPage(e: PaginatorState): void {
    this.first.set(e.first ?? 0);
    this.rows.set(e.rows ?? this.rowsPerPageOptions[0]);
  }

  private byDueDate(a: TaskListItem, b: TaskListItem): number {
    if (!a.dueDate && !b.dueDate) return 0;
    if (!a.dueDate) return 1;
    if (!b.dueDate) return -1;
    return a.dueDate < b.dueDate ? -1 : 1;
  }
}
