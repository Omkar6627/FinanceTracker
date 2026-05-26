import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ModalController, ToastController } from '@ionic/angular';
import { ApiService } from '../../../core/http/api.service';
import { Category, CreateRecurringRule, RecurrenceFrequency } from '../../../core/models/api.models';

@Component({
  selector: 'app-recurring-form',
  templateUrl: './recurring-form.component.html',
  styleUrls: ['./recurring-form.component.scss'],
  standalone: false,
})
export class RecurringFormComponent implements OnInit {
  loading = true;
  saving = false;
  allCategories: Category[] = [];
  categories: Category[] = [];

  readonly frequencies: RecurrenceFrequency[] = ['Daily', 'Weekly', 'Monthly'];

  form = this.fb.nonNullable.group({
    type: ['Expense' as 'Income' | 'Expense', [Validators.required]],
    categoryId: ['', [Validators.required]],
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
    frequency: ['Monthly' as RecurrenceFrequency, [Validators.required]],
    startDate: [new Date().toISOString(), [Validators.required]],
    note: [''],
  });

  constructor(
    private fb: FormBuilder,
    private api: ApiService,
    private modalCtrl: ModalController,
    private toast: ToastController,
  ) {}

  ngOnInit(): void {
    this.api.listCategories().subscribe({
      next: (cats) => {
        this.allCategories = cats;
        this.applyTypeFilter();
        this.loading = false;
      },
      error: () => (this.loading = false),
    });
  }

  setType(t: 'Income' | 'Expense'): void {
    this.form.controls.type.setValue(t);
    this.applyTypeFilter();
  }

  private applyTypeFilter(): void {
    this.categories = this.allCategories.filter((c) => c.type === this.form.controls.type.value);
    const current = this.form.controls.categoryId.value;
    if (!this.categories.some((c) => c.id === current)) {
      this.form.controls.categoryId.setValue(this.categories[0]?.id ?? '');
    }
  }

  dismiss(role: 'cancel' | 'saved' = 'cancel', data?: unknown): void {
    this.modalCtrl.dismiss(data, role);
  }

  async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const payload: CreateRecurringRule = {
      categoryId: v.categoryId,
      amount: Number(v.amount),
      type: v.type,
      frequency: v.frequency,
      startDate: v.startDate,
      note: v.note?.trim() || null,
    };
    this.saving = true;
    this.api.createRecurring(payload).subscribe({
      next: (r) => {
        this.saving = false;
        this.dismiss('saved', r);
      },
      error: async (err) => {
        this.saving = false;
        const message = err?.error?.message || 'Could not create recurring rule';
        const t = await this.toast.create({ message, duration: 2200, color: 'danger', position: 'top' });
        await t.present();
      },
    });
  }
}
