import { Component, OnInit } from '@angular/core';
import { AlertController, ToastController } from '@ionic/angular';
import { ApiService } from '../../core/http/api.service';
import { TransactionItem } from '../../core/models/api.models';

@Component({
  selector: 'app-approvals',
  templateUrl: './approvals.page.html',
  styleUrls: ['./approvals.page.scss'],
  standalone: false,
})
export class ApprovalsPage implements OnInit {
  loading = true;
  busyId: string | null = null;
  items: TransactionItem[] = [];

  constructor(
    private api: ApiService,
    private alert: AlertController,
    private toast: ToastController,
  ) {}

  ngOnInit(): void { this.load(); }
  ionViewWillEnter(): void { this.load(); }

  private load(): void {
    this.loading = true;
    this.api.listPendingTransactions(1, 50).subscribe({
      next: (r) => { this.items = r.items; this.loading = false; },
      error: () => { this.loading = false; },
    });
  }

  approve(t: TransactionItem): void {
    this.busyId = t.id;
    this.api.approveTransaction(t.id).subscribe({
      next: async () => {
        this.busyId = null;
        this.items = this.items.filter((x) => x.id !== t.id);
        (await this.toast.create({ message: 'Approved', duration: 1400, color: 'success' })).present();
      },
      error: async () => {
        this.busyId = null;
        (await this.toast.create({ message: 'Could not approve', duration: 1800, color: 'danger' })).present();
      },
    });
  }

  async reject(t: TransactionItem): Promise<void> {
    const a = await this.alert.create({
      header: 'Reject transaction',
      inputs: [{ name: 'reason', type: 'text', placeholder: 'Reason for rejection' }],
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        {
          text: 'Reject', role: 'destructive',
          handler: (data) => {
            const reason = (data?.reason ?? '').trim();
            if (!reason) return false;
            this.busyId = t.id;
            this.api.rejectTransaction(t.id, { reason }).subscribe({
              next: async () => {
                this.busyId = null;
                this.items = this.items.filter((x) => x.id !== t.id);
                (await this.toast.create({ message: 'Rejected', duration: 1400, color: 'warning' })).present();
              },
              error: async () => {
                this.busyId = null;
                (await this.toast.create({ message: 'Could not reject', duration: 1800, color: 'danger' })).present();
              },
            });
            return true;
          },
        },
      ],
    });
    await a.present();
  }
}
