import { Pipe, PipeTransform } from '@angular/core';
import { CurrencyService } from '../../core/currency/currency.service';

@Pipe({ name: 'money', standalone: false, pure: false })
export class MoneyPipe implements PipeTransform {
  constructor(private currency: CurrencyService) {}

  transform(value: number | null | undefined, opts: { sign?: boolean; abs?: boolean } = {}): string {
    if (value === null || value === undefined || isNaN(value)) return '—';
    const currency = this.currency.effectiveCurrency();
    const rate = this.currency.rate();
    const v = (opts.abs ? Math.abs(value) : value) * rate;
    const formatted = new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency,
      maximumFractionDigits: 2,
      minimumFractionDigits: 2,
    }).format(v);
    if (opts.sign && value > 0) return `+${formatted}`;
    return formatted;
  }
}
