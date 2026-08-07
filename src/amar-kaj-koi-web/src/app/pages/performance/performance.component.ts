import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CardModule } from 'primeng/card';
import { ProgressBarModule } from 'primeng/progressbar';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { PerformanceRow } from '../../core/models';

@Component({
  selector: 'app-performance',
  standalone: true,
  imports: [CardModule, ProgressBarModule],
  templateUrl: './performance.component.html',
  styleUrls: ['./performance.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PerformanceComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);

  public readonly row = signal<PerformanceRow | null>(null);

  public ngOnInit(): void {
    this.api.selfPerformance().subscribe((r) => {
      r.fullName = this.auth.user()?.fullName ?? '';
      this.row.set(r);
    });
  }
}
