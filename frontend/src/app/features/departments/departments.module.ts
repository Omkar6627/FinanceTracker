import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { DepartmentsPage } from './departments.page';

const routes: Routes = [{ path: '', component: DepartmentsPage }];

@NgModule({
  declarations: [DepartmentsPage],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class DepartmentsModule {}
