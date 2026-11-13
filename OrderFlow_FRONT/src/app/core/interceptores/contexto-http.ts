import { HttpContext, HttpContextToken } from '@angular/common/http';

// Peticiones de fondo (por ejemplo, refrescar la lista de pedidos cada 3 s): no muestran la barra de carga.
export const SIN_BARRA_DE_CARGA = new HttpContextToken<boolean>(() => false);

export function peticionSilenciosa(): HttpContext {
  return new HttpContext().set(SIN_BARRA_DE_CARGA, true);
}
