# ScrumBoard

Módulo inicial de una plataforma de gestión Scrum: proyectos, columnas configurables y un
tablero kanban con tiempo real, drag & drop persistente, y exportación de reportes en PDF y
Excel. Desarrollado como prueba técnica para IDEASGROUP (proceso IDEASGROUP-REM-LAT-26-2907).

## Estado actual

| Componente | Estado |
|---|---|
| API .NET 8 (arquitectura hexagonal) | OK |
| PostgreSQL 16 (Docker) | OK healthy |
| Docker Compose (db + api + frontend) | OK |
| EF Core — migración `InitialCreate` | OK aplicada |
| Tablas en base de datos | OK verificadas |
| Seed de usuarios iniciales | OK |
| Autenticación JWT + BCrypt/Pepper (backend) | OK login verificado |
| Swagger | OK |
| CORS | OK configurado para el frontend |
| Frontend Angular 17 + PrimeNG Sakai (scaffold, tema, layout) | OK |
| Frontend — login real conectado a la Api | OK |
| Frontend — guard de ruta + interceptor JWT | OK |
| **Frontend — CRUD de Proyectos (tabla paginada + filtro + alta/edición/baja)** | OK |
| **Frontend — Tablero Kanban (columnas + tareas, drag & drop, filtros)** | OK |
| Frontend — Docker (nginx) | OK imagen armada |
| Tiempo real (SignalR), reportes (PDF/Excel) | Pendiente |
| **Backend — CRUD de Proyectos (paginado + filtro)** | OK |
| Backend — middleware centralizado de excepciones | OK |
| Backend — Swagger con autenticación Bearer | OK |
| **Backend — CRUD de Columnas (incluye orden y reordenamiento)** | OK |
| **Backend — CRUD de Tareas (incluye mover/reordenar entre columnas)** | OK |
| **Backend — endpoint de Usuarios (listado, para asignar responsable)** | OK |

> Bitácora de decisiones técnicas: [`docs/decisiones.md`](docs/decisiones.md).

## Requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) + Docker Compose
- Node.js / npm (para el frontend, cuando se incorpore)
- .NET 8 SDK — **solo** si se quiere compilar/ejecutar el backend fuera de Docker, o generar
  migraciones nuevas de EF Core

## Stack

| Componente | Tecnología |
|---|---|
| Backend | .NET 8, C#, arquitectura hexagonal (Domain / Application / Infrastructure / Api) |
| Frontend | Angular 17, PrimeNG 17 (plantilla Sakai, tag `17.0.0`), TypeScript, SCSS |
| Persistencia | PostgreSQL + Entity Framework Core (migraciones incrementales) |
| Reporte PDF | QuestPDF |
| Reporte Excel | ClosedXML |
| Tiempo real | SignalR |
| Contenedores | Docker Compose (Postgres + Api + SPA con nginx) |

## Configuración (.env)

`docker-compose.yml` y `.env` están en la **raíz del repositorio** (no dentro de `backend/`).
Copiar la plantilla antes de levantar el proyecto:

```bash
cp .env.example .env
```

El `.env` contiene la configuración de:

- **PostgreSQL:** `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_PORT`
- **Api:** `ASPNETCORE_ENVIRONMENT`, `API_PORT`
- **JWT:** `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_EXPIRACION_MINUTOS`
- **Hash de contraseña:** `PASSWORD_PEPPER`
- **Frontend:** `FRONTEND_URL`, `FRONTEND_PORT`, `API_URL`, `SIGNALR_URL`

