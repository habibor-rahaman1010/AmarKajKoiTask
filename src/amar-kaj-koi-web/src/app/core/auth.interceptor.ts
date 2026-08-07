import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';
import { ToastService } from './toast.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const toast = inject(ToastService);
  const token = auth.token();

  const authed = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authed).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401) {
        auth.logout();
        toast.error(
          'Your sign-in is no longer valid, so you have been logged out. Sign in again to carry on.',
          'Session expired',
        );
      } else if (err.status === 403) {
        toast.error(
          `Your role (${auth.role() || 'unknown'}) is not allowed to do that. Ask an administrator if you need access.`,
          'Not permitted',
        );
      } else if (err.status === 400) {
        // The API sends a human-readable message for rule violations; prefer it.
        toast.error(
          err.error?.message ??
            'The server rejected that request as invalid. Check the values and try again.',
          'Could not save',
        );
      } else if (err.status === 0) {
        toast.error(
          'The API did not respond. Check that the server is running and that you are online, then try again.',
          'Cannot reach the server',
        );
      } else if (err.status >= 500) {
        toast.error(
          err.error?.message ??
            'Something failed on the server. Nothing was saved — please try again.',
          'Server error',
        );
      }
      return throwError(() => err);
    }),
  );
};
