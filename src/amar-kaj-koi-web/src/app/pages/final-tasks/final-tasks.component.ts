import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { TaskListItem } from '../../core/models';

const STATUS_PASSED = '22222222-2222-2222-2222-000000000007';
const STATUS_FAILED = '22222222-2222-2222-2222-000000000008';
const STATUS_CANCELLED = '22222222-2222-2222-2222-000000000009';

interface FinalCounts {
  passed: number;
  failed: number;
  cancelled: number;
}

@Component({
  selector: 'app-final-tasks',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    CardModule,
    TableModule,
    SelectModule,
    ButtonModule,
  ],
  templateUrl: './final-tasks.component.html',
  styleUrls: ['./final-tasks.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FinalTasksComponent implements OnInit {
  private readonly api = inject(ApiService);

  public readonly rows = signal<TaskListItem[]>([]);
  public readonly loading = signal(true);
  public readonly counts = signal<FinalCounts>({ passed: 0, failed: 0, cancelled: 0 });

  /** A lone control rather than a group: this card has exactly one filter. */
  public readonly filterStatus = new FormControl<string | null>(null);

  public readonly filterOptions = [
    { label: 'Passed', value: STATUS_PASSED },
    { label: 'Failed', value: STATUS_FAILED },
    { label: 'Cancelled', value: STATUS_CANCELLED },
  ];

  public ngOnInit(): void {
    this.reload();
  }

  public reload(): void {
    this.loading.set(true);
    const status = this.filterStatus.value;
    if (status) {
      this.api.listAllTasks({ statusId: status }).subscribe((r) => {
        this.rows.set(r);
        this.loading.set(false);
      });
    } else {
      forkJoin({
        p: this.api.listAllTasks({ statusId: STATUS_PASSED }),
        f: this.api.listAllTasks({ statusId: STATUS_FAILED }),
        c: this.api.listAllTasks({ statusId: STATUS_CANCELLED }),
      }).subscribe(({ p, f, c }) => {
        this.counts.set({ passed: p.length, failed: f.length, cancelled: c.length });
        this.rows.set([...p, ...f, ...c].sort((a, b) => (a.createdAt < b.createdAt ? 1 : -1)));
        this.loading.set(false);
      });
    }
  }
}