`docker-compose.yml` traduce estas variables a la convención de configuración de ASP.NET Core
(doble guion bajo) al inyectarlas en el contenedor `api`, por ejemplo `POSTGRES_*` arman
`ConnectionStrings__Default`, y `JWT_SECRET` → `Jwt__Secret`, `PASSWORD_PEPPER` →
`PasswordHasher__Pepper`, etc. Ninguno de estos valores está en `appsettings.json` ni
versionado en el repositorio — ver sección [Autenticación](#autenticación-y-seguridad).

## Cómo levantar el proyecto

```bash
git clone <url-del-repo>
cd scrum-board
cp .env.example .env
docker compose up -d --build
```

Si por ahora solo se necesita el backend (frontend todavía no incorporado al compose):

```bash
docker compose up -d --build db api
```

Al arrancar, la Api aplica automáticamente las migraciones de EF Core pendientes
(`Database.MigrateAsync()` en `Program.cs`) y ejecuta el seed de usuarios — no hace falta
correr ningún comando manual adicional para tener la base de datos lista.

### Verificar que los contenedores están arriba

```bash
docker compose ps
```

Se debería ver `scrumboard-db` como `Up (healthy)` y `scrumboard-api` como `Up`.

### Ver logs

```bash
docker compose logs --tail=200 api
```

### Acceso

- **Frontend:** http://localhost:4200 — pantalla de login; tras autenticarse redirige a
  `/proyectos`.
- **Api:** http://localhost:8080
- **Swagger:** http://localhost:8080/swagger *(habilitado porque `ASPNETCORE_ENVIRONMENT=Development`)*
- **PostgreSQL:** `localhost:5432` — base `scrumboard`, usuario `scrumboard_user` *(password:
  ver `.env`, no se documenta en texto público)*

### Correr el frontend fuera de Docker (desarrollo con hot-reload)

```bash
cd frontend
npm install
npm start
```

Sirve en http://localhost:4200 con `ng serve` apuntando a `environment.ts` (Api en
`http://localhost:8080/api`, definida en `frontend/src/environments/`).

### Usuario de desarrollo/demo

El seed crea usuarios iniciales para poder probar el login. **Es una credencial de
desarrollo/demo, no de producción:**

| Email | Password |
|---|---|
| `admin@scrumboard.local` | `Admin123!` |
| `usuario@scrumboard.local` | `Usuario123!` |

Verificado funcionando end-to-end (login por Swagger → token JWT válido).

## Estructura del repositorio

```
scrum-board/
├── backend/
│   ├── .dockerignore
│   ├── ScrumBoard.sln
│   ├── src/
│   │   ├── ScrumBoard.Domain/          → entidades, reglas de negocio, sin dependencias
│   │   ├── ScrumBoard.Application/     → casos de uso, puertos (interfaces), DTOs
│   │   ├── ScrumBoard.Infrastructure/  → EF Core, Migrations, BCrypt, JWT
│   │   │   └── Migrations/             → migraciones EF Core (InitialCreate)
│   │   └── ScrumBoard.Api/             → controllers, Program.cs, appsettings.json, Dockerfile
│   └── tests/
│       └── ScrumBoard.Application.Tests/
├── frontend/                            → Angular 17 + PrimeNG Sakai (tag 17.0.0)
│   ├── Dockerfile, nginx.conf
│   └── src/app/
│       ├── core/
│       │   ├── models/                   → interfaces que reflejan los DTOs del backend
│       │   └── services/                 → AuthService, ProyectoService, ColumnaService, TareaService, UsuarioService
│       ├── features/
│       │   ├── auth/login/                → login real conectado a la Api
│       │   ├── proyectos/                 → tabla paginada + filtro + alta/edición/baja
│       │   └── tablero/                    → Kanban: columnas + tareas, drag & drop, filtros
│       ├── shared/not-found/
│       └── layout/                        → chrome de Sakai (topbar, sidebar, menú, footer)
├── docs/
│   └── decisiones.md                    → decisiones técnicas permanentes
├── docker-compose.yml                   → en la raíz (no dentro de backend/)
├── .env                                 → en la raíz, no versionado (gitignored)
├── .env.example
└── README.md
```

## Arquitectura

**Backend — hexagonal, sin MediatR.** El flujo de una request es
`Controller → Application Service (interfaz) → Repository (interfaz) → Infrastructure`.
Se decidió no usar MediatR para mantener el número de conceptos a defender en la
sustentación técnica acotado al alcance real del ejercicio. Ver detalle y alternativas
descartadas en `docs/decisiones.md`.

