import { Component, OnInit } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { ApiService } from '../../core/http/api.service';
import {
  BudgetStatus,
  DashboardSummary,
} from '../../core/models/api.models';
import { TransactionFormComponent } from '../../shared/components/transaction-form/transaction-form.component';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.page.html',
  styleUrls: ['./dashboard.page.scss'],
  standalone: false,
})
export class DashboardPage implements OnInit {
  loading = true;
  summary: DashboardSummary | null = null;
  budgets: BudgetStatus[] = [];

  constructor(
    private api: ApiService,
    public auth: AuthService,
    private modal: ModalController,
  ) {}

  ngOnInit(): void {
    this.refresh();
  }

  ionViewWillEnter(): void {
    this.refresh();
  }

  refresh(ev?: CustomEvent): void {
    this.loading = !ev;
    forkJoin({
      summary: this.api.getDashboard(),
      budgets: this.api.getBudgetStatus(),
    }).subscribe({
      next: (r) => {
        this.summary = r.summary;
        this.budgets = r.budgets;
        this.loading = false;
        (ev?.target as HTMLIonRefresherElement | undefined)?.complete();
      },
      error: () => {
        this.loading = false;
        (ev?.target as HTMLIonRefresherElement | undefined)?.complete();
      },
    });
  }

  get topBudgets(): BudgetStatus[] {
    return [...this.budgets]
      .sort((a, b) => b.percentUsed - a.percentUsed)
      .slice(0, 3);
  }

  get greeting(): string {
    const h = new Date().getHours();
    if (h < 12) return 'Good morning';
    if (h < 18) return 'Good afternoon';
    return 'Good evening';
  }

  get firstName(): string {
    return this.auth.currentUser()?.fullName?.split(' ')[0] || 'there';
  }

  async openAdd(): Promise<void> {
    const m = await this.modal.create({
      component: TransactionFormComponent,
      breakpoints: [0, 1],
      initialBreakpoint: 1,
      handle: false,
    });
    await m.present();
    const { role } = await m.onDidDismiss();
    if (role === 'saved') this.refresh();
  }

  budgetRingDashOffset(pct: number, circumference: number): number {
    const clamped = Math.min(100, Math.max(0, pct));
    return circumference * (1 - clamped / 100);
  }

  budgetRingColor(pct: number): string {
    if (pct >= 100) return 'var(--ion-color-danger)';
    if (pct >= 80) return 'var(--ion-color-warning)';
    return 'var(--ion-color-success)';
  }
}
