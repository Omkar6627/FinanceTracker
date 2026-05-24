import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastController } from '@ionic/angular';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-accept-invite',
  templateUrl: './accept-invite.page.html',
  styleUrls: ['../register/register.page.scss'],
  standalone: false,
})
export class AcceptInvitePage implements OnInit {
  form = this.fb.group({
    fullName: ['', [Validators.required, Validators.minLength(2)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });
  loading = false;
  token = '';

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private toast: ToastController,
  ) {}

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (!this.token) {
      this.router.navigateByUrl('/login');
    }
  }

  submit(): void {
    if (this.form.invalid || this.loading || !this.token) return;
    this.loading = true;
    const v = this.form.value;
    this.auth.acceptInvitation({
      token: this.token,
      fullName: v.fullName!,
      password: v.password!,
    }).subscribe({
      next: async () => {
        this.loading = false;
        (await this.toast.create({ message: 'Welcome aboard!', duration: 1400, color: 'success' })).present();
        this.router.navigateByUrl('/app/dashboard');
      },
      error: async (err) => {
        this.loading = false;
        const msg = err?.error?.message ?? 'Could not accept invitation';
        (await this.toast.create({ message: msg, duration: 2000, color: 'danger' })).present();
      },
    });
  }
}
