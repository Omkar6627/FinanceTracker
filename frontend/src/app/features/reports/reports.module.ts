import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { ReportsPage } from './reports.page';

const routes: Routes = [{ path: '', component: ReportsPage }];

@NgModule({
  declarations: [ReportsPage],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class ReportsModule {}
