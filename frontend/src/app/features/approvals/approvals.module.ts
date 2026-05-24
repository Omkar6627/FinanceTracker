import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { ApprovalsPage } from './approvals.page';

const routes: Routes = [{ path: '', component: ApprovalsPage }];

@NgModule({
  declarations: [ApprovalsPage],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class ApprovalsModule {}
