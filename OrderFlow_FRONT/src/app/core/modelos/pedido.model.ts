export type EstadoPedido = 'Pending' | 'Confirmed' | 'Rejected';

// Respuesta de GET /api/orders y GET /api/orders/{id}.
export interface Pedido {
  id: string;
  clienteNombre: string;
  sku: string;
  cantidad: number;
  estado: EstadoPedido;
  motivoRechazo: string | null;
  creadoEn: string;
  actualizadoEn: string;
}

// Cuerpo de POST /api/orders.
export interface NuevoPedido {
  clienteNombre: string;
  sku: string;
  cantidad: number;
}

export interface Producto {
  sku: string;
  nombre: string;
}

// Reglas compartidas con el backend (clsValidadorPedido): el front valida lo mismo para avisar antes de enviar.
export const REGLAS_PEDIDO = {
  cantidadMinima: 1,
  cantidadMaxima: 100,
  longitudMaximaNombre: 150
} as const;

// Cómo se muestra cada estado en la interfaz.
export const ESTADOS_PEDIDO: Record<EstadoPedido, { texto: string; clase: string; icono: string }> = {
  Pending: { texto: 'Pendiente', clase: 'advertencia', icono: 'bi-hourglass-split' },
  Confirmed: { texto: 'Confirmado', clase: 'exito', icono: 'bi-check-circle' },
  Rejected: { texto: 'Rechazado', clase: 'peligro', icono: 'bi-x-circle' }
};
