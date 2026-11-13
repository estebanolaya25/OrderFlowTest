import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API } from '../config/api.config';
import { peticionSilenciosa } from '../interceptores/contexto-http';
import { NuevoPedido, Pedido } from '../modelos/pedido.model';

@Injectable({ providedIn: 'root' })
export class PedidosService {
  constructor(private http: HttpClient) {}

  crear(pedido: NuevoPedido): Observable<Pedido> {
    return this.http.post<Pedido>(API.pedidos, pedido);
  }

  // silenciosa: el refresco periódico no muestra la barra de carga cada 3 segundos.
  listar(silenciosa = false): Observable<Pedido[]> {
    return this.http.get<Pedido[]>(API.pedidos, silenciosa ? { context: peticionSilenciosa() } : {});
  }

  obtener(id: string): Observable<Pedido> {
    return this.http.get<Pedido>(`${API.pedidos}/${id}`);
  }
}
