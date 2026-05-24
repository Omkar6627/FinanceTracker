import { NgModule } from '@angular/core';
import { PreloadAllModules, RouterModule, Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guard';

const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'app/dashboard' },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadChildren: () => import('./features/auth/auth.module').then((m) => m.AuthModule)
  },
  {
    path: 'app',
    canActivate: [authGuard],
    loadChildren: () => import('./shell/shell.module').then((m) => m.ShellModule)
  },
  { path: '**', redirectTo: 'app/dashboard' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { preloadingStrategy: PreloadAllModules })],
  exports: [RouterModule]
})
export class AppRoutingModule {}
