import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { ApiService } from '../../core/api.service';
import { TaskListItem } from '../../core/models';

@Component({
  selector: 'app-my-commitments',
  standalone: true,
  imports: [CommonModule, RouterLink, CardModule, TableModule, ButtonModule],
  templateUrl: './my-commitments.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyCommitmentsComponent implements OnInit {
  private readonly api = inject(ApiService);

  public readonly tasks = signal<TaskListItem[]>([]);
  public readonly loading = signal(true);

  public ngOnInit(): void {
    this.api.listMyCreatedTasks().subscribe({
      next: (rows) => {
        this.tasks.set(rows);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }
}
