import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { DividerModule } from 'primeng/divider';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { TimelineModule } from 'primeng/timeline';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { DialogService } from '../../core/dialog.service';
import { EmployeeRef, ExtendRequest, TaskDetail } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { HumanizePipe } from '../../shared/humanize.pipe';
import { VoicePlayerComponent } from '../../shared/voice-player.component';
import { VoiceRecorderComponent } from '../../shared/voice-recorder.component';

interface SelectOption {
  label: string;
  value: string;
}

@Component({
  selector: 'app-task-detail',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    CardModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    TextareaModule,
    SelectModule,
    DatePickerModule,
    DividerModule,
    TimelineModule,
    VoiceRecorderComponent,
    VoicePlayerComponent,
    HumanizePipe,
  ],
  templateUrl: './task-detail.component.html',
  styleUrls: ['./task-detail.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskDetailComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(DialogService);
  private readonly auth = inject(AuthService);

  // Everything below is filled in from a subscribe callback, so all of it has to be
  // a signal for this OnPush view to repaint.
  public readonly task = signal<TaskDetail | null>(null);
  public readonly employees = signal<EmployeeRef[]>([]);
  public readonly employeeOptions = signal<SelectOption[]>([]);
  public readonly centerOptions = signal<SelectOption[]>([]);
  public readonly channelOptions = signal<SelectOption[]>([]);
  public readonly editDayOptions = signal<SelectOption[]>([]);

  /** Dialog visibility and the save spinner are all toggled from callbacks too. */
  public readonly showExtend = signal(false);
  public readonly showEdit = signal(false);
  public readonly savingEdit = signal(false);

  public readonly today = new Date();
  public role = '';

  /**
   * The management actions on this page are single fields rather than one form, so
   * each is its own control instead of being bundled into a group they do not share.
   */
  public readonly assigneeId = new FormControl<string | null>(null);
  public readonly newDueDate = new FormControl<Date | null>(null);
  public readonly finalComment = new FormControl('', { nonNullable: true });
  public readonly extendDecisionReason = new FormControl('', { nonNullable: true });
  public readonly markPassedDecisionReason = new FormControl('', { nonNullable: true });

  /** FR-19: the Extend / Revise dialog. */
  public readonly extendForm = this.fb.group({
    extendType: this.fb.nonNullable.control<'Extend' | 'Revise'>('Extend'),
    extendDate: this.fb.control<Date | null>(null),
    extendReason: this.fb.nonNullable.control(''),
  });

  /** FR-03 / FR-08: the Edit dialog. */
  public readonly editForm = this.fb.group({
    taskName: this.fb.nonNullable.control('', Validators.required),
    description: this.fb.nonNullable.control(''),
    taskCenterId: this.fb.control<string | null>(null),
    eventChannelId: this.fb.control<string | null>(null),
    dayEventId: this.fb.control<string | null>(null),
    dueDate: this.fb.control<Date | null>(null),
  });

  /**
   * Outside the forms: the recorder reports both through output bindings rather
   * than being typed into a field.
   */
  public readonly extendVoiceId = signal<string | null>(null);
  public readonly editVoiceFileId = signal<string | null>(null);
  /** FR-01: a replacement voice under 3 seconds must not be saveable. */
  public readonly editVoiceBlocked = signal(false);

  public ngOnInit(): void {
    this.role = this.auth.role();
    this.route.paramMap.subscribe((pm) => this.load(pm.get('id')!));
    this.api.employees().subscribe((e) => {
      this.employees.set(e);
      this.employeeOptions.set(e.map((x) => ({ label: x.fullName, value: x.userId })));
    });
    this.api
      .taskCenters()
      .subscribe((c) =>
        this.centerOptions.set(c.map((x) => ({ label: x.name, value: x.taskCenterId }))),
      );
    this.api
      .eventChannels()
      .subscribe((c) =>
        this.channelOptions.set(c.map((x) => ({ label: x.name, value: x.eventChannelId }))),
      );
  }

  public load(id: string): void {
    this.api.getTask(id).subscribe((t) => this.task.set(t));
  }

  public isAssignee(): boolean {
    return this.task()?.assignedToUserId === this.auth.user()?.userId;
  }

  /** FR-19/FR-23: the Extend or Revise request currently awaiting a decision. */
  public pendingExtendRequest(): ExtendRequest | null {
    return (
      this.task()?.extendRequests.find(
        (r) => r.status === 'Pending' && r.requestType !== 'MarkPassed',
      ) ?? null
    );
  }

  /** FR-26: a Mark-as-Passed request lives beside the task, not in its status. */
  public pendingMarkPassedRequest(): ExtendRequest | null {
    return (
      this.task()?.extendRequests.find(
        (r) => r.status === 'Pending' && r.requestType === 'MarkPassed',
      ) ?? null
    );
  }

  public canPost(): boolean {
    const t = this.task();
    return t?.statusCode === 'Draft' && t?.createdByUserId === this.auth.user()?.userId;
  }

  /** FR-03 / FR-08: creator can edit Draft (either type) or Pending-Approval commitment */
  public canEdit(): boolean {
    const t = this.task();
    if (!t) return false;
    if (t.createdByUserId !== this.auth.user()?.userId) return false;
    if (t.statusCode === 'Draft') return true;
    if (t.taskType === 'Commitment' && t.statusCode === 'PendingManagementApproval') return true;
    return false;
  }

  public openEditDialog(): void {
    const t = this.task();
    if (!t) return;
    this.editVoiceBlocked.set(false);
    this.editVoiceFileId.set(null);
    this.editForm.reset({
      taskName: t.taskName,
      description: t.description ?? '',
      taskCenterId: null,
      eventChannelId: null,
      dayEventId: null,
      dueDate: t.dueDate ? new Date(t.dueDate) : null,
    });
    // Preselect commitment fields by matching name
    if (t.taskType === 'Commitment') {
      const c = this.centerOptions().find((x) => x.label === t.taskCenter);
      const ch = this.channelOptions().find((x) => x.label === t.eventChannel);
      this.editForm.controls.taskCenterId.setValue(c?.value ?? null);
      this.editForm.controls.eventChannelId.setValue(ch?.value ?? null);
      if (ch) this.onEditChannel(ch.value);
    }
    this.showEdit.set(true);
  }

  public onEditChannel(channelId?: string): void {
    this.editForm.controls.dayEventId.setValue(null);
    if (!channelId) {
      this.editDayOptions.set([]);
      return;
    }
    this.api.dayEvents(channelId).subscribe((d) => {
      const options = d.map((x) => ({ label: x.name, value: x.dayEventId }));
      this.editDayOptions.set(options);
      const match = options.find((x) => x.label === this.task()?.dayEvent);
      if (match) this.editForm.controls.dayEventId.setValue(match.value);
    });
  }

  public submitEdit(): void {
    const t = this.task();
    if (!t || this.editForm.invalid || this.editVoiceBlocked()) {
      this.editForm.markAllAsTouched();
      return;
    }
    this.savingEdit.set(true);
    const e = this.editForm.getRawValue();
    const iso = e.dueDate ? e.dueDate.toISOString().substring(0, 10) : null;

    const req$ =
      t.taskType === 'Target'
        ? this.api.editTarget({
            taskId: t.taskId,
            taskName: e.taskName,
            description: e.description,
            voiceFileId: this.editVoiceFileId(),
          })
        : this.api.editCommitment({
            taskId: t.taskId,
            taskName: e.taskName,
            description: e.description,
            taskCenterId: e.taskCenterId,
            eventChannelId: e.eventChannelId,
            dayEventId: e.dayEventId,
            dueDate: iso,
          });

    req$.subscribe({
      next: () => {
        this.savingEdit.set(false);
        this.showEdit.set(false);
        this.toast.success(`Your changes to "${e.taskName}" have been saved.`, 'Changes saved');
        this.load(t.taskId);
      },
      error: () => {
        this.savingEdit.set(false);
      },
    });
  }

  public post(): void {
    const t = this.task();
    if (!t) return;
    this.api.postTask(t.taskId).subscribe(() => {
      this.toast.success(
        t.taskType === 'Target'
          ? `"${t.taskName}" has gone to the Voice Reviewer, who will turn it into an open task.`
          : `"${t.taskName}" has gone to management for approval. You will be notified when they decide.`,
        'Posted',
      );
      this.load(t.taskId);
    });
  }

  public async deleteDraft(): Promise<void> {
    const t = this.task();
    if (!t) return;
    const ok = await this.dialog.confirm({
      title: `Delete "${t.taskName}"?`,
      text: 'This draft and its recording will be removed for good. This cannot be undone.',
      confirmText: 'Delete',
      danger: true,
    });
    if (!ok) return;

    this.api.deleteDraft(t.taskId).subscribe(() => {
      this.toast.success(
        `"${t.taskName}" has been deleted. Taking you back to the task list.`,
        'Draft deleted',
      );
      this.router.navigateByUrl('/tasks');
    });
  }

  public approve(): void {
    const t = this.task();
    if (!t) return;
    const assignee = this.employeeOptions().find((o) => o.value === this.assigneeId.value)?.label;
    this.api.approveTask(t.taskId, this.assigneeId.value ?? undefined).subscribe(() => {
      this.toast.success(
        assignee
          ? `"${t.taskName}" is now open and assigned to ${assignee}.`
          : `"${t.taskName}" is now open and stays with ${t.createdByName}, who created it.`,
        'Approved',
      );
      this.load(t.taskId);
    });
  }

  public async reject(): Promise<void> {
    const t = this.task();
    if (!t) return;
    const reason = await this.dialog.prompt({
      title: `Reject "${t.taskName}"?`,
      text: `${t.createdByName} will see the reason you give, and the task will not go ahead.`,
      placeholder: 'Why is this being rejected?',
      confirmText: 'Reject',
      requiredMessage: 'A reason is required to reject.',
    });
    if (!reason) return;
    this.api.rejectTask(t.taskId, reason).subscribe(() => {
      this.toast.success(
        `"${t.taskName}" has been rejected and ${t.createdByName} can now read your reason.`,
        'Rejected',
      );
      this.load(t.taskId);
    });
  }

  public async sendBack(): Promise<void> {
    const t = this.task();
    if (!t) return;
    const reason = await this.dialog.prompt({
      title: `Send "${t.taskName}" back?`,
      text: `It returns to ${t.createdByName} as a draft so they can correct and re-post it.`,
      placeholder: 'What should they change?',
      confirmText: 'Send back',
      requiredMessage: 'A reason is required.',
    });
    if (!reason) return;
    this.api.sendBackTask(t.taskId, reason).subscribe(() => {
      this.toast.success(
        `"${t.taskName}" has gone back to ${t.createdByName} as a draft, with your note attached.`,
        'Sent back for correction',
      );
      this.load(t.taskId);
    });
  }

  public submitExtend(): void {
    const t = this.task();
    if (!t) return;
    const { extendType, extendDate, extendReason } = this.extendForm.getRawValue();
    const iso = extendDate ? extendDate.toISOString().substring(0, 10) : null;
    this.api
      .requestExtend({
        taskId: t.taskId,
        requestType: extendType,
        requestedDueDate: iso,
        reasonText: extendReason,
        voiceFileId: this.extendVoiceId(),
      })
      .subscribe(() => {
        const left = 3 - (t.requestCount + 1);
        const newDate = extendDate?.toLocaleDateString(undefined, { dateStyle: 'medium' });
        this.toast.success(
          `Your ${extendType} request for "${t.taskName}"${newDate ? ` (new date ${newDate})` : ''} is now with management. ` +
            `You have ${left} of 3 request${left === 1 ? '' : 's'} left on this task.`,
          `${extendType} requested`,
        );
        this.showExtend.set(false);
        this.extendForm.reset();
        this.extendVoiceId.set(null);
        this.load(t.taskId);
      });
  }

  public requestPassed(): void {
    const t = this.task();
    if (!t) return;
    this.api.requestMarkPassed(t.taskId).subscribe(() => {
      this.toast.success(
        `You have told management that "${t.taskName}" is finished. It stays open until they approve, and you cannot send another request until they decide.`,
        'Sent to management',
      );
      this.load(t.taskId);
    });
  }

  /** FR-26: management may only Approve or Reject the employee's request. */
  public decideMarkPassed(requestId: string, approve: boolean): void {
    const t = this.task();
    const who = this.pendingMarkPassedRequest()?.requestedByName ?? 'the assignee';
    if (!approve && !this.markPassedDecisionReason.value) {
      this.toast.error(
        `Type a reason in the "Decision reason" box before rejecting — ${who} will be shown it.`,
        'Reason needed',
      );
      return;
    }
    this.api.decideExtend(requestId, approve, this.markPassedDecisionReason.value).subscribe(() => {
      this.toast.success(
        approve
          ? `"${t?.taskName}" is now closed as Passed. ${who} has been notified.`
          : `${who}'s claim that "${t?.taskName}" is finished was rejected. The task stays open and they can see your reason.`,
        approve ? 'Marked as Passed' : 'Request rejected',
      );
      this.markPassedDecisionReason.setValue('');
      this.load(this.task()!.taskId);
    });
  }

  public attachVoice(v: { voiceFileId: string } | null): void {
    const t = this.task();
    if (!t || !v) return;
    this.api.attachVoiceCommitment(t.taskId, v.voiceFileId).subscribe(() => {
      this.toast.success(
        `Your voice commitment is now attached to "${t.taskName}". Management can play it from this page.`,
        'Voice attached',
      );
      this.load(t.taskId);
    });
  }

  public changeDue(): void {
    const t = this.task();
    const due = this.newDueDate.value;
    if (!t || !due) return;
    const iso = due.toISOString().substring(0, 10);
    const wasDue = t.dueDate
      ? new Date(t.dueDate).toLocaleDateString(undefined, { dateStyle: 'medium' })
      : 'no date';
    this.api.changeDueDate(t.taskId, iso).subscribe(() => {
      this.toast.success(
        `"${t.taskName}" moved from ${wasDue} to ${due.toLocaleDateString(undefined, { dateStyle: 'medium' })}. ${t.assignedToName ?? 'The assignee'} has been notified.`,
        'Due date updated',
      );
      this.load(t.taskId);
    });
  }

  public changeAssignee(): void {
    const t = this.task();
    const assignee = this.assigneeId.value;
    if (!t || !assignee) return;
    const newName =
      this.employeeOptions().find((o) => o.value === assignee)?.label ?? 'someone else';
    this.api.changeAssignee(t.taskId, assignee).subscribe(() => {
      this.toast.success(
        `"${t.taskName}" moved from ${t.assignedToName ?? 'nobody'} to ${newName}. Both have been notified.`,
        'Assignee changed',
      );
      this.load(t.taskId);
    });
  }

  public pin(): void {
    const t = this.task();
    if (!t) return;
    this.api.pinTask(t.taskId, !t.isPinned).subscribe(() => this.load(t.taskId));
  }

  public async markFinal(decision: 'Passed' | 'Failed' | 'Cancelled'): Promise<void> {
    const t = this.task();
    if (!t) return;

    // Passing a task needs no confirmation; failing or cancelling one does.
    if (decision !== 'Passed') {
      const ok = await this.dialog.confirm({
        title: `Mark "${t.taskName}" as ${decision}?`,
        text: `This closes the task for ${t.assignedToName ?? 'the assignee'} and counts against their performance. Only a System Admin can reopen it.`,
        confirmText: `Mark ${decision}`,
        danger: true,
      });
      if (!ok) return;
    }

    this.api.markFinal(t.taskId, decision, this.finalComment.value).subscribe(() => {
      this.toast.success(
        `"${t.taskName}" is now closed as ${decision}${this.finalComment.value ? ', with your final comment saved' : ''}. ${t.assignedToName ?? 'The assignee'} has been notified.`,
        `Marked ${decision}`,
      );
      this.load(t.taskId);
    });
  }

  public decideExtend(requestId: string, approve: boolean): void {
    const t = this.task();
    const req = this.pendingExtendRequest();
    const who = req?.requestedByName ?? 'the assignee';
    const kind = req?.requestType ?? 'request';

    if (!approve && !this.extendDecisionReason.value) {
      this.toast.error(
        `Type a reason in the "Decision reason" box before rejecting — ${who} will be shown it.`,
        'Reason needed',
      );
      return;
    }

    this.api.decideExtend(requestId, approve, this.extendDecisionReason.value).subscribe(() => {
      const newDate = req?.requestedDueDate
        ? new Date(req.requestedDueDate).toLocaleDateString(undefined, { dateStyle: 'medium' })
        : null;
      this.toast.success(
        approve
          ? `${who}'s ${kind} request on "${t?.taskName}" was approved${newDate ? ` — it is now due ${newDate}` : ''}.`
          : `${who}'s ${kind} request on "${t?.taskName}" was rejected. The original due date stands and they can see your reason.`,
        approve ? 'Request approved' : 'Request rejected',
      );
      this.load(this.task()!.taskId);
    });
  }

  public reopen(): void {
    const t = this.task();
    if (!t) return;
    this.api.reopenTask(t.taskId).subscribe(() => {
      this.toast.success(
        `"${t.taskName}" has been moved from ${t.statusName} back to Open. ${t.assignedToName ?? 'The assignee'} can work on it again.`,
        'Task reopened',
      );
      this.load(t.taskId);
    });
  }
}
