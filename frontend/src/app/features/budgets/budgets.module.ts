import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { BudgetFormComponent } from './budget-form/budget-form.component';
import { BudgetsPage } from './budgets.page';

const routes: Routes = [{ path: '', component: BudgetsPage }];

@NgModule({
  declarations: [BudgetsPage, BudgetFormComponent],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class BudgetsModule {}
