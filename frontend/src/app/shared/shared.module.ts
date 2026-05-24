import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '@ionic/angular';
import { RoleBadgeComponent } from './components/role-badge/role-badge.component';
import { StatusChipComponent } from './components/status-chip/status-chip.component';
import { TransactionFormComponent } from './components/transaction-form/transaction-form.component';
import { HasPermissionDirective } from './directives/has-permission.directive';
import { MoneyPipe } from './pipes/money.pipe';
import { RelativeDatePipe } from './pipes/relative-date.pipe';

@NgModule({
  declarations: [
    MoneyPipe,
    RelativeDatePipe,
    TransactionFormComponent,
    StatusChipComponent,
    RoleBadgeComponent,
    HasPermissionDirective,
  ],
  imports: [CommonModule, FormsModule, ReactiveFormsModule, IonicModule],
  exports: [
    CommonModule, FormsModule, ReactiveFormsModule, IonicModule,
    MoneyPipe, RelativeDatePipe, TransactionFormComponent,
    StatusChipComponent, RoleBadgeComponent, HasPermissionDirective,
  ],
})
export class SharedModule {}
