import { Component, Input, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ModalController, ToastController } from '@ionic/angular';
import { ApiService } from '../../../core/http/api.service';
import {
  Category,
  CreateTransactionRequest,
  TransactionDetail,
  TxType,
} from '../../../core/models/api.models';

@Component({
  selector: 'app-transaction-form',
  templateUrl: './transaction-form.component.html',
  styleUrls: ['./transaction-form.component.scss'],
  standalone: false,
})
export class TransactionFormComponent implements OnInit {
  @Input() existing: TransactionDetail | null = null;

  loadingCategories = true;
  saving = false;
  type: TxType = 'Expense';
  categories: Category[] = [];

  form = this.fb.nonNullable.group({
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
    categoryId: ['', [Validators.required]],
    date: [new Date().toISOString(), [Validators.required]],
    note: [''],
  });

  constructor(
    private fb: FormBuilder,
    private api: ApiService,
    private modalCtrl: ModalController,
    private toast: ToastController,
  ) {}

  get filteredCategories(): Category[] {
    return this.categories.filter((c) => c.type === this.type);
  }

  ngOnInit(): void {
    if (this.existing) {
      this.type = this.existing.type === 'Income' ? 'Income' : 'Expense';
      this.form.patchValue({
        amount: this.existing.amount,
        categoryId: this.existing.categoryId,
        date: this.existing.date,
        note: this.existing.note,
      });
    }
    this.api.listCategories().subscribe({
      next: (cats) => {
        this.categories = cats;
        this.loadingCategories = false;
        if (!this.existing && !this.form.controls.categoryId.value) {
          const first = this.filteredCategories[0];
          if (first) this.form.patchValue({ categoryId: first.id });
        }
      },
      error: () => (this.loadingCategories = false),
    });
  }

  setType(t: TxType): void {
    this.type = t;
    const first = this.filteredCategories[0];
    this.form.patchValue({ categoryId: first ? first.id : '' });
  }

  pickCategory(id: string): void {
    this.form.patchValue({ categoryId: id });
  }

  dismiss(role: 'cancel' | 'saved' | 'deleted' = 'cancel', data?: unknown): void {
    this.modalCtrl.dismiss(data, role);
  }

  async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const payload: CreateTransactionRequest = {
      amount: Number(v.amount),
      categoryId: v.categoryId,
      type: this.type,
      date: v.date,
      note: v.note || null,
    };
    this.saving = true;
    const obs = this.existing
      ? this.api.updateTransaction(this.existing.id, payload)
      : this.api.createTransaction(payload);
    obs.subscribe({
      next: (saved) => {
        this.saving = false;
        this.dismiss('saved', saved);
      },
      error: async (err) => {
        this.saving = false;
        const message = err?.error?.message || 'Could not save transaction';
        const t = await this.toast.create({ message, duration: 2200, color: 'danger', position: 'top' });
        await t.present();
      },
    });
  }

  async delete(): Promise<void> {
    if (!this.existing) return;
    this.saving = true;
    this.api.deleteTransaction(this.existing.id).subscribe({
      next: () => {
        this.saving = false;
        this.dismiss('deleted', { id: this.existing!.id });
      },
      error: async () => {
        this.saving = false;
        const t = await this.toast.create({ message: 'Could not delete', duration: 2000, color: 'danger', position: 'top' });
        await t.present();
      },
    });
  }
}
