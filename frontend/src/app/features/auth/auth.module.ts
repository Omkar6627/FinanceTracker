import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { AcceptInvitePage } from './accept-invite/accept-invite.page';
import { LoginPage } from './login/login.page';
import { RegisterPage } from './register/register.page';

const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: LoginPage },
  { path: 'register', component: RegisterPage },
  { path: 'accept-invite', component: AcceptInvitePage },
];

@NgModule({
  declarations: [LoginPage, RegisterPage, AcceptInvitePage],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class AuthModule {}
