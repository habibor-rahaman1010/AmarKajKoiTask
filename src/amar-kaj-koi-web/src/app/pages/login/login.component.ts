import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { AuthService } from '../../core/auth.service';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonModule, CardModule, InputTextModule, PasswordModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  public readonly form = this.fb.nonNullable.group({
    username: ['', Validators.required],
    password: ['', Validators.required],
  });

  /** Cleared from the login callback, which is why this one is a signal. */
  public readonly submitting = signal(false);

  public onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    const { username, password } = this.form.getRawValue();
    this.auth.login(username, password).subscribe({
      next: (resp) => {
        this.submitting.set(false);
        this.toast.success(
          `Signed in as ${resp.user.roleName}. Taking you to your dashboard.`,
          `Welcome, ${resp.user.fullName}`,
        );
        this.router.navigateByUrl('/dashboard');
      },
      error: () => {
        this.submitting.set(false);
        this.toast.error(
          'That username and password did not match. Check them and try again.',
          'Sign in failed',
        );
      },
    });
  }
}
