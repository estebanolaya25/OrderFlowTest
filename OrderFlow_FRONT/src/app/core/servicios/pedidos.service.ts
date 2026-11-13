import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { RUTAS_API } from '../config/api.config';
import { peticionSilenciosa } from '../interceptores/contexto-http';
import { NuevoPedido, Pedido } from '../modelos/pedido.model';
import { ConfigService } from './config.service';

@Injectable({ providedIn: 'root' })
export class PedidosService {
  constructor(private http: HttpClient, private config: ConfigService) {}

  private get url(): string {
    return `${this.config.apiUrl}${RUTAS_API.pedidos}`;
  }

  crear(pedido: NuevoPedido): Observable<Pedido> {
    return this.http.post<Pedido>(this.url, pedido);
  }

  // silenciosa: el refresco periódico no muestra la barra de carga cada 3 segundos.
  listar(silenciosa = false): Observable<Pedido[]> {
    return this.http.get<Pedido[]>(this.url, silenciosa ? { context: peticionSilenciosa() } : {});
  }

  obtener(id: string): Observable<Pedido> {
    return this.http.get<Pedido>(`${this.url}/${id}`);
  }
}
