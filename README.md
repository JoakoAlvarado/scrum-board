# ScrumBoard

Módulo inicial de una plataforma de gestión Scrum: proyectos, columnas configurables y un
tablero kanban con tiempo real, drag & drop persistente, y exportación de reportes en PDF y
Excel. Desarrollado como prueba técnica para IDEASGROUP (proceso IDEASGROUP-REM-LAT-26-2907).

## Estado actual

| Componente | Estado |
|---|---|
| API .NET 8 (arquitectura hexagonal) | OK |
| PostgreSQL 16 (Docker) | OK healthy |
| Docker Compose (db + api) | OK |
| EF Core — migración `InitialCreate` | OK aplicada |
| Tablas en base de datos | OK verificadas |
| Seed de usuarios iniciales | OK |
| Autenticación JWT + BCrypt/Pepper | OK login verificado |
| Swagger | OK |
| CORS | OK configurado para el frontend |
| Frontend Angular + PrimeNG Sakai | Pendiente en desarrollo |
| Tiempo real (SignalR), reportes (PDF/Excel) | Pendiente pendiente |

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
| Frontend | Angular 17, PrimeNG (plantilla Sakai), TypeScript, SCSS |
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

- **Api:** http://localhost:8080
- **Swagger:** http://localhost:8080/swagger *(habilitado porque `ASPNETCORE_ENVIRONMENT=Development`)*
- **PostgreSQL:** `localhost:5432` — base `scrumboard`, usuario `scrumboard_user` *(password:
  ver `.env`, no se documenta en texto público)*
- **Frontend:** http://localhost:4200 *(pendiente de incorporar al compose)*

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
├── frontend/                            → Angular 17 + PrimeNG Sakai (en progreso)
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

## Información para desarrollar el frontend

- Base URL de la Api: `http://localhost:8080` (configurable vía `API_URL` en `.env`, se debe
  leer desde `environment.ts` en Angular, nunca embebida en componentes/servicios).
- Login: `POST /api/auth/login` con `{ email, password }`, devuelve `{ usuarioId, nombre,
  email, token, expiraUtc }`.
- El resto de los endpoints de negocio (proyectos, columnas, tareas) están protegidos con JWT
  Bearer y todavía no están implementados — ver estado actual arriba y el plan de ejecución.
- CORS ya admite `http://localhost:4200` como origen por defecto.
- El canal de tiempo real (SignalR) todavía no está implementado; la URL reservada para eso es
  `SIGNALR_URL` en `.env`.



## Diagrama del modelo de base de datos

*(Pendiente — se incrusta como imagen en esta sección junto con las migraciones incrementales
generadas por EF Core, antes de la entrega final.)*

## Pruebas automatizadas

Backend: `dotnet test` desde `backend/`. Incluye, entre otros, el cálculo de la nueva
posición de una tarea al reordenarla (`CalculadorDeOrdenTests`) y las reglas de negocio del
agregado `Proyecto` (`ProyectoTests`).

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
