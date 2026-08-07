import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import {
  FormBuilder,
  FormControl,
  FormGroup,
  FormRecord,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DatePickerModule } from 'primeng/datepicker';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { ApiService } from '../../core/api.service';
import { DialogService } from '../../core/dialog.service';
import {
  DayEventRef,
  EmployeeRef,
  EventChannelRef,
  SelectOption,
  TaskCenterRef,
  TaskListItem,
} from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { VoicePlayerComponent } from '../../shared/voice-player.component';

/** The fields a reviewer fills in to turn one voice target into an open task. */
type ReviewForm = FormGroup<{
  taskName: FormControl<string>;
  taskCenterId: FormControl<string | null>;
  eventChannelId: FormControl<string | null>;
  dayEventId: FormControl<string | null>;
  dueDate: FormControl<Date | null>;
  assignedToUserId: FormControl<string | null>;
}>;

@Component({
  selector: 'app-voice-review',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    CardModule,
    ButtonModule,
    InputTextModule,
    SelectModule,
    DatePickerModule,
    VoicePlayerComponent,
  ],
  templateUrl: './voice-review.component.html',
  styleUrls: ['./voice-review.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VoiceReviewComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(DialogService);

  public readonly centers = signal<TaskCenterRef[]>([]);
  public readonly channels = signal<EventChannelRef[]>([]);
  public readonly employees = signal<EmployeeRef[]>([]);
  public readonly centerOptions = signal<SelectOption[]>([]);
  public readonly channelOptions = signal<SelectOption[]>([]);
  public readonly employeeOptions = signal<SelectOption[]>([]);
  public readonly pending = signal<TaskListItem[]>([]);
  public readonly loading = signal(true);

  /**
   * Both are filled in from inside subscribe callbacks and keyed by task id, so
   * they are replaced rather than mutated — a signal only notifies on a new value.
   */
  public readonly daysByTask = signal<Record<string, SelectOption[]>>({});
  public readonly voiceLoaded = signal<Record<string, string>>({});

  /**
   * One form per pending task, keyed by task id — each card on this screen is an
   * independent form that is submitted on its own. Rebuilt just before `pending`
   * is published so a card never renders before its form exists.
   */
  public readonly forms = new FormRecord<ReviewForm>({});

  public readonly today = new Date();

  // Well-known StatusId for 'PendingVoiceReview' (must match seed)
  private readonly StatusPendingVoiceReview = '22222222-2222-2222-2222-000000000003';

  public ngOnInit(): void {
    this.api.taskCenters().subscribe((x) => {
      this.centers.set(x);
      this.centerOptions.set(x.map((c) => ({ label: c.name, value: c.taskCenterId })));
    });
    this.api.eventChannels().subscribe((x) => {
      this.channels.set(x);
      this.channelOptions.set(x.map((c) => ({ label: c.name, value: c.eventChannelId })));
    });
    this.api.employees().subscribe((x) => {
      this.employees.set(x);
      this.employeeOptions.set(x.map((u) => ({ label: u.fullName, value: u.userId })));
    });
    this.load();
  }

  public load(): void {
    this.loading.set(true);
    this.api.listTasks({ statusId: this.StatusPendingVoiceReview }).subscribe((rows) => {
      Object.keys(this.forms.controls).forEach((k) => this.forms.removeControl(k));
      rows.forEach((r) => {
        this.forms.addControl(r.taskId, this.buildReviewForm(r.taskName));
        this.api.getTask(r.taskId).subscribe((t) => {
          if (t.voiceFileId) {
            this.voiceLoaded.update((m) => ({ ...m, [r.taskId]: t.voiceFileId! }));
          }
        });
      });
      this.pending.set(rows);
      this.loading.set(false);
    });
  }

  private buildReviewForm(taskName: string): ReviewForm {
    return this.fb.group({
      taskName: this.fb.nonNullable.control(taskName, Validators.required),
      taskCenterId: this.fb.control<string | null>(null, Validators.required),
      eventChannelId: this.fb.control<string | null>(null, Validators.required),
      dayEventId: this.fb.control<string | null>(null),
      dueDate: this.fb.control<Date | null>(null, Validators.required),
      assignedToUserId: this.fb.control<string | null>(null, Validators.required),
    });
  }

  public loadDays(taskId: string, channelId?: string): void {
    this.forms.controls[taskId].controls.dayEventId.setValue(null);
    if (!channelId) {
      this.daysByTask.update((m) => ({ ...m, [taskId]: [] }));
      return;
    }
    this.api.dayEvents(channelId).subscribe((d: DayEventRef[]) => {
      const options = d.map((x) => ({ label: x.name, value: x.dayEventId }));
      this.daysByTask.update((m) => ({ ...m, [taskId]: options }));
    });
  }

  public isValid(taskId: string): boolean {
    return this.forms.controls[taskId]?.valid ?? false;
  }

  public complete(taskId: string): void {
    const form = this.forms.controls[taskId];
    if (!form || form.invalid) {
      form?.markAllAsTouched();
      const missing = form ? this.missingFieldsOf(form) : [];
      this.toast.error(
        missing.length
          ? `Still needed on this card: ${missing.join(', ')}. Fill these in, then Save & Open Task.`
          : 'Fill in the required fields on this card before opening the task.',
        'Missing details',
      );
      return;
    }
    const f = form.getRawValue();
    const dto = {
      taskId,
      taskName: f.taskName,
      taskCenterId: f.taskCenterId,
      eventChannelId: f.eventChannelId,
      dayEventId: f.dayEventId,
      dueDate: f.dueDate ? f.dueDate.toISOString().substring(0, 10) : null,
      assignedToUserId: f.assignedToUserId,
    };
    const assignee = this.employeeOptions().find((o) => o.value === f.assignedToUserId)?.label;
    const due = f.dueDate?.toLocaleDateString(undefined, { dateStyle: 'medium' });
    this.api.voiceReviewComplete(dto).subscribe(() => {
      this.toast.success(
        `"${f.taskName}" is now an open task${assignee ? ` assigned to ${assignee}` : ''}${due ? `, due ${due}` : ''}. It has left your review queue.`,
        'Task opened',
      );
      this.load();
    });
  }

  /** Turns the failing controls into wording a reviewer can act on. */
  private missingFieldsOf(form: ReviewForm): string[] {
    const labels: Record<string, string> = {
      taskName: 'task name',
      taskCenterId: 'task center',
      eventChannelId: 'event channel',
      dueDate: 'due date',
      assignedToUserId: 'assigned to',
    };
    return Object.entries(labels)
      .filter(([key]) => form.get(key)?.invalid)
      .map(([, label]) => label);
  }

  public async sendBack(taskId: string): Promise<void> {
    const name = this.pending().find((t) => t.taskId === taskId)?.taskName ?? 'This target';
    const reason = await this.dialog.prompt({
      title: `Send "${name}" back?`,
      text: 'Tell the creator what needs re-recording or fixing.',
      placeholder: 'e.g. the recording is unclear after the first few seconds',
      confirmText: 'Send back',
      requiredMessage: 'A reason is required.',
    });
    if (!reason) return;
    this.api.voiceReviewSendBack(taskId, reason).subscribe(() => {
      this.toast.success(
        `"${name}" has gone back to its creator as a draft and has left your review queue.`,
        'Sent back for correction',
      );
      this.load();
    });
  }
}
