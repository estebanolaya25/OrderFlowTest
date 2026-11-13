import { HttpErrorResponse } from '@angular/common/http';

// Convierte cualquier error del backend en un mensaje legible para el usuario.
export function mensajeDeError(error: unknown, mensajePorDefecto = 'Ocurrió un error inesperado. Intente de nuevo.'): string {
  if (!(error instanceof HttpErrorResponse)) {
    return mensajePorDefecto;
  }

  if (error.status === 0) {
    return 'No hay conexión con el servidor. Verifique que el backend esté corriendo.';
  }
  if (error.status === 502 || error.status === 503 || error.status === 504) {
    return 'El servicio no está disponible en este momento. Intente en unos segundos.';
  }

  // ProblemDetails (RFC 7807): Orders API pone el resumen en "title".
  const titulo = error.error?.title;
  if (typeof titulo === 'string' && titulo && error.status < 500) {
    return titulo;
  }
  return mensajePorDefecto;
}

// Errores por campo de un ValidationProblemDetails ({ errors: { clienteNombre: ["..."] } }).
// ASP.NET usa "$.cantidad" cuando el JSON no se pudo convertir; se normaliza a "cantidad"
// para que coincida con el nombre del control del formulario.
export function erroresPorCampo(error: unknown): Record<string, string> {
  if (!(error instanceof HttpErrorResponse) || error.status !== 400) {
    return {};
  }

  const errores = error.error?.errors as Record<string, string[]> | undefined;
  const resultado: Record<string, string> = {};
  for (const [clave, mensajes] of Object.entries(errores ?? {})) {
    const campo = clave.replace(/^\$\./, '');
    if (mensajes?.length) {
      resultado[campo] = mensajes[0];
    }
  }
  return resultado;
}
