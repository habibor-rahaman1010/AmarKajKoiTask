import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CardModule } from 'primeng/card';
import { ProgressBarModule } from 'primeng/progressbar';
import { TableModule } from 'primeng/table';
import { ApiService } from '../../core/api.service';
import { PerformanceRow } from '../../core/models';

@Component({
  selector: 'app-performance-all',
  standalone: true,
  imports: [CardModule, TableModule, ProgressBarModule],
  templateUrl: './performance-all.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PerformanceAllComponent implements OnInit {
  private readonly api = inject(ApiService);

  public readonly rows = signal<PerformanceRow[]>([]);
  public readonly loading = signal(true);

  public ngOnInit(): void {
    this.api.allPerformance().subscribe((r) => {
      this.rows.set(r);
      this.loading.set(false);
    });
  }
}
