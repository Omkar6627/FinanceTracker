import { Component, Input } from '@angular/core';
import { TxStatus } from '../../../core/models/api.models';

@Component({
  selector: 'app-status-chip',
  standalone: false,
  template: `<span class="status-chip" [attr.data-status]="status">{{ label }}</span>`,
  styles: [`
    .status-chip {
      display: inline-flex;
      align-items: center;
      padding: 2px 10px;
      border-radius: 999px;
      font-size: 11px;
      font-weight: 600;
      letter-spacing: 0.02em;
      text-transform: uppercase;
    }
    .status-chip[data-status="PendingApproval"] { background: rgba(245, 158, 11, 0.15); color: #f59e0b; }
    .status-chip[data-status="Approved"]        { background: rgba(16, 185, 129, 0.15); color: #10b981; }
    .status-chip[data-status="AutoApproved"]    { background: rgba(99, 102, 241, 0.15); color: #6366f1; }
    .status-chip[data-status="Rejected"]        { background: rgba(239, 68, 68, 0.15); color: #ef4444; }
    .status-chip[data-status="Draft"]           { background: rgba(107, 114, 128, 0.15); color: #6b7280; }
  `],
})
export class StatusChipComponent {
  @Input() status: TxStatus = 'AutoApproved';

  get label(): string {
    switch (this.status) {
      case 'PendingApproval': return 'Pending';
      case 'AutoApproved':    return 'Auto';
      default:                return this.status;
    }
  }
}
