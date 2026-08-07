import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }
  router.navigateByUrl('/login');
  return false;
};

export const roleGuard = (allowed: string[]): CanActivateFn => {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    if (!auth.isAuthenticated()) {
      router.navigateByUrl('/login');
      return false;
    }
    if (auth.hasAnyRole(allowed)) {
      return true;
    }
    router.navigateByUrl('/dashboard');
    return false;
  };
};
