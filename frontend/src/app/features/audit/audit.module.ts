import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { AuditPage } from './audit.page';

const routes: Routes = [{ path: '', component: AuditPage }];

@NgModule({
  declarations: [AuditPage],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class AuditModule {}
