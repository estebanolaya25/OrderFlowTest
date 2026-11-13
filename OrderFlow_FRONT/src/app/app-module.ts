import { LOCALE_ID, NgModule, inject, provideAppInitializer, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeEsCO from '@angular/common/locales/es-CO';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { CoreModule } from './core/core-module';
import { ConfigService } from './core/servicios/config.service';
import { SharedModule } from './shared/shared-module';

// Fechas en español de Colombia ("13 nov 2026, 4:30 p. m.") en los pipes de Angular.
registerLocaleData(localeEsCO);

@NgModule({
  declarations: [App],
  imports: [BrowserModule, AppRoutingModule, CoreModule, SharedModule],
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    { provide: LOCALE_ID, useValue: 'es-CO' },
    // Lee /assets/config.json (URL de la API) antes de que arranque cualquier componente.
    provideAppInitializer(() => inject(ConfigService).cargar())
  ],
  bootstrap: [App]
})
export class AppModule {}
