import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ApiService } from '../../core/api.service';
import { EmployeeRef } from '../../core/models';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    CardModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    SelectModule,
    TagModule,
  ],
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);

  public readonly users = signal<EmployeeRef[]>([]);

  public readonly form = this.fb.nonNullable.group({
    fullName: ['', Validators.required],
    username: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    roleName: ['Employee'],
  });

  public readonly roleOptions = [
    { label: 'Employee', value: 'Employee' },
    { label: 'Top Management', value: 'TopManagement' },
    { label: 'Voice Reviewer', value: 'VoiceReviewer' },
    { label: 'System Admin', value: 'SystemAdmin' },
  ];

  public ngOnInit(): void {
    this.load();
  }

  public load(): void {
    this.api.allUsers().subscribe((x) => this.users.set(x));
  }

  public save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { fullName, username, roleName } = this.form.getRawValue();
    this.api.register(this.form.getRawValue()).subscribe(() => {
      this.toast.success(
        `${fullName} can now sign in as "${username}" with the ${roleName} role.`,
        'User created',
      );
      // reset() puts every control back to the value it was declared with, so the
      // role drops back to Employee rather than to null.
      this.form.reset();
      this.load();
    });
  }

  public roleSeverity(role: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (role) {
      case 'TopManagement':
        return 'info';
      case 'SystemAdmin':
        return 'danger';
      case 'VoiceReviewer':
        return 'warn';
      default:
        return 'success';
    }
  }
}