**Agregado `Proyecto`.** `Columna` y `Tarea` solo se crean o se mueven a través de métodos
del agregado `Proyecto` (no tienen constructor público), lo que centraliza las dos reglas de
negocio explícitas del enunciado: no eliminar una columna con tareas, y no permitir mover una
tarea a una columna de otro proyecto.

**Frontend — por capas** (`core/`, `features/`, `shared/`, `layout/`), con acceso HTTP
aislado en servicios para no acoplar componentes a la URL del backend (requisito de
configuración externa vía `environment.ts`).

## Decisiones técnicas clave (resumen)

| Punto a decidir | Elegido | Alternativa descartada |
|---|---|---|
| Tiempo real | SignalR | WebSocket puro, SSE |
| Reporte Excel | ClosedXML | EPPlus (licenciamiento) |
| Orden en drag & drop | Fraccionario (decimal) | Reindexado de enteros por movimiento |
| Exportación dual | Strategy + Factory sobre un único DTO/query | Duplicar la consulta por formato |
| Capas de aplicación | Servicios de aplicación directos | MediatR |
| Hash de contraseña | BCrypt + pepper de aplicación | Argon2 |

Justificación completa de cada una en `docs/decisiones.md`.

## Estrategia de índices de ordenamiento

Ver `docs/decisiones.md` — resumen: `Columna.Orden` y `Tarea.Orden` son `decimal`, indexados
como `(ProyectoId, Orden)` y `(ColumnaId, Orden)` respectivamente. Insertar o mover un
elemento calcula un valor intermedio entre sus nuevos vecinos (`CalculadorDeOrden`), sin
reindexar el resto de la lista.

## Patrón de exportación dual

`IReporteProyectoQuery` arma un único `ReporteProyectoDto` con una sola consulta a la base de
datos. `IReporteExporter` (Strategy) tiene una implementación por formato
(`PdfReporteExporter`, `ExcelReporteExporter`), resueltas por factory/DI. Agregar un tercer
formato no requiere modificar las clases existentes.

## Migraciones EF Core

Viven en `backend/src/ScrumBoard.Infrastructure/Migrations/` (al mismo nivel que
`Persistence`, `Repositories` y `Security`). La Api las aplica automáticamente al arrancar
(`context.Database.MigrateAsync()` en `Program.cs`), tanto en Docker como corriendo el proyecto
directo con `dotnet run`.

### Generar una migración nueva

Se ejecuta desde `backend/`:

```bash
dotnet tool run dotnet-ef migrations add NombreMigracion \
  --project src/ScrumBoard.Infrastructure \
  --startup-project src/ScrumBoard.Api
```

**Importante — `Host=db` vs `Host=localhost`:** dentro de Docker, la Api se conecta a
PostgreSQL con `Host=db` (nombre del servicio en `docker-compose.yml`, resuelto por la red
interna de Docker). Pero el comando `dotnet ef` de arriba corre directo en Windows, **fuera**
de esa red, así que necesita la connection string con `Host=localhost` para llegar al Postgres
publicado en el puerto `5432`. Docker Compose lee `.env` automáticamente; `dotnet ef` ejecutado
desde Windows no, por lo que esa connection string hay que proveérsela al proceso por su cuenta
(variable de entorno de la sesión o `appsettings.Development.json` local, no versionado) antes
de correr el comando. **Nunca cambiar `Host=db` por `Host=localhost` dentro de
`docker-compose.yml`** — son dos contextos de red distintos, no un error a corregir.

## Autenticación y seguridad

- **JWT:** emitido y validado con clave simétrica (`Jwt__Secret`, fuera de `appsettings.json`,
  solo por variable de entorno). Configuración no sensible (`Issuer`, `Audience`,
  `ExpiracionMinutos`) sí vive en `appsettings.json` porque no es secreta.
- **BCrypt + pepper:** cada password se hashea con BCrypt (salt embebido automático) sobre
  `password + PASSWORD_PEPPER`. El pepper es un secreto de aplicación, no de fila — solo en
  variable de entorno.
