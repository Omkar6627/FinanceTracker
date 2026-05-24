import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'relativeDate', standalone: false })
export class RelativeDatePipe implements PipeTransform {
  transform(value: string | Date | null | undefined): string {
    if (!value) return '';
    const d = typeof value === 'string' ? new Date(value) : value;
    if (isNaN(d.getTime())) return '';
    const now = new Date();
    const day = new Date(d.getFullYear(), d.getMonth(), d.getDate());
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const diffDays = Math.round((today.getTime() - day.getTime()) / 86400000);
    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return 'Yesterday';
    if (diffDays > 0 && diffDays < 7) return `${diffDays} days ago`;
    if (diffDays < 0 && diffDays > -7) return `In ${-diffDays} days`;
    return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: now.getFullYear() === d.getFullYear() ? undefined : 'numeric' });
  }
}
