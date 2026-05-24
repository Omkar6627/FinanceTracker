import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { TransactionsPage } from './transactions.page';

const routes: Routes = [{ path: '', component: TransactionsPage }];

@NgModule({
  declarations: [TransactionsPage],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class TransactionsModule {}
