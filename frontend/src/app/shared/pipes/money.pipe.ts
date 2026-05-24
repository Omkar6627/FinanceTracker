import { Pipe, PipeTransform } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

@Pipe({ name: 'money', standalone: false, pure: false })
export class MoneyPipe implements PipeTransform {
  constructor(private auth: AuthService) {}

  transform(value: number | null | undefined, opts: { sign?: boolean; abs?: boolean } = {}): string {
    if (value === null || value === undefined || isNaN(value)) return '—';
    const currency = this.auth.currentUser()?.currency || 'USD';
    const v = opts.abs ? Math.abs(value) : value;
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
