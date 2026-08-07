import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DividerModule } from 'primeng/divider';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { VoiceRecorderComponent } from '../../shared/voice-recorder.component';

@Component({
  selector: 'app-new-target',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    CardModule,
    ButtonModule,
    InputTextModule,
    TextareaModule,
    DividerModule,
    VoiceRecorderComponent,
  ],
  templateUrl: './new-target.component.html',
  styleUrls: ['./new-target.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewTargetComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  public readonly form = this.fb.nonNullable.group({
    taskName: '',
    description: '',
  });

  /**
   * Not a form control: the recorder reports it through an output binding, and it
   * is an id the user never types. A signal so the buttons it gates repaint.
   */
  public readonly voiceFileId = signal<string | null>(null);

  public readonly savingDraft = signal(false);
  public readonly savingPost = signal(false);

  public save(post: boolean): void {
    const voiceFileId = this.voiceFileId();
    if (!voiceFileId) {
      this.toast.error(
        'A voice target needs a recording of at least 3 seconds. Use the Record button above, then save.',
        'Voice recording required',
      );
      return;
    }

    if (post) {
      this.savingPost.set(true);
    }
    else {
      this.savingDraft.set(true);
    }

    const { taskName, description } = this.form.getRawValue();

    this.api
      .createTarget({
        taskName: taskName || '(Voice Target)',
        description,
        voiceFileId,
        postImmediately: post,
      })
      .subscribe({
        next: (r) => {
          this.savingDraft.set(false);
          this.savingPost.set(false);
          this.toast.success(
            post
              ? `"${taskName || '(Voice Target)'}" has gone to the Voice Reviewer, who will listen to it and turn it into an open task.`
              : `"${taskName || '(Voice Target)'}" is saved privately. Only you can see it — open it later to edit, delete or post it.`,
            post ? 'Target posted for review' : 'Saved as draft',
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