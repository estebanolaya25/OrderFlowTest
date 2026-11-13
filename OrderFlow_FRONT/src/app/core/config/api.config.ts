// Todas las llamadas van a /api, que reenvía a Orders API:
//  - en Docker, nginx (nginx.conf) hace proxy a http://orders-api:8080
//  - en desarrollo, ng serve hace proxy a http://localhost:5201 (proxy.conf.json)
// Así no hay URLs del backend en el código ni necesidad de CORS.
export const API = {
  pedidos: '/api/orders',
  productos: '/api/products'
} as const;

// Cada cuánto se refresca la lista de pedidos (polling). El PDF lo acepta; SignalR sería el bonus.
export const INTERVALO_REFRESCO_MS = 3000;
