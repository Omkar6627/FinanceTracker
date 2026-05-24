import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { NgModule, isDevMode } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { ServiceWorkerModule } from '@angular/service-worker';
import { IonicModule, IonicRouteStrategy } from '@ionic/angular';
import { RouteReuseStrategy } from '@angular/router';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { errorInterceptor } from './core/http/error.interceptor';
import { jwtInterceptor } from './core/http/jwt.interceptor';

import { addIcons } from 'ionicons';
import {
  addOutline, addCircleOutline, airplaneOutline, alertCircleOutline, analyticsOutline,
  arrowDownOutline, arrowUpOutline, bagOutline, basketOutline, briefcaseOutline, businessOutline,
  calendarOutline, carOutline, cardOutline, cashOutline, checkmarkDoneOutline,
  chevronForwardOutline, closeOutline, closeCircleOutline, createOutline, documentTextOutline,
  ellipsisHorizontal, filmOutline, gridOutline, homeOutline, informationCircleOutline,
  linkOutline, listOutline, lockClosedOutline, logOutOutline, mailOutline, medkitOutline,
  moonOutline, pencilOutline, peopleOutline, personAddOutline, personCircleOutline,
  personOutline, pieChartOutline, receiptOutline, removeCircleOutline, restaurantOutline,
  returnUpBackOutline, schoolOutline, settingsOutline, statsChartOutline, sunnyOutline,
  swapHorizontalOutline, syncOutline, trashOutline, trendingDownOutline, trendingUpOutline,
  tvOutline, walletOutline, eyeOutline, eyeOffOutline, checkmarkCircleOutline
} from 'ionicons/icons';

addIcons({
  'add-outline': addOutline, 'add-circle-outline': addCircleOutline,
  'airplane-outline': airplaneOutline, 'alert-circle-outline': alertCircleOutline,
  'analytics-outline': analyticsOutline, 'arrow-down-outline': arrowDownOutline,
  'arrow-up-outline': arrowUpOutline, 'bag-outline': bagOutline, 'basket-outline': basketOutline,
  'briefcase-outline': briefcaseOutline, 'calendar-outline': calendarOutline,
  'car-outline': carOutline, 'card-outline': cardOutline, 'cash-outline': cashOutline,
  'chevron-forward-outline': chevronForwardOutline, 'close-outline': closeOutline,
  'create-outline': createOutline, 'ellipsis-horizontal': ellipsisHorizontal,
  'film-outline': filmOutline, 'grid-outline': gridOutline, 'home-outline': homeOutline,
  'list-outline': listOutline, 'lock-closed-outline': lockClosedOutline,
  'log-out-outline': logOutOutline, 'mail-outline': mailOutline, 'medkit-outline': medkitOutline,
  'moon-outline': moonOutline, 'pencil-outline': pencilOutline,
  'person-circle-outline': personCircleOutline, 'person-outline': personOutline,
  'pie-chart-outline': pieChartOutline, 'receipt-outline': receiptOutline,
  'remove-circle-outline': removeCircleOutline, 'restaurant-outline': restaurantOutline,
  'return-up-back-outline': returnUpBackOutline, 'school-outline': schoolOutline,
  'settings-outline': settingsOutline, 'stats-chart-outline': statsChartOutline,
  'sunny-outline': sunnyOutline, 'swap-horizontal-outline': swapHorizontalOutline,
  'sync-outline': syncOutline, 'trash-outline': trashOutline,
  'trending-down-outline': trendingDownOutline, 'trending-up-outline': trendingUpOutline,
  'tv-outline': tvOutline, 'wallet-outline': walletOutline,
  'eye-outline': eyeOutline, 'eye-off-outline': eyeOffOutline,
  'checkmark-circle-outline': checkmarkCircleOutline,
  'business-outline': businessOutline, 'checkmark-done-outline': checkmarkDoneOutline,
  'close-circle-outline': closeCircleOutline, 'document-text-outline': documentTextOutline,
  'information-circle-outline': informationCircleOutline, 'link-outline': linkOutline,
  'people-outline': peopleOutline, 'person-add-outline': personAddOutline,
});

@NgModule({
  declarations: [AppComponent],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    FormsModule,
    ReactiveFormsModule,
    IonicModule.forRoot({ mode: 'md' }),
    AppRoutingModule,
    ServiceWorkerModule.register('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000'
    })
  ],
  providers: [
    { provide: RouteReuseStrategy, useClass: IonicRouteStrategy },
    provideHttpClient(withInterceptors([jwtInterceptor, errorInterceptor]))
  ],
  bootstrap: [AppComponent]
})
export class AppModule {}
