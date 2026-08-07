import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DatePickerModule } from 'primeng/datepicker';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { ApiService, TaskFilter } from '../../core/api.service';
import { DialogService } from '../../core/dialog.service';
import { AuthService } from '../../core/auth.service';
import { SelectOption, StatusRef, TaskListItem } from '../../core/models';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-tasks-list',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    CardModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    SelectModule,
    DatePickerModule,
  ],
  templateUrl: './tasks-list.component.html',
  styleUrls: ['./tasks-list.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})



export class TasksListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly dialog = inject(DialogService);
  private readonly toast = inject(ToastService);
  public readonly auth = inject(AuthService);

  public readonly statuses = signal<StatusRef[]>([]);
  public readonly statusOptions = signal<SelectOption[]>([]);
  public readonly tasks = signal<TaskListItem[]>([]);
  public readonly loading = signal(false);

  public readonly filterForm = this.fb.group({
    search: [''],
    statusId: [null as string | null],
    fromDate: [null as Date | null],
    toDate: [null as Date | null],
  });

  // Still plain: these come from the table's two-way selection binding, a template
  // event, which re-renders on its own under OnPush.
  public selected: string[] = [];
  public selectedRows: TaskListItem[] = [];

  public ngOnInit(): void {
    this.api.statuses().subscribe((s) => {
      this.statuses.set(s);
      this.statusOptions.set(s.map((x) => ({ label: x.statusName, value: x.statusId })));
    });
    this.load();
  }

  public isRole(r: string[]): boolean {
    return this.auth.hasAnyRole(r);
  }

  public load(): void {
    this.loading.set(true);
    this.selected = [];
    this.selectedRows = [];

    const { search, statusId, fromDate, toDate } = this.filterForm.getRawValue();
    const f: TaskFilter = {
      search: search ?? undefined,
      statusId: statusId ?? undefined,
      fromDate: fromDate ? this.toIsoDate(fromDate) : undefined,
      toDate: toDate ? this.toIsoDate(toDate) : undefined,
    };

    this.api.listTasks(f).subscribe({
      next: (rows) => {
        this.tasks.set(rows);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }

  public reset(): void {
    this.filterForm.reset();
    this.load();
  }

  public onSelectionChange(): void {
    this.selected = this.selectedRows.map((r) => r.taskId);
  }

  public async bulk(decision: 'Passed' | 'Failed' | 'Cancelled'): Promise<void> {
    const count = this.selected.length;
    const ok = await this.dialog.confirm({
      title: `Mark ${count} task${count === 1 ? '' : 's'} as ${decision}?`,
      text: `This closes ${count === 1 ? 'it' : 'them all'} at once. Only a System Admin can reopen a task afterwards.`,
      confirmText: `Mark ${decision}`,
      danger: decision !== 'Passed',
    });
    if (!ok) return;

    this.api.bulkFinal(this.selected, decision).subscribe((r) => {
      this.toast.success(
        `${r.affected} of ${count} selected task${count === 1 ? '' : 's'} closed as ${decision}. They no longer appear as active work.`,
        'Bulk update applied',
      );
      this.load();
    });
  }

  private toIsoDate(d: Date): string {
    return d.toISOString().substring(0, 10);
  }
}
