# Bitácora de decisiones

Registro cronológico de decisiones técnicas, para no tener que reconstruirlas de memoria el
último día. Se consolida en el README final.

## Día 1 — Setup

**Arquitectura backend: hexagonal sin MediatR.**
`Controller → Application Service (interfaz) → Repository (interfaz) → Infrastructure`.
Se descarta MediatR: para el alcance de 7 días agrega una dependencia y un concepto
(pipeline de comandos/queries) sin necesidad real de desacoplar el envío de la ejecución.
Menos piezas, más fácil de sustentar en la entrevista técnica.

**Hash de contraseña: BCrypt + pepper de aplicación.**
BCrypt genera y embebe el salt por password automáticamente (cumple la parte de "salt" del
enunciado sin trabajo extra). El "pepper" es un secreto adicional de aplicación (no de fila),
se concatena al password antes de hashear, y vive solo en variable de entorno
(`PASSWORD_PEPPER`), nunca en la base de datos ni en el repositorio.
Descartado: Argon2 — más moderno, pero innecesario para el alcance del reto y más difícil
de justificar sus parámetros (memoria/paralelismo) en la sustentación.

**Modelo de datos: `Tarea.ProyectoId` denormalizado.**
Además de `Tarea.ColumnaId`, se guarda `Tarea.ProyectoId` directo. Simplifica autorización,
consultas de reporte, agrupación de SignalR e índices, sin pasar por `Columna` en cada
filtro. Contrapartida: hay que garantizar que la columna destino de un movimiento pertenezca
al mismo proyecto. Se resuelve en el agregado `Proyecto` (`MoverTarea`/`AgregarTarea`
validan explícitamente la pertenencia antes de mutar), nunca queda a cargo de la
Infraestructura ni del controller.

**Estrategia de orden: fraccionaria (decimal), no reindexado por movimiento.**
`CalculadorDeOrden.CalcularOrden(anterior, siguiente)` calcula el promedio entre vecinos
(o aplica un gap fijo en los extremos). Mover una tarea es un solo `UPDATE`. Si el gap entre
dos vecinos se vuelve demasiado chico para admitir un valor intermedio distinto, se lanza
una excepción explícita señalando que hace falta un reindexado — no se agrega el
reindexado automático en el ejercicio por alcance, pero queda documentado como mejora futura.

**Agregado `Proyecto` como raíz.**
`Columna` y `Tarea` solo pueden crearse (constructor `internal`) o moverse a través de
métodos del agregado `Proyecto` (`AgregarColumna`, `EliminarColumna`, `AgregarTarea`,
`MoverTarea`). Esto centraliza en un solo lugar las dos reglas de negocio explícitas del
enunciado: no eliminar una columna con tareas, y no permitir mover una tarea a una columna
de otro proyecto.

<!-- Próximas entradas: SignalR, ClosedXML, Strategy/Factory de reportes (ya decididos en
el plan base, se documentan acá con más detalle cuando se implementen). -->

## Infraestructura y entorno local (verificado)

**PostgreSQL en Docker.** Se ejecuta como servicio (`postgres:16-alpine`) en lugar de una
instalación local, para garantizar un entorno reproducible e idéntico al que usará el
evaluador. Contenedor `scrumboard-db`, con `healthcheck` (`pg_isready`) del que depende el
arranque de la Api (`depends_on: db: condition: service_healthy`).

**Configuración exclusivamente por variables de entorno.** `appsettings.json` no contiene
secretos ni connection strings — solo configuración no sensible (`Jwt:Issuer`, `Jwt:Audience`,
`Jwt:ExpiracionMinutos`). Todo lo dependiente del entorno (`ConnectionStrings__Default`,
`Jwt__Secret`, `PasswordHasher__Pepper`, `FrontendUrl`) llega por variables de entorno, que
`docker-compose.yml` puebla a partir de `.env` (ubicado en la raíz del repo, junto con
`docker-compose.yml`, no dentro de `backend/`).

**Migraciones automáticas al iniciar.** La Api ejecuta `context.Database.MigrateAsync()` en
`Program.cs` al arrancar, así que Docker puede levantar la aplicación completa con la base de
datos ya preparada, sin pasos manuales. Las migraciones viven en
`ScrumBoard.Infrastructure/Migrations/`, al mismo nivel que `Persistence`, `Repositories` y
`Security` — no se relocalizan.

**Separación de hostnames `db` vs `localhost`.** Dentro de la red interna de Docker, la Api
resuelve PostgreSQL como `Host=db` (nombre del servicio en `docker-compose.yml`). Al generar
una migración de EF Core directamente desde Windows (`dotnet ef ... `, fuera del contenedor),
la connection string debe apuntar a `Host=localhost`, porque ese proceso corre fuera de la red
de Docker y no lee `.env` automáticamente (a diferencia de Docker Compose). Esta diferencia es
intencional y no debe "corregirse" cambiando `Host=db` dentro de `docker-compose.yml`.

**`.dockerignore` en `backend/`.** Excluye `bin/`, `obj/`, `.vs/`, `.vscode/`, artefactos de
test y `node_modules/` del contexto de build de Docker. Evita que binarios/objetos generados
localmente en Windows (con rutas propias de NuGet de esa máquina) contaminen el build dentro
del contenedor Linux.

**CORS.** Habilitado únicamente para el origen configurado en `FRONTEND_URL`
(`http://localhost:4200` en desarrollo), con `AllowCredentials()` para soportar más adelante
el handshake de SignalR.

**Estado verificado:** build de la imagen de la Api, contenedores `db` y `api` up/healthy,
migración `InitialCreate` aplicada, tablas `usuarios`/`proyectos`/`columnas`/`tareas`/
`__EFMigrationsHistory` creadas, seed de usuarios ejecutado, login con JWT verificado desde
Swagger.

**Pendiente para un entorno productivo (no bloqueante para el challenge):** las claves de
ASP.NET Core Data Protection se almacenan hoy en el filesystem efímero del contenedor
(warning visible en logs). En producción deberían persistirse fuera del contenedor.
