import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../shared/shared.module';
import { ShellPage } from './shell.page';

const routes: Routes = [
  {
    path: '',
    component: ShellPage,
    children: [
      {
        path: 'dashboard',
        loadChildren: () =>
          import('../features/dashboard/dashboard.module').then((m) => m.DashboardModule),
      },
      {
        path: 'transactions',
        loadChildren: () =>
          import('../features/transactions/transactions.module').then((m) => m.TransactionsModule),
      },
      {
        path: 'budgets',
        loadChildren: () =>
          import('../features/budgets/budgets.module').then((m) => m.BudgetsModule),
      },
      {
        path: 'reports',
        loadChildren: () =>
          import('../features/reports/reports.module').then((m) => m.ReportsModule),
      },
      {
        path: 'settings',
        loadChildren: () =>
          import('../features/settings/settings.module').then((m) => m.SettingsModule),
      },
      {
        path: 'approvals',
        loadChildren: () =>
          import('../features/approvals/approvals.module').then((m) => m.ApprovalsModule),
      },
      {
        path: 'members',
        loadChildren: () =>
          import('../features/members/members.module').then((m) => m.MembersModule),
      },
      {
        path: 'departments',
        loadChildren: () =>
          import('../features/departments/departments.module').then((m) => m.DepartmentsModule),
      },
      {
        path: 'audit',
        loadChildren: () =>
          import('../features/audit/audit.module').then((m) => m.AuditModule),
      },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
    ],
  },
];

@NgModule({
  declarations: [ShellPage],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class ShellModule {}