- **CORS:** habilitado únicamente para el origen configurado en `FRONTEND_URL`
  (`http://localhost:4200` por defecto), con `AllowCredentials()` habilitado para soportar el
  handshake de SignalR más adelante.
- Verificado funcionando de punta a punta: login por Swagger devuelve un JWT válido.

### Consideración para producción (no bloqueante ahora)

Los logs muestran un warning de ASP.NET Core Data Protection: las claves se están
almacenando en el filesystem efímero del contenedor (`/root/.aspnet/DataProtection-Keys`). No
afecta el desarrollo del challenge, pero en un entorno productivo real esas claves deberían
persistirse fuera del contenedor (volumen, almacenamiento externo o Azure Key Vault, según el
proveedor).

## API de Proyectos

Todos los endpoints requieren JWT (`Authorization: Bearer <token>`, o el botón "Authorize" en
Swagger pegando solo el token).

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/proyectos?nombre=&pagina=&tamanioPagina=` | Paginado, filtro por nombre (coincidencia parcial, case-insensitive, resuelto en el servidor con `ILIKE`) |
| `GET` | `/api/proyectos/{id}` | Detalle de un proyecto |
| `POST` | `/api/proyectos` | Alta |
| `PUT` | `/api/proyectos/{id}` | Edición (incluye cambio de estado) |
| `DELETE` | `/api/proyectos/{id}` | Baja |

`tamanioPagina` tiene un tope de 50 (definido en `ProyectoService`) para evitar listados sin
cota. Las excepciones de negocio (`RecursoNoEncontradoException` → 404,
`CredencialesInvalidasException` → 401, `DomainException` → 409, `ArgumentException` → 400) se
traducen centralizadamente en `ExceptionHandlingMiddleware`; los controllers no repiten
try/catch en cada acción.

## API de Columnas

Anidada bajo el proyecto. Todas requieren JWT.

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/proyectos/{proyectoId}/columnas` | Listado ordenado por `Orden` |
| `POST` | `/api/proyectos/{proyectoId}/columnas` | Alta (se agrega siempre al final del tablero) |
| `PUT` | `/api/proyectos/{proyectoId}/columnas/{id}` | Renombrar |
| `PUT` | `/api/proyectos/{proyectoId}/columnas/{id}/orden` | Reordenar (drag & drop); body `{ columnaAnteriorId?, columnaSiguienteId? }` |
| `DELETE` | `/api/proyectos/{proyectoId}/columnas/{id}` | Baja — **409** si la columna todavía tiene tareas (requisito 6.4) |

## API de Tareas

Anidada bajo el proyecto. Todas requieren JWT.

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/proyectos/{proyectoId}/tareas?columnaId=&responsableId=&prioridad=` | Listado con filtros opcionales (requisito deseable 7) |
| `POST` | `/api/proyectos/{proyectoId}/tareas` | Alta (se agrega al final de su columna) |
| `PUT` | `/api/proyectos/{proyectoId}/tareas/{id}` | Edición (título, descripción, prioridad, responsable) |
| `PUT` | `/api/proyectos/{proyectoId}/tareas/{id}/mover` | Traslado entre columnas y/o reordenamiento; body `{ columnaDestinoId, tareaAnteriorId?, tareaSiguienteId? }` |
| `DELETE` | `/api/proyectos/{proyectoId}/tareas/{id}` | Baja |

El `responsableId` se valida contra la tabla de usuarios antes de crear/editar (404 si no existe).

## API de Usuarios

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/usuarios` | Listado mínimo (`id`, `nombre`, `email`) para poblar el selector de responsable |

No forma parte del modelo de dominio mínimo del enunciado — es un endpoint de apoyo de solo
lectura, sin alta/edición/baja (fuera de alcance del challenge).

## Frontend — funcionalidad implementada

- **Proyectos** (`features/proyectos`): tabla paginada server-side (`p-table` con `lazy`),
  filtro por nombre con debounce, alta/edición en un diálogo (`proyecto-form`), baja con
  confirmación. Click en el nombre o ícono de tabla abre el tablero del proyecto.
