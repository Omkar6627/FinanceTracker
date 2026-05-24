import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { SettingsPage } from './settings.page';

const routes: Routes = [{ path: '', component: SettingsPage }];

@NgModule({
  declarations: [SettingsPage],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class SettingsModule {}
