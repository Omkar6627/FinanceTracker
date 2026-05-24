import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated) return true;
  router.navigate(['/login']);
  return false;
};

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isAuthenticated) return true;
  router.navigate(['/app/dashboard']);
  return false;
};

export function permissionGuard(permission: string): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    if (auth.isAuthenticated && auth.can(permission)) return true;
    router.navigate(['/app/dashboard']);
    return false;
  };
}

export const enterpriseGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isEnterprise) return true;
  router.navigate(['/app/dashboard']);
  return false;
};