- **Tablero Kanban** (`features/tablero`, ruta `/proyectos/:id/tablero`): columnas y tareas
  cargadas de la Api, drag & drop con Angular CDK (`@angular/cdk/drag-drop`) tanto para mover
  tareas entre columnas/reordenarlas como para reordenar columnas. Alta/edición de tareas en
  diálogo (`tarea-form`) con selector de responsable poblado desde `/api/usuarios`. Alta y
  renombrado de columnas inline; baja de columna con confirmación (muestra el mensaje de la
  Api si falla por tener tareas — requisito 6.4).
- **Filtros del tablero** (requisito deseable 7): búsqueda por texto, por prioridad y por
  responsable, resueltos en el cliente (los datos del tablero ya están completos en memoria).
  Con un filtro activo se **deshabilita el drag & drop** — ver justificación en
  `docs/decisiones.md`.
- Todas las peticiones pasan por `AuthInterceptor` (JWT) y por los servicios de
  `core/services/*.service.ts`, que reflejan 1:1 los DTOs/rutas del backend.

## Frontend — pendiente

- Tiempo real (SignalR): hoy el tablero no se sincroniza solo entre dos sesiones — hay que
  recargar para ver cambios de otro usuario.
- Descarga de reportes PDF/Excel (depende de que el backend los tenga implementados).
- Indicador de usuarios conectados (deseable, depende de SignalR).
- Tests de frontend (Karma/Jasmine).



## Diagrama del modelo de base de datos

*(Pendiente — se incrusta como imagen en esta sección junto con las migraciones incrementales
generadas por EF Core, antes de la entrega final.)*

## Pruebas automatizadas

Backend: `dotnet test` desde `backend/`. 16 tests, entre otros: el cálculo de la nueva
posición de una tarea al reordenarla (`CalculadorDeOrdenTests`, obligatorio por 6.9), las
reglas de negocio del agregado `Proyecto` (`ProyectoTests` — no eliminar columna con tareas,
no mover tarea a columna de otro proyecto, eliminar/renombrar), y el flujo completo de
creación y movimiento de tareas a través del caso de uso (`ProyectoServiceTests`,
`TareaServiceTests`), con repositorios fake — sin EF Core ni PostgreSQL de por medio.

Frontend: pendiente (Día 6 del plan de ejecución).

## Declaración de uso de asistentes de inteligencia artificial

Se utilizaron asistentes de IA durante todo el desarrollo, de la misma forma en que se
usarían en el trabajo diario del puesto:

- **Claude (Anthropic):** planificación del trabajo de 7 días; decisiones técnicas donde el
  enunciado dejaba margen de elección (tiempo real, librería de Excel, estrategia de orden,
  patrón de exportación dual, uso o no de MediatR, estrategia de hash de contraseña);
  scaffolding inicial de la solución .NET (entidades de dominio, puertos, DbContext,
  configuraciones de EF Core, autenticación JWT, `Program.cs`, Dockerfile); y actualización de
  esta documentación a medida que se verifica cada parte del backend.
- **ChatGPT (OpenAI):** apoyo para diagnosticar y resolver problemas puntuales al levantar el
  entorno localmente (contexto de build de Docker contaminado por `bin`/`obj` generados en
  Windows, y la diferencia de hostname `db` vs `localhost` al generar migraciones de EF Core
  fuera de Docker). Las soluciones resultantes (`.dockerignore`, aclaración de connection
  strings) están documentadas como decisiones técnicas en `docs/decisiones.md`.

El candidato es responsable de cada decisión tomada, puede justificarlas y sustituirlas en la
entrevista técnica, y revisó/verificó personalmente el funcionamiento real de cada componente
(build, migraciones, seed, login) antes de darlo por resuelto.

## Plan de ejecución

Ver el documento de planificación completo compartido junto con la entrega (7 días,
decisiones tecnológicas y cronograma día a día).
