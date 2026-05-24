import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TokenService } from '../auth/token.service';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const tokens = inject(TokenService);
  const token = tokens.getAccessToken();
  if (token && !req.url.includes('/auth/login') && !req.url.includes('/auth/register')) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }
  return next(req);
};
