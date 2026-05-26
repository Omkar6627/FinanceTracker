import { Injectable, signal } from '@angular/core';
import { ApiService } from '../http/api.service';
import { AuthService } from '../auth/auth.service';

const STORAGE_KEY = 'ft.displayCurrency';

/**
 * Display-only multi-currency. Amounts are always stored in the organisation's base
 * currency; this service holds a chosen display currency + live FX rates (relative to
 * base) so MoneyPipe can convert on the fly. Falls back to base currency if rates are
 * unavailable.
 */
@Injectable({ providedIn: 'root' })
export class CurrencyService {
  private readonly _display = signal<string>('');
  private readonly _rates = signal<Record<string, number>>({});

  readonly displayCurrency = this._display.asReadonly();

  constructor(private api: ApiService, private auth: AuthService) {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved) this._display.set(saved);
  }

  base(): string {
    return this.auth.currentUser()?.currency || 'USD';
  }

  /** Currency actually used for display (chosen one, or base if none chosen). */
  effectiveCurrency(): string {
    const d = this._display();
    return d || this.base();
  }

  /** Multiplier to convert a base-currency amount into the display currency. */
  rate(): number {
    const d = this._display();
    if (!d || d === this.base()) return 1;
    return this._rates()[d] ?? 1;
  }

  /** Call once after auth is restored (app boot / login). */
  init(): void {
    const display = this._display() || this.base();
    this._display.set(display);
    if (display !== this.base()) this.loadRates();
  }

  setDisplayCurrency(cur: string): void {
    this._display.set(cur);
    localStorage.setItem(STORAGE_KEY, cur);
    if (cur === this.base()) this._rates.set({});
    else this.loadRates();
  }

  private loadRates(): void {
    this.api.getFxRates(this.base()).subscribe({
      next: (r) => this._rates.set(r.rates || {}),
      error: () => this._rates.set({}),
    });
  }
}
