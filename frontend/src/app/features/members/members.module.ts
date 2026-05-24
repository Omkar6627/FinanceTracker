import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { InviteMemberComponent } from './invite-member/invite-member.component';
import { MembersPage } from './members.page';

const routes: Routes = [{ path: '', component: MembersPage }];

@NgModule({
  declarations: [MembersPage, InviteMemberComponent],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class MembersModule {}
