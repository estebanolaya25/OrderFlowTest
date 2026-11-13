import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { AvisosService } from '../servicios/avisos.service';

// Errores que no dependen de la pantalla (sin conexión, servicio caído): se avisan en un toast,
// una sola vez aunque fallen varias peticiones seguidas (el refresco cada 3 s no inunda de avisos).
// El error se vuelve a lanzar: cada pantalla sigue mostrando su propio mensaje.
@Injectable()
export class ErroresInterceptor implements HttpInterceptor {
  private readonly ultimoAviso = new Map<string, number>();

  constructor(private avisos: AvisosService) {}

  intercept(peticion: HttpRequest<unknown>, siguiente: HttpHandler): Observable<HttpEvent<unknown>> {
    return siguiente.handle(peticion).pipe(
      catchError((error: unknown) => {
        if (error instanceof HttpErrorResponse) {
          this.avisar(error.status);
        }
        return throwError(() => error);
      })
    );
  }

  private avisar(estado: number): void {
    if (estado === 0) {
      this.unaVez('sin-conexion', () =>
        this.avisos.error('Sin conexión con el servidor', 'Revise que el backend esté corriendo (docker compose up).'));
    } else if (estado === 502 || estado === 503 || estado === 504) {
      this.unaVez('servicio', () =>
        this.avisos.error('Servicio no disponible', 'Orders API no responde en este momento. Intente en unos segundos.'));
    } else if (estado >= 500) {
      this.unaVez('servidor', () =>
        this.avisos.error('Error del servidor', 'Ocurrió un error inesperado. Intente de nuevo.'));
    }
  }

  private unaVez(clave: string, mostrar: () => void): void {
    const ahora = Date.now();
    if (ahora - (this.ultimoAviso.get(clave) ?? 0) > 8000) {
      this.ultimoAviso.set(clave, ahora);
      mostrar();
    }
  }
}
