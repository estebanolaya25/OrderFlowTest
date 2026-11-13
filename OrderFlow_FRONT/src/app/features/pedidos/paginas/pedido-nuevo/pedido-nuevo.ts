import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { NuevoPedido, Producto, REGLAS_PEDIDO } from '../../../../core/modelos/pedido.model';
import { AvisosService } from '../../../../core/servicios/avisos.service';
import { PedidosService } from '../../../../core/servicios/pedidos.service';
import { ProductosService } from '../../../../core/servicios/productos.service';
import { erroresPorCampo, mensajeDeError } from '../../../../core/utilidades/errores-http';
import { entero, sinSoloEspacios } from '../../../../core/utilidades/validadores';

type Campo = keyof NuevoPedido;

@Component({
  selector: 'app-pedido-nuevo',
  standalone: false,
  templateUrl: './pedido-nuevo.html',
  styleUrl: './pedido-nuevo.scss'
})
export class PedidoNuevo implements OnInit {
  readonly formulario;
  readonly reglas = REGLAS_PEDIDO;

  productos: Producto[] = [];
  cargandoProductos = true;
  errorProductos: string | null = null;

  enviando = false;
  error: string | null = null;

  constructor(
    fb: FormBuilder,
    private pedidosService: PedidosService,
    private productosService: ProductosService,
    private avisos: AvisosService,
    private router: Router
  ) {
    // Las mismas reglas que valida el backend (clsValidadorPedido): se avisa antes de enviar,
    // pero el backend sigue siendo la fuente de verdad (ver mostrarErroresDelServidor).
    this.formulario = fb.nonNullable.group({
      clienteNombre: ['', [Validators.required, sinSoloEspacios, Validators.maxLength(REGLAS_PEDIDO.longitudMaximaNombre)]],
      sku: ['', Validators.required],
      cantidad: [1, [Validators.required, entero, Validators.min(REGLAS_PEDIDO.cantidadMinima), Validators.max(REGLAS_PEDIDO.cantidadMaxima)]]
    });
  }

  ngOnInit(): void {
    this.cargarProductos();
  }

  cargarProductos(): void {
    this.cargandoProductos = true;
    this.errorProductos = null;
    this.productosService.listar().subscribe({
      next: productos => {
        this.productos = productos;
        this.cargandoProductos = false;
      },
      error: error => {
        this.errorProductos = mensajeDeError(error, 'No se pudo cargar el catálogo de productos.');
        this.cargandoProductos = false;
      }
    });
  }

  invalido(campo: Campo): boolean {
    const control = this.formulario.controls[campo];
    return control.invalid && (control.dirty || control.touched);
  }

  // Un solo mensaje por campo; el del servidor tiene prioridad porque es el que decidió.
  mensaje(campo: Campo): string {
    const errores = this.formulario.controls[campo].errors ?? {};
    if (errores['servidor']) return errores['servidor'];
    if (errores['required']) return campo === 'sku' ? 'Seleccione un producto.' : 'Este campo es obligatorio.';
    if (errores['soloEspacios']) return 'El nombre no puede tener solo espacios.';
    if (errores['maxlength']) return `Máximo ${errores['maxlength'].requiredLength} caracteres.`;
    if (errores['entero']) return 'La cantidad debe ser un número entero.';
    if (errores['min'] || errores['max']) return `La cantidad debe estar entre ${this.reglas.cantidadMinima} y ${this.reglas.cantidadMaxima}.`;
    return '';
  }

  crear(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    const valores = this.formulario.getRawValue();
    const pedido: NuevoPedido = { ...valores, clienteNombre: valores.clienteNombre.trim() };

    this.enviando = true;
    this.error = null;
    this.pedidosService.crear(pedido).subscribe({
      next: creado => {
        this.avisos.exito('Pedido creado', 'Queda Pendiente mientras Inventory reserva el stock.');
        this.router.navigate(['/pedidos'], { queryParams: { nuevo: creado.id } });
      },
      error: error => {
        this.enviando = false;
        this.mostrarErroresDelServidor(error);
      }
    });
  }

  // Un 400 trae errores por campo: se pegan al control correspondiente para mostrarlos debajo del input.
  // Lo que no corresponde a un campo (sin conexión, error 500...) va al mensaje general del formulario.
  private mostrarErroresDelServidor(error: unknown): void {
    const porCampo = erroresPorCampo(error);
    let asignados = 0;
    for (const [campo, mensaje] of Object.entries(porCampo)) {
      const control = this.formulario.get(campo);
      if (control) {
        control.setErrors({ ...control.errors, servidor: mensaje });
        control.markAsTouched();
        asignados++;
      }
    }

    if (asignados === 0) {
      this.error = mensajeDeError(error, 'No se pudo crear el pedido.');
    }
  }
}
