import { Component, OnInit } from '@angular/core';
import { AlertController, ModalController } from '@ionic/angular';
import { ApiService } from '../../core/http/api.service';
import { BudgetStatus } from '../../core/models/api.models';
import { BudgetFormComponent } from './budget-form/budget-form.component';

@Component({
  selector: 'app-budgets',
  templateUrl: './budgets.page.html',
  styleUrls: ['./budgets.page.scss'],
  standalone: false,
})
export class BudgetsPage implements OnInit {
  loading = true;
  budgets: BudgetStatus[] = [];

  constructor(
    private api: ApiService,
    private modal: ModalController,
    private alert: AlertController,
  ) {}

  ngOnInit(): void { this.reload(); }
  ionViewWillEnter(): void { this.reload(); }

  reload(ev?: CustomEvent): void {
    this.loading = !ev;
    this.api.getBudgetStatus().subscribe({
      next: (b) => {
        this.budgets = b;
        this.loading = false;
        (ev?.target as HTMLIonRefresherElement | undefined)?.complete();
      },
      error: () => {
        this.loading = false;
        (ev?.target as HTMLIonRefresherElement | undefined)?.complete();
      },
    });
  }

  get totalLimit(): number {
    return this.budgets.reduce((s, b) => s + b.limitAmount, 0);
  }

  get totalSpent(): number {
    return this.budgets.reduce((s, b) => s + b.spentAmount, 0);
  }

  get totalPct(): number {
    return this.totalLimit > 0 ? (this.totalSpent / this.totalLimit) * 100 : 0;
  }

  barColor(pct: number): string {
    if (pct >= 100) return 'var(--ion-color-danger)';
    if (pct >= 80) return 'var(--ion-color-warning)';
    return 'var(--ion-color-success)';
  }

  async openAdd(): Promise<void> {
    const m = await this.modal.create({
      component: BudgetFormComponent,
      breakpoints: [0, 1],
      initialBreakpoint: 1,
      handle: false,
    });
    await m.present();
    const { role } = await m.onDidDismiss();
    if (role === 'saved') this.reload();
  }

  async confirmDelete(b: BudgetStatus): Promise<void> {
    const a = await this.alert.create({
      header: 'Delete budget?',
      message: `Stop tracking the ${b.categoryName} budget?`,
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        {
          text: 'Delete', role: 'destructive',
          handler: () => {
            this.api.deleteBudget(b.id).subscribe(() => this.reload());
          },
        },
      ],
    });
    await a.present();
  }
}
