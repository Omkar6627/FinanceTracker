import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
} from '@angular/core';
import { Chart, registerables } from 'chart.js';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { ApiService } from '../../core/http/api.service';
import { MonthlyReport, TrendReport } from '../../core/models/api.models';

Chart.register(...registerables);

@Component({
  selector: 'app-reports',
  templateUrl: './reports.page.html',
  styleUrls: ['./reports.page.scss'],
  standalone: false,
})
export class ReportsPage implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('trendCanvas') trendCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('pieCanvas') pieCanvas!: ElementRef<HTMLCanvasElement>;

  loading = true;
  monthly: MonthlyReport | null = null;
  trend: TrendReport | null = null;

  private year = new Date().getFullYear();
  private month = new Date().getMonth() + 1;
  private trendChart: Chart | null = null;
  private pieChart: Chart | null = null;
  private viewReady = false;

  constructor(private api: ApiService, private auth: AuthService) {}

  ngOnInit(): void { this.reload(); }
  ngAfterViewInit(): void { this.viewReady = true; this.renderCharts(); }

  ionViewWillEnter(): void { this.reload(); }

  ngOnDestroy(): void {
    this.trendChart?.destroy();
    this.pieChart?.destroy();
  }

  reload(ev?: CustomEvent): void {
    this.loading = !ev;
    forkJoin({
      monthly: this.api.getMonthly(this.year, this.month),
      trend: this.api.getTrends(6),
    }).subscribe({
      next: (r) => {
        this.monthly = r.monthly;
        this.trend = r.trend;
        this.loading = false;
        (ev?.target as HTMLIonRefresherElement | undefined)?.complete();
        setTimeout(() => this.renderCharts(), 0);
      },
      error: () => {
        this.loading = false;
        (ev?.target as HTMLIonRefresherElement | undefined)?.complete();
      },
    });
  }

  get monthName(): string {
    return new Date(this.year, this.month - 1, 1).toLocaleDateString(undefined, {
      month: 'long', year: 'numeric',
    });
  }

  get currency(): string {
    return this.auth.currentUser()?.currency || 'USD';
  }

  private renderCharts(): void {
    if (!this.viewReady) return;
    if (this.trend && this.trendCanvas) this.renderTrend();
    if (this.monthly && this.pieCanvas) this.renderPie();
  }

  private renderTrend(): void {
    this.trendChart?.destroy();
    const points = this.trend!.points;
    const labels = points.map((p) =>
      new Date(p.year, p.month - 1, 1).toLocaleDateString(undefined, { month: 'short' }),
    );
    this.trendChart = new Chart(this.trendCanvas.nativeElement, {
      type: 'bar',
      data: {
        labels,
        datasets: [
          { label: 'Income',  data: points.map((p) => p.income),  backgroundColor: '#10b981', borderRadius: 8, barPercentage: 0.6 },
          { label: 'Expense', data: points.map((p) => p.expense), backgroundColor: '#ef4444', borderRadius: 8, barPercentage: 0.6 },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { position: 'bottom', labels: { boxWidth: 12, padding: 16, font: { size: 12 } } },
          tooltip: {
            callbacks: {
              label: (ctx) => `${ctx.dataset.label}: ${new Intl.NumberFormat(undefined, { style: 'currency', currency: this.currency }).format(Number(ctx.parsed.y ?? 0))}`,
            },
          },
        },
        scales: {
          x: { grid: { display: false } },
          y: {
            ticks: { callback: (v) => new Intl.NumberFormat(undefined, { notation: 'compact' }).format(Number(v)) },
            grid: { color: 'rgba(148, 163, 184, 0.15)' },
          },
        },
      },
    });
  }

  private renderPie(): void {
    this.pieChart?.destroy();
    const slices = this.monthly!.expenseByCategory;
    if (!slices.length) return;
    this.pieChart = new Chart(this.pieCanvas.nativeElement, {
      type: 'doughnut',
      data: {
        labels: slices.map((s) => s.categoryName),
        datasets: [
          {
            data: slices.map((s) => s.amount),
            backgroundColor: slices.map((s) => s.categoryColor),
            borderWidth: 0,
            hoverOffset: 8,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        cutout: '65%',
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              label: (ctx) => {
                const value = Number(ctx.parsed);
                return `${ctx.label}: ${new Intl.NumberFormat(undefined, { style: 'currency', currency: this.currency }).format(value)}`;
              },
            },
          },
        },
      },
    });
  }

  changeMonth(delta: number): void {
    let m = this.month + delta;
    let y = this.year;
    while (m < 1) { m += 12; y -= 1; }
    while (m > 12) { m -= 12; y += 1; }
    this.month = m;
    this.year = y;
    this.reload();
  }
}
