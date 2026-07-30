# ScrumBoard

Módulo inicial de una plataforma de gestión Scrum: proyectos, columnas configurables y un
tablero kanban con tiempo real, drag & drop persistente, y exportación de reportes en PDF y
Excel. Desarrollado como prueba técnica para IDEASGROUP (proceso IDEASGROUP-REM-LAT-26-2907).

> **Estado:** en desarrollo activo. Este README se actualiza día a día — ver
> [`docs/decisiones.md`](docs/decisiones.md) para el detalle cronológico de cada decisión.

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

## Cómo levantar el proyecto

```bash
git clone <url-del-repo>
cd scrum-board
cp .env.example .env
docker compose up -d --build
```

- Api: http://localhost:8080 (Swagger en `/swagger`, solo en entorno Development)
- Frontend: http://localhost:4200 *(se habilita cuando el scaffolding de Angular esté
  incorporado al compose — ver estado actual más abajo)*

Usuarios precargados por migración semilla:

| Email | Password |
|---|---|
| `admin@scrumboard.local` | `Admin123!` |
| `usuario@scrumboard.local` | `Usuario123!` |

Las migraciones de EF Core se aplican automáticamente al arrancar la Api (`Database.MigrateAsync`
en `Program.cs`), no requiere pasos manuales adicionales.

## Estructura del repositorio

```
scrum-board/
├── backend/
│   ├── ScrumBoard.sln
│   ├── src/
│   │   ├── ScrumBoard.Domain/          → entidades, reglas de negocio, sin dependencias
│   │   ├── ScrumBoard.Application/     → casos de uso, puertos (interfaces), DTOs
│   │   ├── ScrumBoard.Infrastructure/  → EF Core, BCrypt, JWT, (luego) ClosedXML/QuestPDF
│   │   └── ScrumBoard.Api/             → controllers, Program.cs, Dockerfile
│   └── tests/
│       └── ScrumBoard.Application.Tests/
├── frontend/                            → Angular 17 + PrimeNG Sakai (en progreso)
├── docs/
│   └── decisiones.md                    → bitácora de decisiones técnicas
├── docker-compose.yml
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

## Diagrama del modelo de base de datos

*(Pendiente — se incrusta como imagen en esta sección junto con las migraciones incrementales
generadas por EF Core, antes de la entrega final.)*

## Pruebas automatizadas

Backend: `dotnet test` desde `backend/`. Incluye, entre otros, el cálculo de la nueva
posición de una tarea al reordenarla (`CalculadorDeOrdenTests`) y las reglas de negocio del
agregado `Proyecto` (`ProyectoTests`).

Frontend: pendiente (Día 6 del plan de ejecución).

## Declaración de uso de asistentes de inteligencia artificial

Se utilizó **Claude Code** durante todo el desarrollo, de la misma forma en que se
usaría en el trabajo diario del puesto:

- **Planificación y arquitectura:** definición del plan de trabajo de 7 días, y de las
  decisiones técnicas donde el enunciado dejaba margen de elección (tiempo real, librería de
  Excel, estrategia de orden, patrón de exportación dual, uso o no de MediatR, estrategia de
  hash de contraseña).
- **Generación de código:** scaffolding inicial de la solución .NET (entidades de dominio,
  puertos, DbContext, configuraciones de EF Core, autenticación JWT, `Program.cs`,
  Dockerfile) y su posterior revisión y ajuste manual.
- **Revisión:** segunda opinión sobre decisiones de diseño y sobreingeniería antes de
  incorporarlas.

## Plan de ejecución

Ver el documento de planificación completo compartido junto con la entrega (7 días,
decisiones tecnológicas y cronograma día a día).
