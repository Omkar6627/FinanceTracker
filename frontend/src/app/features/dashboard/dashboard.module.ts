import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { DashboardPage } from './dashboard.page';

const routes: Routes = [{ path: '', component: DashboardPage }];

@NgModule({
  declarations: [DashboardPage],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class DashboardModule {}
