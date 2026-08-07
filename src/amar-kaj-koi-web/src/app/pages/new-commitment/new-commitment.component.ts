import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DatePickerModule } from 'primeng/datepicker';
import { DividerModule } from 'primeng/divider';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { ApiService } from '../../core/api.service';
import { DayEventRef, EventChannelRef, TaskCenterRef } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { VoiceRecorderComponent } from '../../shared/voice-recorder.component';

interface SelectOption {
  label: string;
  value: string;
}

@Component({
  selector: 'app-new-commitment',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    CardModule,
    ButtonModule,
    InputTextModule,
    TextareaModule,
    SelectModule,
    DatePickerModule,
    DividerModule,
    VoiceRecorderComponent,
  ],
  templateUrl: './new-commitment.component.html',
  styleUrls: ['./new-commitment.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewCommitmentComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  public readonly centers = signal<TaskCenterRef[]>([]);
  public readonly channels = signal<EventChannelRef[]>([]);
  public readonly days = signal<DayEventRef[]>([]);
  public readonly centerOptions = signal<SelectOption[]>([]);
  public readonly channelOptions = signal<SelectOption[]>([]);
  public readonly dayOptions = signal<SelectOption[]>([]);

  public readonly savingDraft = signal(false);
  public readonly savingPost = signal(false);

  public readonly today = new Date();

  public readonly form = this.fb.group({
    taskName: ['', Validators.required],
    description: [''],
    taskCenterId: [null as string | null],
    eventChannelId: [null as string | null],
    dayEventId: [null as string | null],
    dueDate: [null as Date | null],
  });

  /**
   * Outside the form on purpose: the recorder reports both through output bindings,
   * and neither is something the user types into a field.
   */
  public readonly voiceFileId = signal<string | null>(null);
  /** FR-01: an under-3-second voice must not be saveable, even though voice is optional here. */
  public readonly voiceBlocked = signal(false);

  public ngOnInit(): void {
    this.api.taskCenters().subscribe((x) => {
      this.centers.set(x);
      this.centerOptions.set(x.map((c) => ({ label: c.name, value: c.taskCenterId })));
    });
    this.api.eventChannels().subscribe((x) => {
      this.channels.set(x);
      this.channelOptions.set(x.map((c) => ({ label: c.name, value: c.eventChannelId })));
    });
  }

  public onChannel(id?: string): void {
    this.form.controls.dayEventId.setValue(null);
    if (!id) {
      this.dayOptions.set([]);
      return;
    }
    this.api.dayEvents(id).subscribe((x) => {
      this.days.set(x);
      this.dayOptions.set(x.map((d) => ({ label: d.name, value: d.dayEventId })));
    });
  }

  public save(post: boolean): void {
    if (this.form.invalid || this.voiceBlocked()) {
      this.form.markAllAsTouched();
      return;
    }
    if (post) this.savingPost.set(true);
    else this.savingDraft.set(true);

    const { dueDate, ...rest } = this.form.getRawValue();
    const payload = {
      ...rest,
      voiceFileId: this.voiceFileId(),
      dueDate: dueDate ? dueDate.toISOString().substring(0, 10) : undefined,
      postImmediately: post,
    };

    this.api.createCommitment(payload).subscribe({
      next: (r) => {
        this.savingDraft.set(false);
        this.savingPost.set(false);
        this.toast.success(
          post
            ? `"${rest.taskName}" has gone to management for approval. You will be notified when they decide.`
            : `"${rest.taskName}" is saved privately. Only you can see it — open it later to edit or post it.`,
          post ? 'Commitment posted' : 'Saved as draft',
        );
        this.router.navigate(['/tasks', r.taskId]);
      },
      error: () => {
        this.savingDraft.set(false);
        this.savingPost.set(false);
      },
    });
  }
}
