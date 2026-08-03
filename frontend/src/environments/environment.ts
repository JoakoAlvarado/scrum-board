// Entorno de desarrollo (ng serve). Se reemplaza por environment.prod.ts en
// builds de producción (ver angular.json > configurations > production >
// fileReplacements) — requisito 6.1: configuración externa, sin URLs
// embebidas en componentes o servicios.
export const environment = {
  production: false,
  apiUrl: 'http://localhost:8080/api',
  signalRUrl: 'http://localhost:8080/hubs/tablero'
};
