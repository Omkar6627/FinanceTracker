import { Component, OnInit } from '@angular/core';
import { AlertController, ModalController, ToastController } from '@ionic/angular';
import { ApiService } from '../../core/http/api.service';
import { RecurringRule } from '../../core/models/api.models';
import { RecurringFormComponent } from './recurring-form/recurring-form.component';

@Component({
  selector: 'app-recurring',
  templateUrl: './recurring.page.html',
  styleUrls: ['./recurring.page.scss'],
  standalone: false,
})
export class RecurringPage implements OnInit {
  loading = true;
  rules: RecurringRule[] = [];
  busyId: string | null = null;

  constructor(
    private api: ApiService,
    private modalCtrl: ModalController,
    private alertCtrl: AlertController,
    private toastCtrl: ToastController,
  ) {}

  ngOnInit(): void { this.reload(); }
  ionViewWillEnter(): void { this.reload(); }

  reload(ev?: CustomEvent): void {
    this.loading = !ev;
    this.api.listRecurring().subscribe({
      next: (r) => {
        this.rules = r;
        this.loading = false;
        (ev?.target as HTMLIonRefresherElement | undefined)?.complete();
      },
      error: () => {
        this.loading = false;
        (ev?.target as HTMLIonRefresherElement | undefined)?.complete();
      },
    });
  }

  async openAdd(): Promise<void> {
    const m = await this.modalCtrl.create({
      component: RecurringFormComponent,
      breakpoints: [0, 1],
      initialBreakpoint: 1,
      handle: false,
    });
    await m.present();
    const { role } = await m.onDidDismiss();
    if (role === 'saved') this.reload();
  }

  runNow(rule: RecurringRule): void {
    this.busyId = rule.id;
    this.api.runRecurring(rule.id).subscribe({
      next: () => {
        this.busyId = null;
        this.toast('Transaction created', 'success');
        this.reload();
      },
      error: (e) => {
        this.busyId = null;
        this.toast(e?.error?.message || 'Could not run rule', 'danger');
      },
    });
  }

  togglePause(rule: RecurringRule): void {
    this.busyId = rule.id;
    this.api.updateRecurring(rule.id, {
      categoryId: rule.categoryId,
      accountId: rule.accountId ?? null,
      amount: rule.amount,
      type: rule.type,
      note: rule.note,
      frequency: rule.frequency,
      endDate: rule.endDate ?? null,
      isActive: !rule.isActive,
    }).subscribe({
      next: () => {
        this.busyId = null;
        this.toast(rule.isActive ? 'Paused' : 'Resumed', 'success');
        this.reload();
      },
      error: () => {
        this.busyId = null;
        this.toast('Could not update rule', 'danger');
      },
    });
  }

  async confirmDelete(rule: RecurringRule): Promise<void> {
    const a = await this.alertCtrl.create({
      header: 'Delete recurring rule?',
      message: `Stop the recurring ${rule.categoryName} ${rule.type.toLowerCase()}? Existing transactions are kept.`,
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        {
          text: 'Delete', role: 'destructive',
          handler: () => this.api.deleteRecurring(rule.id).subscribe(() => this.reload()),
        },
      ],
    });
    await a.present();
  }

  private async toast(message: string, color: 'success' | 'danger'): Promise<void> {
    const t = await this.toastCtrl.create({ message, duration: 2200, color, position: 'bottom' });
    await t.present();
  }
}
