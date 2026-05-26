import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AlertController, ToastController } from '@ionic/angular';
import { AuthService } from '../../core/auth/auth.service';
import { CurrencyService } from '../../core/currency/currency.service';
import { ApiService } from '../../core/http/api.service';

const THEME_KEY = 'ft.theme';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.page.html',
  styleUrls: ['./settings.page.scss'],
  standalone: false,
})
export class SettingsPage implements OnInit {
  dark = false;
  switching = false;

  readonly currencyOptions = ['USD', 'EUR', 'GBP', 'INR', 'JPY', 'AUD', 'CAD', 'SGD', 'AED', 'CHF'];

  constructor(
    public auth: AuthService,
    public currency: CurrencyService,
    private router: Router,
    private alert: AlertController,
    private api: ApiService,
    private toast: ToastController,
  ) {}

  get displayCurrency(): string {
    return this.currency.effectiveCurrency();
  }

  get currencyList(): string[] {
    const base = this.currency.base();
    return this.currencyOptions.includes(base) ? this.currencyOptions : [base, ...this.currencyOptions];
  }

  onDisplayCurrencyChange(cur: string): void {
    if (cur && cur !== this.currency.effectiveCurrency()) {
      this.currency.setDisplayCurrency(cur);
    }
  }

  ngOnInit(): void {
    const saved = localStorage.getItem(THEME_KEY);
    const prefersDark = window.matchMedia?.('(prefers-color-scheme: dark)').matches;
    this.dark = saved ? saved === 'dark' : prefersDark;
    this.applyTheme();
  }

  toggleDark(): void {
    this.dark = !this.dark;
    localStorage.setItem(THEME_KEY, this.dark ? 'dark' : 'light');
    this.applyTheme();
  }

  private applyTheme(): void {
    document.body.classList.toggle('dark', this.dark);
  }

  async logout(): Promise<void> {
    const a = await this.alert.create({
      header: 'Sign out?',
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        {
          text: 'Sign out', role: 'destructive',
          handler: () => {
            this.auth.logout();
            this.router.navigateByUrl('/login');
          },
        },
      ],
    });
    await a.present();
  }

  async switchMode(): Promise<void> {
    if (this.switching) return;
    const target = this.auth.isEnterprise ? 'Individual' : 'Enterprise';
    const a = await this.alert.create({
      header: `Switch to ${target}?`,
      message: this.auth.isEnterprise
        ? 'All non-owner members must be removed first. Existing transactions are kept.'
        : 'Approvals, departments, and member invites will be unlocked. You stay as Owner.',
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        {
          text: `Switch to ${target}`,
          handler: () => { this.doSwitch(target); },
        },
      ],
    });
    await a.present();
  }

  private doSwitch(target: 'Individual' | 'Enterprise'): void {
    this.switching = true;
    this.api.switchOrganisationMode({ mode: target }).subscribe({
      next: async (org) => {
        this.auth.updateUser({ organisationMode: org.mode });
        // Force a refresh so the JWT carries the new mode in its claims.
        this.auth.refresh().subscribe({
          next: async () => {
            this.switching = false;
            (await this.toast.create({ message: `Now in ${target} mode`, duration: 1600, color: 'success' })).present();
          },
          error: async () => {
            this.switching = false;
            (await this.toast.create({ message: 'Mode switched (sign out + back in to refresh role)', duration: 2000 })).present();
          },
        });
      },
      error: async (err) => {
        this.switching = false;
        const msg = err?.error?.message ?? 'Could not switch mode';
        (await this.toast.create({ message: msg, duration: 2200, color: 'danger' })).present();
      },
    });
  }
}
