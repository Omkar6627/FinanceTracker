import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../core/http/api.service';
import { AuditEntry } from '../../core/models/api.models';

@Component({
  selector: 'app-audit',
  templateUrl: './audit.page.html',
  styleUrls: ['./audit.page.scss'],
  standalone: false,
})
export class AuditPage implements OnInit {
  loading = true;
  items: AuditEntry[] = [];
  page = 1;
  pageSize = 50;
  total = 0;
  finished = false;

  constructor(private api: ApiService) {}

  ngOnInit(): void { this.reload(); }
  ionViewWillEnter(): void { this.reload(); }

  private reload(): void {
    this.loading = true;
    this.page = 1;
    this.api.listAudit({ page: 1, pageSize: this.pageSize }).subscribe({
      next: (r) => {
        this.items = r.items;
        this.total = r.total;
        this.finished = this.items.length >= r.total;
        this.loading = false;
      },
      error: () => { this.loading = false; },
    });
  }

  loadMoreClick(): void {
    if (this.finished) return;
    this.page += 1;
    this.api.listAudit({ page: this.page, pageSize: this.pageSize }).subscribe({
      next: (r) => {
        this.items = [...this.items, ...r.items];
        this.finished = this.items.length >= r.total;
      },
    });
  }

  iconFor(action: string): string {
    if (action.startsWith('transaction.approved')) return 'checkmark-circle-outline';
    if (action.startsWith('transaction.rejected')) return 'close-circle-outline';
    if (action.startsWith('transaction.created'))  return 'add-circle-outline';
    if (action.startsWith('transaction.deleted'))  return 'trash-outline';
    if (action.startsWith('member'))               return 'person-outline';
    if (action.startsWith('department'))           return 'business-outline';
    if (action.startsWith('organisation'))         return 'settings-outline';
    return 'information-circle-outline';
  }

  labelFor(action: string): string {
    return action.replace(/^[a-z]+\./, '').replace(/_/g, ' ');
  }
}
