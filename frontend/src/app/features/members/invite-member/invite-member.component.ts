import { Component, Input, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ModalController, ToastController } from '@ionic/angular';
import { ApiService } from '../../../core/http/api.service';
import { DepartmentDto, MemberRole } from '../../../core/models/api.models';

@Component({
  selector: 'app-invite-member',
  templateUrl: './invite-member.component.html',
  styleUrls: ['./invite-member.component.scss'],
  standalone: false,
})
export class InviteMemberComponent implements OnInit {
  @Input() departments: DepartmentDto[] = [];

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    role: ['Member' as MemberRole, Validators.required],
    departmentId: [null as string | null],
  });

  roles: MemberRole[] = ['Admin', 'Manager', 'Member', 'Viewer'];
  saving = false;

  constructor(
    private fb: FormBuilder,
    private api: ApiService,
    private modalCtrl: ModalController,
    private toast: ToastController,
  ) {}

  ngOnInit(): void {}

  close(): void { this.modalCtrl.dismiss(undefined, 'cancel'); }

  submit(): void {
    if (this.form.invalid || this.saving) return;
    this.saving = true;
    const v = this.form.value;
    this.api.inviteMember({
      email: v.email!,
      role: v.role as MemberRole,
      departmentId: v.departmentId ?? null,
    }).subscribe({
      next: async (inv) => {
        this.saving = false;
        const link = `${location.origin}/login/accept-invite?token=${encodeURIComponent(inv.token)}`;
        await navigator.clipboard?.writeText(link).catch(() => {});
        (await this.toast.create({ message: 'Invite created · link copied', duration: 1800, color: 'success' })).present();
        this.modalCtrl.dismiss(inv, 'invited');
      },
      error: async (err) => {
        this.saving = false;
        const msg = err?.error?.message ?? 'Could not invite';
        (await this.toast.create({ message: msg, duration: 2000, color: 'danger' })).present();
      },
    });
  }
}
