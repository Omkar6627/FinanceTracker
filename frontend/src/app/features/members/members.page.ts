import { Component, OnInit } from '@angular/core';
import { AlertController, ModalController, ToastController } from '@ionic/angular';
import { AuthService } from '../../core/auth/auth.service';
import { Permissions } from '../../core/auth/permissions';
import { ApiService } from '../../core/http/api.service';
import {
  DepartmentDto,
  InvitationDto,
  MemberDto,
  MemberRole,
} from '../../core/models/api.models';
import { InviteMemberComponent } from './invite-member/invite-member.component';

@Component({
  selector: 'app-members',
  templateUrl: './members.page.html',
  styleUrls: ['./members.page.scss'],
  standalone: false,
})
export class MembersPage implements OnInit {
  loading = true;
  members: MemberDto[] = [];
  invitations: InvitationDto[] = [];
  departments: DepartmentDto[] = [];
  canInvite = false;
  canManage = false;

  constructor(
    private api: ApiService,
    private auth: AuthService,
    private modalCtrl: ModalController,
    private alert: AlertController,
    private toast: ToastController,
  ) {}

  ngOnInit(): void {
    this.canInvite = this.auth.can(Permissions.MemberInvite);
    this.canManage = this.auth.can(Permissions.MemberManage);
    this.reload();
  }

  ionViewWillEnter(): void { this.reload(); }

  private reload(): void {
    this.loading = true;
    this.api.listMembers().subscribe({
      next: (m) => { this.members = m; this.loading = false; },
      error: () => { this.loading = false; },
    });
    this.api.listDepartments().subscribe({ next: (d) => (this.departments = d) });
    if (this.canInvite) {
      this.api.listInvitations().subscribe({ next: (inv) => (this.invitations = inv) });
    }
  }

  async openInvite(): Promise<void> {
    const m = await this.modalCtrl.create({
      component: InviteMemberComponent,
      componentProps: { departments: this.departments },
      breakpoints: [0, 1],
      initialBreakpoint: 1,
      handle: false,
    });
    await m.present();
    const { role } = await m.onDidDismiss();
    if (role === 'invited') this.reload();
  }

  async changeRole(member: MemberDto): Promise<void> {
    if (!this.canManage || member.role === 'Owner') return;
    const inputs = (['Admin', 'Manager', 'Member', 'Viewer'] as MemberRole[]).map((r) => ({
      type: 'radio' as const,
      label: r,
      value: r,
      checked: member.role === r,
    }));
    const a = await this.alert.create({
      header: `Change role for ${member.fullName}`,
      inputs,
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        {
          text: 'Save',
          handler: (newRole: MemberRole) => {
            if (!newRole || newRole === member.role) return;
            this.api.changeMemberRole(member.id, { role: newRole, departmentId: member.departmentId ?? null }).subscribe({
              next: async () => {
                (await this.toast.create({ message: 'Role updated', duration: 1400, color: 'success' })).present();
                this.reload();
              },
            });
          },
        },
      ],
    });
    await a.present();
  }

  async remove(member: MemberDto): Promise<void> {
    if (!this.canManage || member.role === 'Owner') return;
    const a = await this.alert.create({
      header: 'Remove member?',
      message: `${member.fullName} will lose access.`,
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        {
          text: 'Remove', role: 'destructive',
          handler: () => {
            this.api.removeMember(member.id).subscribe(() => this.reload());
          },
        },
      ],
    });
    await a.present();
  }

  async revoke(inv: InvitationDto): Promise<void> {
    this.api.revokeInvitation(inv.id).subscribe({
      next: async () => {
        (await this.toast.create({ message: 'Invitation revoked', duration: 1400 })).present();
        this.reload();
      },
    });
  }

  copyLink(inv: InvitationDto): void {
    const link = `${location.origin}/login/accept-invite?token=${encodeURIComponent(inv.token)}`;
    navigator.clipboard?.writeText(link);
    this.toast.create({ message: 'Invite link copied', duration: 1400 }).then((t) => t.present());
  }
}
