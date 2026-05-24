import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ModalController, ToastController } from '@ionic/angular';
import { ApiService } from '../../../core/http/api.service';
import { Category, CreateBudgetRequest } from '../../../core/models/api.models';

@Component({
  selector: 'app-budget-form',
  templateUrl: './budget-form.component.html',
  styleUrls: ['./budget-form.component.scss'],
  standalone: false,
})
export class BudgetFormComponent implements OnInit {
  loading = true;
  saving = false;
  categories: Category[] = [];

  form = this.fb.nonNullable.group({
    categoryId: ['', [Validators.required]],
    limitAmount: [null as number | null, [Validators.required, Validators.min(0.01)]],
    period: ['Monthly' as 'Weekly' | 'Monthly' | 'Custom', [Validators.required]],
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
        this.categories = cats.filter((c) => c.type === 'Expense');
        this.loading = false;
        if (this.categories.length) this.form.patchValue({ categoryId: this.categories[0].id });
      },
      error: () => (this.loading = false),
    });
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
    const payload: CreateBudgetRequest = {
      categoryId: v.categoryId,
      limitAmount: Number(v.limitAmount),
      period: v.period,
    };
    this.saving = true;
    this.api.createBudget(payload).subscribe({
      next: (b) => {
        this.saving = false;
        this.dismiss('saved', b);
      },
      error: async (err) => {
        this.saving = false;
        const message = err?.error?.message || 'Could not create budget';
        const t = await this.toast.create({ message, duration: 2200, color: 'danger', position: 'top' });
        await t.present();
      },
    });
  }
}
