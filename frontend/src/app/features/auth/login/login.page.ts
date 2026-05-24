import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastController } from '@ionic/angular';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.page.html',
  styleUrls: ['./login.page.scss'],
  standalone: false,
})
export class LoginPage {
  showPassword = false;
  loading = false;

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
    private toast: ToastController,
  ) {}

  togglePassword() { this.showPassword = !this.showPassword; }

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading = true;
    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigateByUrl('/app/dashboard');
      },
      error: async (err) => {
        this.loading = false;
        const message = err?.error?.message || 'Invalid email or password';
        const t = await this.toast.create({ message, duration: 2500, color: 'danger', position: 'top' });
        await t.present();
      },
    });
  }
}
