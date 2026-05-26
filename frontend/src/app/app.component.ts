import { Component } from '@angular/core';
import { CurrencyService } from './core/currency/currency.service';

@Component({
  selector: 'app-root',
  template: '<ion-app><ion-router-outlet></ion-router-outlet></ion-app>',
})
export class AppComponent {
  constructor(private currency: CurrencyService) {
    const saved = localStorage.getItem('ft.theme');
    const prefersDark = window.matchMedia?.('(prefers-color-scheme: dark)').matches;
    const dark = saved ? saved === 'dark' : prefersDark;
    document.body.classList.toggle('dark', dark);
    this.currency.init();
  }
}
