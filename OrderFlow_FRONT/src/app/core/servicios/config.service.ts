import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

interface ConfiguracionApp {
  apiUrl: string;
}

// Configuración leída en tiempo de ejecución desde /assets/config.json (mismo patrón que FormularioContraparte):
// la URL de la API no queda compilada en el bundle, así el mismo build sirve para cualquier entorno.
//  - Docker y ng serve: "/api" (nginx o proxy.conf.json reenvían a Orders API).
//  - Azure Static Web Apps: se reemplaza config.json por la URL pública de Orders API al desplegar.
@Injectable({ providedIn: 'root' })
export class ConfigService {
  private configuracion: ConfiguracionApp = { apiUrl: '/api' };

  constructor(private http: HttpClient) {}

  // Se ejecuta antes de arrancar la app (provideAppInitializer en AppModule).
  // Si el archivo no se puede leer, se sigue con "/api" en vez de dejar la app en blanco.
  cargar(): Promise<void> {
    return firstValueFrom(this.http.get<ConfiguracionApp>(`/assets/config.json?v=${Date.now()}`))
      .then(datos => {
        if (datos?.apiUrl) {
          this.configuracion = { apiUrl: datos.apiUrl.replace(/\/+$/, '') };
        }
      })
      .catch(() => console.warn('No se pudo leer /assets/config.json; se usa "/api".'));
  }

  get apiUrl(): string {
    return this.configuracion.apiUrl;
  }
}
