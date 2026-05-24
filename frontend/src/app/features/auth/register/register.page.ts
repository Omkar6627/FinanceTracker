import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastController } from '@ionic/angular';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.page.html',
  styleUrls: ['../login/login.page.scss', './register.page.scss'],
  standalone: false,
})
export class RegisterPage {
  showPassword = false;
  loading = false;

  readonly currencies = ['USD', 'EUR', 'GBP', 'INR', 'JPY', 'CAD', 'AUD', 'CHF', 'CNY', 'SGD'];

  form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    currency: ['USD', [Validators.required]],
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
    this.auth.register(this.form.getRawValue()).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigateByUrl('/app/dashboard');
      },
      error: async (err) => {
        this.loading = false;
        const message = err?.error?.message || 'Could not create account';
        const t = await this.toast.create({ message, duration: 2500, color: 'danger', position: 'top' });
        await t.present();
      },
    });
  }
}
