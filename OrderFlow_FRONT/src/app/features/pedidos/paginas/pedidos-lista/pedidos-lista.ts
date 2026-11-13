import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { EMPTY, catchError, exhaustMap, timer } from 'rxjs';
import { INTERVALO_REFRESCO_MS } from '../../../../core/config/api.config';
import { ESTADOS_PEDIDO, EstadoPedido, Pedido } from '../../../../core/modelos/pedido.model';
import { AvisosService } from '../../../../core/servicios/avisos.service';
import { PedidosService } from '../../../../core/servicios/pedidos.service';
import { mensajeDeError } from '../../../../core/utilidades/errores-http';
import { fechaUtc } from '../../../../core/utilidades/fechas';

const DURACION_RESALTADO_MS = 4000;

@Component({
  selector: 'app-pedidos-lista',
  standalone: false,
  templateUrl: './pedidos-lista.html',
  styleUrl: './pedidos-lista.scss'
})
export class PedidosLista implements OnInit {
  pedidos: Pedido[] | null = null;
  error: string | null = null;
  ultimaActualizacion: Date | null = null;
  filtro: EstadoPedido | '' = '';

  // Pedido recién creado (llega en ?nuevo=) y pedidos que acaban de cambiar de estado: se resaltan.
  nuevoId: string | null = null;
  readonly resaltados = new Set<string>();

  readonly estados = Object.entries(ESTADOS_PEDIDO).map(([valor, info]) => ({ valor: valor as EstadoPedido, ...info }));
  readonly intervaloSegundos = INTERVALO_REFRESCO_MS / 1000;

  private readonly estadosAnteriores = new Map<string, EstadoPedido>();
  private readonly destroyRef = inject(DestroyRef);

  constructor(private pedidosService: PedidosService, private avisos: AvisosService, private ruta: ActivatedRoute) {}

  ngOnInit(): void {
    this.nuevoId = this.ruta.snapshot.queryParamMap.get('nuevo');

    // Polling: consulta inmediata y luego cada 3 s mientras la página esté abierta.
    //  - exhaustMap ignora un tic si la consulta anterior sigue en curso (no se acumulan peticiones si el backend va lento).
    //  - catchError dentro del exhaustMap: un fallo no detiene el polling; se reintenta en el siguiente tic.
    //  - takeUntilDestroyed: al salir de la página se cancela el timer.
    timer(0, INTERVALO_REFRESCO_MS)
      .pipe(
        exhaustMap(tic =>
          this.pedidosService.listar(tic > 0).pipe(
            catchError(error => {
              this.error = mensajeDeError(error, 'No se pudo cargar la lista de pedidos.');
              return EMPTY;
            })
          )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(pedidos => {
        this.detectarCambiosDeEstado(pedidos);
        this.pedidos = pedidos;
        this.error = null;
        this.ultimaActualizacion = new Date();
      });
  }

  get pedidosFiltrados(): Pedido[] {
    return (this.pedidos ?? []).filter(pedido => !this.filtro || pedido.estado === this.filtro);
  }

  cantidadPorEstado(estado: EstadoPedido): number {
    return (this.pedidos ?? []).filter(pedido => pedido.estado === estado).length;
  }

  filtrar(estado: EstadoPedido | ''): void {
    this.filtro = estado;
  }

  fecha(texto: string): Date {
    return fechaUtc(texto);
  }

  // Evita que Angular redibuje todas las filas en cada refresco: solo cambia lo que cambió.
  porId(_: number, pedido: Pedido): string {
    return pedido.id;
  }

  // Compara con la consulta anterior: si un pedido pasó de Pending a otro estado, se resalta y se avisa.
  private detectarCambiosDeEstado(pedidos: Pedido[]): void {
    const primeraCarga = this.estadosAnteriores.size === 0 && this.pedidos === null;

    for (const pedido of pedidos) {
      const anterior = this.estadosAnteriores.get(pedido.id);
      if (!primeraCarga && anterior && anterior !== pedido.estado) {
        this.resaltar(pedido.id);
        const info = ESTADOS_PEDIDO[pedido.estado];
        const titulo = `Pedido de ${pedido.clienteNombre}: ${info.texto.toLowerCase()}`;
        if (pedido.estado === 'Confirmed') {
          this.avisos.exito(titulo, `${pedido.cantidad} × ${pedido.sku} reservado.`);
        } else {
          this.avisos.info(titulo, pedido.motivoRechazo ?? undefined);
        }
      }
      this.estadosAnteriores.set(pedido.id, pedido.estado);
    }
  }

  private resaltar(id: string): void {
    this.resaltados.add(id);
    setTimeout(() => this.resaltados.delete(id), DURACION_RESALTADO_MS);
  }
}
