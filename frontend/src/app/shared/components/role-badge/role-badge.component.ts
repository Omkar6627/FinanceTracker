import { Component, Input } from '@angular/core';
import { MemberRole } from '../../../core/models/api.models';

@Component({
  selector: 'app-role-badge',
  standalone: false,
  template: `<span class="role-badge" [attr.data-role]="role">{{ role }}</span>`,
  styles: [`
    .role-badge {
      display: inline-flex;
      align-items: center;
      padding: 2px 10px;
      border-radius: 6px;
      font-size: 11px;
      font-weight: 600;
      letter-spacing: 0.03em;
      text-transform: uppercase;
    }
    .role-badge[data-role="Owner"]   { background: rgba(99, 102, 241, 0.18); color: #6366f1; }
    .role-badge[data-role="Admin"]   { background: rgba(16, 185, 129, 0.18); color: #10b981; }
    .role-badge[data-role="Manager"] { background: rgba(245, 158, 11, 0.18); color: #f59e0b; }
    .role-badge[data-role="Member"]  { background: rgba(59, 130, 246, 0.18); color: #3b82f6; }
    .role-badge[data-role="Viewer"]  { background: rgba(107, 114, 128, 0.18); color: #6b7280; }
  `],
})
export class RoleBadgeComponent {
  @Input() role: MemberRole = 'Member';
}
