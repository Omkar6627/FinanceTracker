import { Component, OnInit } from '@angular/core';
import { AlertController, ToastController } from '@ionic/angular';
import { AuthService } from '../../core/auth/auth.service';
import { Permissions } from '../../core/auth/permissions';
import { ApiService } from '../../core/http/api.service';
import { DepartmentDto } from '../../core/models/api.models';

@Component({
  selector: 'app-departments',
  templateUrl: './departments.page.html',
  styleUrls: ['./departments.page.scss'],
  standalone: false,
})
export class DepartmentsPage implements OnInit {
  loading = true;
  items: DepartmentDto[] = [];
  canManage = false;

  constructor(
    private api: ApiService,
    private auth: AuthService,
    private alert: AlertController,
    private toast: ToastController,
  ) {}

  ngOnInit(): void {
    this.canManage = this.auth.can(Permissions.DepartmentManage);
    this.reload();
  }

  ionViewWillEnter(): void { this.reload(); }

  private reload(): void {
    this.loading = true;
    this.api.listDepartments().subscribe({
      next: (d) => { this.items = d; this.loading = false; },
      error: () => { this.loading = false; },
    });
  }

  async createDepartment(): Promise<void> {
    if (!this.canManage) return;
    const a = await this.alert.create({
      header: 'New department',
      inputs: [{ name: 'name', type: 'text', placeholder: 'Department name' }],
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        {
          text: 'Create',
          handler: (data) => {
            const name = (data?.name ?? '').trim();
            if (!name) return false;
            this.api.createDepartment({ name, managerMemberId: null }).subscribe({
              next: async () => {
                (await this.toast.create({ message: 'Department created', duration: 1400, color: 'success' })).present();
                this.reload();
              },
              error: async (err) => {
                const msg = err?.error?.message ?? 'Could not create';
                (await this.toast.create({ message: msg, duration: 1800, color: 'danger' })).present();
              },
            });
            return true;
          },
        },
      ],
    });
    await a.present();
  }

  async rename(d: DepartmentDto): Promise<void> {
    if (!this.canManage) return;
    const a = await this.alert.create({
      header: 'Rename department',
      inputs: [{ name: 'name', type: 'text', value: d.name }],
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        {
          text: 'Save',
          handler: (data) => {
            const name = (data?.name ?? '').trim();
            if (!name || name === d.name) return false;
            this.api.updateDepartment(d.id, { name, managerMemberId: d.managerMemberId ?? null }).subscribe(() => this.reload());
            return true;
          },
        },
      ],
    });
    await a.present();
  }

  async remove(d: DepartmentDto): Promise<void> {
    if (!this.canManage) return;
    const a = await this.alert.create({
      header: 'Delete department?',
      message: `Members in "${d.name}" will be moved out (no data lost).`,
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        {
          text: 'Delete', role: 'destructive',
          handler: () => {
            this.api.deleteDepartment(d.id).subscribe(() => this.reload());
          },
        },
      ],
    });
    await a.present();
  }
}
