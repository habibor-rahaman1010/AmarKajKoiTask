import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormRecord, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { TextareaModule } from 'primeng/textarea';
import { ApiService } from '../../core/api.service';
import { ExtendRequest } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { VoicePlayerComponent } from '../../shared/voice-player.component';

@Component({
  selector: 'app-extend-requests',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    CardModule,
    ButtonModule,
    TagModule,
    TextareaModule,
    VoicePlayerComponent,
  ],
  templateUrl: './extend-requests.component.html',
  styleUrls: ['./extend-requests.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExtendRequestsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);

  // Written from a subscribe callback, so they must be signals under OnPush.
  public readonly requests = signal<ExtendRequest[]>([]);
  public readonly loading = signal(true);

  /**
   * One decision-reason control per pending request, keyed by request id. Rebuilt
   * in load() just before `requests` is published so the two stay in step.
   */
  public readonly reasonForm = new FormRecord<FormControl<string>>({});

  public ngOnInit(): void {
    this.load();
  }

  public load(): void {
    this.loading.set(true);
    this.api.pendingExtends().subscribe((r) => {
      Object.keys(this.reasonForm.controls).forEach((k) => this.reasonForm.removeControl(k));
      r.forEach((x) =>
        this.reasonForm.addControl(x.requestId, new FormControl('', { nonNullable: true })),
      );
      this.requests.set(r);
      this.loading.set(false);
    });
  }

  public decide(requestId: string, approve: boolean): void {
    const req = this.requests().find((r) => r.requestId === requestId);
    const who = req?.requestedByName ?? 'the employee';
    const kind = req?.requestType ?? 'request';
    const reason = this.reasonForm.controls[requestId]?.value ?? '';

    if (!approve && !reason) {
      this.toast.error(
        `Type a reason in the box on this card before rejecting ${who}'s ${kind} request — they will be shown it.`,
        'Reason needed',
      );
      return;
    }

    this.api.decideExtend(requestId, approve, reason).subscribe(() => {
      const newDate = req?.requestedDueDate
        ? new Date(req.requestedDueDate).toLocaleDateString(undefined, { dateStyle: 'medium' })
        : null;
      this.toast.success(
        approve
          ? `${who}'s ${kind} request was approved${newDate ? ` and the task is now due ${newDate}` : ''}.`
          : `${who}'s ${kind} request was rejected. The original due date stands and they can see your reason.`,
        approve ? 'Request approved' : 'Request rejected',
      );
      this.load();
    });
  }
}
