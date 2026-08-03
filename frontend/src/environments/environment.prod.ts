// Entorno de producción. En el build de Docker, la imagen de nginx sirve este
// archivo compilado; API_URL/SIGNALR_URL se resuelven en build-time desde las
// variables de entorno del Dockerfile del frontend (ver frontend/Dockerfile).
export const environment = {
  production: true,
  apiUrl: 'http://localhost:8080/api',
  signalRUrl: 'http://localhost:8080/hubs/tablero'
};
