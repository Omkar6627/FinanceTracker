import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { ToastController } from '@ionic/angular';
import { AuthService } from '../auth/auth.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const auth = inject(AuthService);
  const toastCtrl = inject(ToastController);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401 && !req.url.includes('/auth/')) {
        auth.logout();
        router.navigate(['/login']);
      } else if (err.status === 0) {
        showToast(toastCtrl, 'Cannot reach the server. Is it running?', 'danger');
      } else if (err.status >= 500) {
        showToast(toastCtrl, 'Server error. Please try again.', 'danger');
      }
      return throwError(() => err);
    })
  );
};

async function showToast(ctrl: ToastController, message: string, color: string) {
  const t = await ctrl.create({ message, color, duration: 2500, position: 'bottom' });
  await t.present();
}
