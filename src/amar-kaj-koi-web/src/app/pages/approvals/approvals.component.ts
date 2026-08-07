import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormRecord, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { ApiService } from '../../core/api.service';
import { DialogService } from '../../core/dialog.service';
import { TaskListItem } from '../../core/models';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-approvals',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    CardModule,
    TableModule,
    ButtonModule,
    SelectModule,
  ],
  templateUrl: './approvals.component.html',
  styleUrls: ['./approvals.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApprovalsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(DialogService);

  public readonly pending = signal<TaskListItem[]>([]);
  public readonly employeeOptions = signal<{ label: string; value: string }[]>([]);
  public readonly loading = signal(true);

  /**
   * One control per pending row, keyed by task id. A FormRecord rather than a
   * FormGroup because the keys are only known once the rows arrive; it is rebuilt
   * in load() just before `pending` is published, so the two are never out of step.
   */
  public readonly assignForm = new FormRecord<FormControl<string | null>>({});

  // Well-known StatusId GUIDs (must match TaskStatuses seed in 02_SeedData.sql)
  private readonly StatusPendingApproval = '22222222-2222-2222-2222-000000000002';

  public ngOnInit(): void {
    this.api
      .employees()
      .subscribe((e) =>
        this.employeeOptions.set(e.map((u) => ({ label: u.fullName, value: u.userId }))),
      );
    this.load();
  }

  public load(): void {
    this.loading.set(true);
    this.api.listTasks({ statusId: this.StatusPendingApproval }).subscribe((rows) => {
      this.rebuildAssignControls(rows.map((r) => r.taskId));
      this.pending.set(rows);
      this.loading.set(false);
    });
  }

  private rebuildAssignControls(taskIds: string[]): void {
    Object.keys(this.assignForm.controls).forEach((k) => this.assignForm.removeControl(k));
    taskIds.forEach((id) => this.assignForm.addControl(id, new FormControl<string | null>(null)));
  }

  /** Names are read before the call, because load() replaces the row afterwards. */
  private nameOf(taskId: string): string {
    return this.pending().find((t) => t.taskId === taskId)?.taskName ?? 'The task';
  }

  private assigneeNameOf(taskId: string): string | null {
    const userId = this.assignForm.controls[taskId]?.value;
    return this.employeeOptions().find((o) => o.value === userId)?.label ?? null;
  }

  public approve(id: string): void {
    const name = this.nameOf(id);
    const assignee = this.assigneeNameOf(id);
    this.api.approveTask(id, this.assignForm.controls[id]?.value ?? undefined).subscribe(() => {
      this.toast.success(
        assignee
          ? `"${name}" is now open and assigned to ${assignee}.`
          : `"${name}" is now open and stays with the person who created it.`,
        'Approved',
      );
      this.load();
    });
  }

  public async reject(id: string): Promise<void> {
    const name = this.nameOf(id);
    const reason = await this.dialog.prompt({
      title: `Reject "${name}"?`,
      text: 'The employee who created it will see the reason you give.',
      placeholder: 'Why is this being rejected?',
      confirmText: 'Reject',
      requiredMessage: 'A reason is required to reject.',
    });
    if (!reason) return;
    this.api.rejectTask(id, reason).subscribe(() => {
      this.toast.success(
        `"${name}" has been rejected and the creator can now read your reason. It will not go ahead.`,
        'Rejected',
      );
      this.load();
    });
  }

  public async sendBack(id: string): Promise<void> {
    const name = this.nameOf(id);
    const reason = await this.dialog.prompt({
      title: `Send "${name}" back?`,
      text: 'Tell the employee what needs changing before you will approve it.',
      placeholder: 'What should they correct?',
      confirmText: 'Send back',
      requiredMessage: 'A reason is required.',
    });
    if (!reason) return;
    this.api.sendBackTask(id, reason).subscribe(() => {
      this.toast.success(
        `"${name}" has gone back to its creator as a draft. They can edit it and post it again.`,
        'Sent back for correction',
      );
      this.load();
    });
  }
}
