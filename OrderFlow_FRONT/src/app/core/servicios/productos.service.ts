import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';
import { RUTAS_API } from '../config/api.config';
import { Producto } from '../modelos/pedido.model';
import { ConfigService } from './config.service';

@Injectable({ providedIn: 'root' })
export class ProductosService {
  private catalogo$?: Observable<Producto[]>;

  constructor(private http: HttpClient, private config: ConfigService) {}

  // El catálogo casi no cambia: se pide una vez y se reutiliza. Si falla, se borra para reintentar la próxima vez.
  listar(): Observable<Producto[]> {
    if (!this.catalogo$) {
      this.catalogo$ = this.http
        .get<Producto[]>(`${this.config.apiUrl}${RUTAS_API.productos}`)
        .pipe(shareReplay({ bufferSize: 1, refCount: false }));
      this.catalogo$.subscribe({ error: () => (this.catalogo$ = undefined) });
    }
    return this.catalogo$;
  }
}
