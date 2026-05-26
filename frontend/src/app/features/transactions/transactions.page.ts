import { Component, OnInit } from '@angular/core';
import { AlertController, ModalController, ToastController } from '@ionic/angular';
import { ApiService } from '../../core/http/api.service';
import {
  Category,
  CsvImportResult,
  TransactionItem,
  TxType,
} from '../../core/models/api.models';
import { TransactionFormComponent } from '../../shared/components/transaction-form/transaction-form.component';

interface TxGroup { label: string; items: TransactionItem[]; total: number; }

@Component({
  selector: 'app-transactions',
  templateUrl: './transactions.page.html',
  styleUrls: ['./transactions.page.scss'],
  standalone: false,
})
export class TransactionsPage implements OnInit {
  loading = true;
  loadingMore = false;
  finished = false;

  items: TransactionItem[] = [];
  categories: Category[] = [];
  groups: TxGroup[] = [];

  filterType: TxType | 'All' = 'All';
  filterCategoryId: string | null = null;

  page = 1;
  pageSize = 30;
  total = 0;

  importing = false;

  constructor(
    private api: ApiService,
    private modalCtrl: ModalController,
    private alertCtrl: AlertController,
    private toastCtrl: ToastController,
  ) {}

  ngOnInit(): void {
    this.api.listCategories().subscribe((c) => (this.categories = c));
    this.reload();
  }

  ionViewWillEnter(): void {
    this.reload();
  }

  reload(ev?: CustomEvent): void {
    this.page = 1;
    this.items = [];
    this.groups = [];
    this.finished = false;
    this.loading = !ev;
    this.fetch(() => {
      (ev?.target as HTMLIonRefresherElement | undefined)?.complete();
    });
  }

  loadMore(ev: CustomEvent): void {
    if (this.finished) {
      (ev.target as HTMLIonInfiniteScrollElement).complete();
      return;
    }
    this.page += 1;
    this.loadingMore = true;
    this.fetch(() => {
      this.loadingMore = false;
      (ev.target as HTMLIonInfiniteScrollElement).complete();
    });
  }

  private fetch(done?: () => void): void {
    this.api
      .listTransactions({
        page: this.page,
        pageSize: this.pageSize,
        type: this.filterType === 'All' ? undefined : this.filterType,
        categoryId: this.filterCategoryId ?? undefined,
      })
      .subscribe({
        next: (r) => {
          this.items = this.page === 1 ? r.items : [...this.items, ...r.items];
          this.total = r.total;
          this.finished = this.items.length >= r.total;
          this.groups = this.buildGroups(this.items);
          this.loading = false;
          done?.();
        },
        error: () => {
          this.loading = false;
          done?.();
        },
      });
  }

  setType(t: TxType | 'All'): void {
    this.filterType = t;
    this.reload();
  }

  setCategory(id: string | null): void {
    this.filterCategoryId = id;
    this.reload();
  }

  private buildGroups(items: TransactionItem[]): TxGroup[] {
    const map = new Map<string, TxGroup>();
    for (const t of items) {
      const d = new Date(t.date);
      const key = d.toISOString().slice(0, 10);
      let g = map.get(key);
      if (!g) {
        g = { label: this.formatGroup(d), items: [], total: 0 };
        map.set(key, g);
      }
      g.items.push(t);
      g.total += t.type === 'Income' ? t.amount : t.type === 'Expense' ? -t.amount : 0;
    }
    return Array.from(map.entries())
      .sort((a, b) => b[0].localeCompare(a[0]))
      .map(([, g]) => g);
  }

  private formatGroup(d: Date): string {
    const now = new Date();
    const day = new Date(d.getFullYear(), d.getMonth(), d.getDate());
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const diff = Math.round((today.getTime() - day.getTime()) / 86400000);
    if (diff === 0) return 'Today';
    if (diff === 1) return 'Yesterday';
    if (diff < 7) return d.toLocaleDateString(undefined, { weekday: 'long' });
    return d.toLocaleDateString(undefined, { month: 'long', day: 'numeric', year: now.getFullYear() === d.getFullYear() ? undefined : 'numeric' });
  }

  async openAdd(): Promise<void> {
    const m = await this.modalCtrl.create({
      component: TransactionFormComponent,
      breakpoints: [0, 1],
      initialBreakpoint: 1,
      handle: false,
    });
    await m.present();
    const { role } = await m.onDidDismiss();
    if (role === 'saved') this.reload();
  }

  exportCsv(): void {
    this.api
      .exportTransactionsCsv({
        type: this.filterType === 'All' ? undefined : this.filterType,
        categoryId: this.filterCategoryId ?? undefined,
      })
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `transactions-${new Date().toISOString().slice(0, 10)}.csv`;
          a.click();
          URL.revokeObjectURL(url);
        },
        error: () => this.toast('Export failed', 'danger'),
      });
  }

  onImportFile(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = ''; // allow re-selecting the same file later
    if (!file) return;

    this.importing = true;
    this.api.importTransactionsCsv(file, false).subscribe({
      next: (preview) => {
        this.importing = false;
        this.confirmImport(file, preview);
      },
      error: (e) => {
        this.importing = false;
        this.toast(e?.error?.message || 'Could not read CSV', 'danger');
      },
    });
  }

  private async confirmImport(file: File, preview: CsvImportResult): Promise<void> {
    const valid = preview.totalRows - preview.failed;
    const errorLines = preview.errors.slice(0, 5).map((e) => `Row ${e.row}: ${e.message}`).join('\n');
    const more = preview.errors.length > 5 ? `\n…and ${preview.errors.length - 5} more` : '';

    const alert = await this.alertCtrl.create({
      header: 'Import preview',
      cssClass: 'csv-import-alert',
      message:
        `${valid} of ${preview.totalRows} row(s) ready to import.` +
        (preview.failed ? `\n\n${preview.failed} skipped:\n${errorLines}${more}` : ''),
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        {
          text: valid > 0 ? `Import ${valid}` : 'Close',
          role: valid > 0 ? 'confirm' : 'cancel',
          handler: () => {
            if (valid > 0) this.commitImport(file);
          },
        },
      ],
    });
    await alert.present();
  }

  private commitImport(file: File): void {
    this.importing = true;
    this.api.importTransactionsCsv(file, true).subscribe({
      next: (r) => {
        this.importing = false;
        this.toast(`Imported ${r.imported} transaction(s)`, 'success');
        this.reload();
      },
      error: () => {
        this.importing = false;
        this.toast('Import failed', 'danger');
      },
    });
  }

  private async toast(message: string, color: 'success' | 'danger'): Promise<void> {
    const t = await this.toastCtrl.create({ message, duration: 2500, color, position: 'bottom' });
    await t.present();
  }

  async openEdit(t: TransactionItem): Promise<void> {
    this.api.getTransaction(t.id).subscribe(async (detail) => {
      const m = await this.modalCtrl.create({
        component: TransactionFormComponent,
        componentProps: { existing: detail },
        breakpoints: [0, 1],
        initialBreakpoint: 1,
        handle: false,
      });
      await m.present();
      const { role } = await m.onDidDismiss();
      if (role === 'saved' || role === 'deleted') this.reload();
    });
  }
}
