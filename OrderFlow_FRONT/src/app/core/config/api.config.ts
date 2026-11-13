// Rutas de Orders API, relativas a la URL base que entrega ConfigService (/assets/config.json):
//  - en Docker, "/api" y nginx (nginx.conf) hace proxy a http://orders-api:8080
//  - en desarrollo, "/api" y ng serve hace proxy a http://localhost:5301 (proxy.conf.json)
//  - en Azure Static Web Apps, la URL pública de Orders API
export const RUTAS_API = {
  pedidos: '/orders',
  productos: '/products'
} as const;

// Cada cuánto se refresca la lista de pedidos (polling). El PDF lo acepta; SignalR sería el bonus.
export const INTERVALO_REFRESCO_MS = 3000;
