import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { RecurringFormComponent } from './recurring-form/recurring-form.component';
import { RecurringPage } from './recurring.page';

const routes: Routes = [{ path: '', component: RecurringPage }];

@NgModule({
  declarations: [RecurringPage, RecurringFormComponent],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class RecurringModule {}
