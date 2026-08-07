# Bitácora de decisiones

Registro de decisiones técnicas del proyecto ScrumBoard (IDEASGROUP-REM-LAT-26-2907), con su
justificación y, donde el enunciado lo pide explícitamente, las alternativas evaluadas y
descartadas. Se consolida en el README final.

## Decisiones con alternativas explícitamente descartadas

El enunciado pide, para el README: *"tecnología de tiempo real elegida y alternativas
descartadas, estrategia de índices de ordenamiento, patrón aplicado en la exportación dual"*.
Estas son las decisiones donde hubo más de una opción razonable, resumidas acá; el detalle y
el contexto de cada una está desarrollado más abajo, en la sección correspondiente.

| Punto a decidir | Elegido | Descartado | Por qué |
|---|---|---|---|
| **Tiempo real** | SignalR | WebSocket puro; Server-Sent Events (SSE) | Integración nativa con la autenticación JWT de ASP.NET Core, grupos por proyecto ya resuelven "una sesión no recibe eventos de tableros a los que no está suscrita" (6.7), reconexión automática. WebSocket puro exige reimplementar reconexión/heartbeats/salas a mano sin beneficio real para el alcance del challenge. SSE es unidireccional servidor→cliente: alcanzaría para notificar, pero es un estándar menos maduro en .NET y no aporta nada que SignalR no resuelva ya mejor. |
| **Librería de reporte Excel** | ClosedXML | EPPlus | Licencia MIT sin ambigüedad comercial. EPPlus desde la v5 usa licencia Polyform Noncommercial/comercial de pago; para un ejercicio evaluativo es más simple no tener que justificar el modelo de licenciamiento. |
| **Estrategia de orden (drag & drop)** | Orden fraccionario (`decimal`), `CalculadorDeOrden.CalcularOrden` | Reindexado de enteros consecutivos en cada movimiento | Mover un elemento es un solo `UPDATE`, no un reindexado de toda la columna — más rápido y con menos conflictos de concurrencia entre sesiones simultáneas (relevante para el requisito de tiempo real, 6.7). El reindexado con enteros dispara N updates por cada drag. |
| **Patrón de exportación dual** | Strategy (`IReporteExporter`) + Factory (resolución por `IEnumerable<IReporteExporter>` en `ReporteService`), sobre un único DTO/query | Una consulta y una función de armado de reporte por cada formato (PDF/Excel duplicados) | Cumple el requisito 6.8 de extensibilidad: un tercer formato es una clase nueva sin tocar las existentes (Open/Closed). Duplicar la consulta por formato es exactamente el problema que el requisito busca evitar. |
| **Capas de aplicación** | Servicios de aplicación inyectados directo en el controller (`Controller → Service → Repository`) | MediatR (patrón mediator/CQRS con handlers) | Para el alcance de 7 días, MediatR agrega una dependencia y un concepto (pipeline de comandos/queries) sin necesidad real de desacoplar el envío de la ejecución. Menos piezas, más fácil de sustentar en la entrevista técnica. |
| **Hash de contraseña** | BCrypt + pepper de aplicación (`PASSWORD_PEPPER`, solo en variable de entorno) | Argon2 | BCrypt ya genera y embebe el salt automáticamente (cumple la parte de "salt" del enunciado sin trabajo extra) y alcanza de sobra para el alcance del challenge. Argon2 es más moderno, pero exige justificar parámetros de memoria/paralelismo que no aportan a la evaluación. |
| **Template frontend** | `sakai-ng` tag `17.0.0` (Angular 17.0.5, PrimeNG 17.2.0) | Rama `master` del mismo repositorio | La rama principal de Sakai ya está en Angular 21/PrimeNG 21; el enunciado pide específicamente Angular 17. El tag `17.0.0` es la versión del template contemporánea a esa versión de Angular, con licencia MIT verificada. |
| **Ruteo Angular** | `PathLocationStrategy` (rutas normales) | `HashLocationStrategy` (rutas con `#/`, la que trae el template por defecto) | El template está pensado para hosting estático tipo GitHub Pages. Acá la app se sirve desde nginx propio, así que tiene sentido usar rutas normales — a costa de necesitar un `try_files` de fallback en `nginx.conf` para que rutas resueltas del lado del cliente no den 404 al refrescar. |

## Planeamiento

**Arquitectura backend: hexagonal sin MediatR.**
`Controller → Application Service (interfaz) → Repository (interfaz) → Infrastructure`.
Ver tabla de arriba para la comparación completa con MediatR.

**Hash de contraseña: BCrypt + pepper de aplicación.**
BCrypt genera y embebe el salt por password automáticamente (cumple la parte de "salt" del
enunciado sin trabajo extra). El "pepper" es un secreto adicional de aplicación (no de fila),
se concatena al password antes de hashear, y vive solo en variable de entorno
(`PASSWORD_PEPPER`), nunca en la base de datos ni en el repositorio. Ver tabla de arriba para
la comparación con Argon2.

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
Ver tabla de arriba para la comparación con reindexado de enteros.

**Agregado `Proyecto` como raíz.**
`Columna` y `Tarea` solo pueden crearse (constructor `internal`) o moverse a través de
métodos del agregado `Proyecto` (`AgregarColumna`, `EliminarColumna`, `AgregarTarea`,
`MoverTarea`). Esto centraliza en un solo lugar las dos reglas de negocio explícitas del
enunciado: no eliminar una columna con tareas, y no permitir mover una tarea a una columna
de otro proyecto.

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
una migración de EF Core directamente desde Windows (`dotnet ef ...`, fuera del contenedor),
la connection string debe apuntar a `Host=localhost`, porque ese proceso corre fuera de la red
de Docker y no lee `.env` automáticamente (a diferencia de Docker Compose). Esta diferencia es
intencional y no debe "corregirse" cambiando `Host=db` dentro de `docker-compose.yml`.

**`.dockerignore` en `backend/`.** Excluye `bin/`, `obj/`, `.vs/`, `.vscode/`, artefactos de
test y `node_modules/` del contexto de build de Docker. Evita que binarios/objetos generados
localmente en Windows (con rutas propias de NuGet de esa máquina) contaminen el build dentro
del contenedor Linux.

**CORS.** Habilitado únicamente para el origen configurado en `FRONTEND_URL`
(`http://localhost:4200` en desarrollo), con `AllowCredentials()` para el handshake de SignalR
y `WithExposedHeaders("Content-Disposition")` para la descarga de reportes (ver más abajo).

**Estado verificado:** build de la imagen de la Api, contenedores `db` y `api` up/healthy,
migración `InitialCreate` aplicada, tablas `usuarios`/`proyectos`/`columnas`/`tareas`/
`__EFMigrationsHistory` creadas, seed de usuarios ejecutado, login con JWT verificado desde
Swagger.

**Pendiente para un entorno productivo (no bloqueante para el challenge):** las claves de
ASP.NET Core Data Protection se almacenan hoy en el filesystem efímero del contenedor
(warning visible en logs). En producción deberían persistirse fuera del contenedor.

## Frontend (Angular 17 + PrimeNG Sakai)

**Versión del template y tema visual:** ver tabla de arriba (alternativas descartadas).
Licencia MIT verificada en `LICENSE.md` del template (no es la línea comercial de PrimeNG,
pese a que el `package.json` original traía por error `"license": "PrimeNG Commercial"` — se
corrigió a `MIT` en nuestro `package.json`). Tema fijo `lara-light-blue`, sin selector de
temas: Sakai trae por defecto un panel flotante para cambiar tema/color en caliente
(`app-config`, botón engranaje), que se quitó completamente — el enunciado deja el diseño
visual a criterio del candidato y pide centrarse en funcionalidad, un selector de 15 temas no
aporta valor funcional y suma superficie de código sin usar.

**Reestructuración en capas (`core/` / `features/` / `shared/` / `layout/`).** El template
trae todo el código de negocio bajo `demo/components/*` (dashboard, uikit, prime blocks,
utilidades, páginas de ejemplo). Se eliminó todo lo que no es parte del challenge y se
reorganizó lo que sí se usa:
- `layout/` — el "chrome" de Sakai (topbar, sidebar, menú, footer), sin tocar su mecánica interna.
- `core/` — servicios transversales: `AuthService`, `authGuard` (funcional, Angular 17),
  `AuthInterceptor` (adjunta el JWT y maneja 401), y los servicios HTTP/utilidades de cada
  entidad (`ProyectoService`, `ColumnaService`, `TareaService`, `UsuarioService`,
  `ReporteService`, `TableroRealtimeService`).
- `features/auth/login` — login real conectado a `POST /api/auth/login`.
- `features/proyectos` — CRUD completo (tabla paginada, filtro, alta/edición/baja).
- `features/tablero` — Kanban con drag & drop, filtros, reportes, tiempo real.

**Archivos de entorno (`environment.ts` / `environment.prod.ts`).** Cumplen el requisito 6.1
de configuración externa sin URLs embebidas en componentes/servicios: `apiUrl` y `signalRUrl`
viven en un único lugar y se inyectan por `import { environment } from '...'`. Importante:
Angular resuelve estos archivos en **tiempo de compilación** (vía `fileReplacements` en
`angular.json`, que el template no traía configurado y se agregó), no en runtime del
contenedor — a diferencia del backend, acá no hay variables de entorno de Docker que
sustituyan estos valores después del build. Si cambian los puertos/hosts, hay que editar
`environment.prod.ts` y reconstruir la imagen. `.env` documenta los valores esperados
(`API_URL`, `SIGNALR_URL`) para que ambos queden sincronizados.

**Frontend dockerizado con build multi-stage + nginx.** `frontend/Dockerfile` compila con
`ng build --configuration production` en una etapa `node:20-alpine` y sirve el resultado con
`nginx:alpine`, con `frontend/nginx.conf` agregando el `try_files` de SPA que exige
`PathLocationStrategy` (ver tabla de arriba).

## CRUD de Proyectos (backend)

**Listado paginado: proyección directa a DTO, no el agregado completo.** El puerto
`IProyectoRepository.ListarPaginadoAsync` devuelve `ProyectoDto` directamente (vía `.Select`
de EF Core), en vez de `Proyecto` con `.Include(Columnas).Include(Tareas)`. El conteo de
columnas/tareas por proyecto se resuelve como subquery `COUNT` en SQL, sin traer esas
colecciones completas a memoria para cada fila del listado. Las operaciones de un solo
proyecto (`ObtenerPorIdAsync`, alta/edición/baja) sí cargan el agregado completo — ahí el
volumen es acotado (un proyecto a la vez) y se necesita para aplicar las reglas de negocio del
agregado `Proyecto`.

**Filtro por nombre con `ILIKE` (Npgsql).** Coincidencia parcial case-insensitive resuelta en
PostgreSQL (`EF.Functions.ILike`), no trayendo todos los proyectos para filtrar en memoria.

**Middleware centralizado de excepciones (`ExceptionHandlingMiddleware`).** Traduce
`RecursoNoEncontradoException` → 404, `CredencialesInvalidasException` → 401,
`DomainException` (reglas de negocio del dominio, ej. "no eliminar columna con tareas") → 409,
`ArgumentException` (validaciones de entidad) → 400, y cualquier otra excepción → 500 sin
exponer detalles internos (se loguea el detalle completo del lado del servidor). Los
controllers ya no repiten `try/catch` en cada acción — quedan como simples orquestadores del
caso de uso.

**Swagger con autenticación Bearer.** Se agregó `AddSecurityDefinition`/`AddSecurityRequirement`
para poder probar `/api/proyectos` (protegido con JWT) directo desde Swagger UI, pegando el
token que devuelve `/api/auth/login` en el botón "Authorize".

**Sin cambios de esquema de base de datos.** El CRUD de Proyectos usa las entidades y la
migración `InitialCreate` ya existentes desde el día 1 — no hizo falta una migración nueva.

## CRUD de Columnas y Tareas (backend)

**Sin `IColumnaRepository` ni `ITareaRepository`.** Ni Columna ni Tarea son raíces de
agregado — son entidades hijas de `Proyecto` (ver decisión del día 1). Todas sus operaciones
pasan por `IProyectoRepository` (cargando el agregado completo con
`ObtenerConTableroAsync`) y los métodos del propio `Proyecto`. `ColumnaService` y
`TareaService` no tienen su propio repositorio.

**Mutadores de `Columna`/`Tarea` marcados `internal`.** `Renombrar`, `CambiarOrden` (en
`Columna`) y `Editar` (en `Tarea`) pasaron de `public` a `internal`: solo se pueden invocar
desde dentro del ensamblado `ScrumBoard.Domain`, en la práctica solo desde los métodos del
agregado `Proyecto` (`RenombrarColumna`, `ReordenarColumna`, `EditarTarea`, `EliminarTarea`).
Objetivo: que **toda** mutación de columnas/tareas tenga un único punto de entrada auditable,
sin excepciones — ni siquiera Application puede mutarlas "por el costado" llamando directo a
la entidad hija.

**El cálculo de la nueva posición vive en Application, no en el agregado.** `ColumnaService`
y `TareaService` buscan los vecinos (por id, dentro del agregado ya cargado en memoria),
llaman a `CalculadorDeOrden.CalcularOrden` (dominio puro, sin dependencias) y recién con el
valor ya calculado invocan `Proyecto.ReordenarColumna`/`MoverTarea`. El agregado nunca
recibe "muévase entre estos dos ids": recibe directamente el `decimal` ya calculado. Mantiene
al agregado simple y a `CalculadorDeOrden` testeable de forma aislada.

**Al mover una tarea, los vecinos se buscan dentro de la columna destino.** Una tarea puede
estar cambiando de columna en el mismo movimiento; buscar el vecino "por id en todo el
proyecto" sin filtrar por columna sería un bug (podría encontrar una tarea con ese id en la
columna equivocada). `TareaService.ObtenerOrdenTareaEnColumnaDestino` filtra explícitamente
por `ColumnaDestinoId`.

**Alta de columna/tarea siempre al final.** Nueva columna o tarea usa
`CalculadorDeOrden.CalcularOrden(ultimoOrden, null)` — mismo algoritmo que el reordenamiento,
sin caso especial para "alta".

**Se valida que el `ResponsableId` exista antes de crear/editar una tarea.** Sin esto, un id
inválido solo se detectaría en `SaveChangesAsync` como una violación de foreign key de
PostgreSQL — un error 500 genérico y poco claro. `TareaService` valida contra
`IUsuarioRepository` primero y devuelve un 404 explícito.

**Nuevo endpoint de solo lectura `GET /api/usuarios`.** No está en el modelo de dominio
mínimo del enunciado, pero sin una forma de listar los usuarios precargados, el frontend no
tendría cómo poblar el selector de "responsable" al crear/editar una tarea (requisito 6.5).
Se agregó `IUsuarioRepository.ListarTodosAsync` + `UsuarioService` + `UsuariosController`,
sin alta/edición/baja de usuarios (fuera de alcance del challenge).

**Sin cambios de esquema de base de datos** (de nuevo): Columna y Tarea ya estaban modeladas
y migradas desde el día 1; este paso fue enteramente de Application/Api.

## Nivelación del frontend: CRUD de Proyectos y Tablero Kanban

**Bug real encontrado al integrar: enums viajaban como número, no como texto (parte 1 —
respuestas HTTP normales).** `System.Text.Json` serializa `enum` como su valor numérico por
defecto; el frontend (por diseño, ver modelos en `core/models/`) espera/envía
`Prioridad`/`EstadoProyecto` como string (`"Media"`, `"Planificado"`). Se agregó
`JsonStringEnumConverter` en `Program.cs` (`AddControllers().AddJsonOptions(...)`).
Importante: esto es independiente de `.HasConversion<string>()` en las configuraciones de EF
Core — esa conversión controla cómo se guarda el enum en PostgreSQL; el converter de
`AddJsonOptions` controla cómo viaja en el JSON de las respuestas HTTP. Ver más abajo la
"parte 2" de este mismo bug, encontrada después en los eventos de SignalR.

**Los componentes de formulario (`proyecto-form`, `tarea-form`) se guardan a sí mismos.**
En vez de que el componente padre reciba los datos del formulario y haga el `POST`/`PUT`, el
propio diálogo llama al servicio HTTP y emite `(guardado)` recién cuando la petición
resuelve. El padre solo necesita reaccionar a `(guardado)` recargando su listado — no conoce
la forma del payload ni maneja el estado de carga/error del formulario. Mismo patrón para
ambos formularios, así que es fácil de reconocer y replicar si se agrega un tercero.

**Los filtros del tablero deshabilitan el drag & drop mientras están activos.** El cálculo de
"vecino anterior/siguiente" para el reordenamiento fraccionario asume que el índice dentro
del array **completo** de la columna coincide con el índice visual. Ocultar tarjetas
filtradas con `*ngIf` rompería esa correspondencia. En vez de resolverlo con una estructura
paralela filtrada + lógica de mapeo de índices — complejidad innecesaria para un requisito
opcional (7, filtros) que no puede comprometer uno obligatorio (6.6, drag & drop) —, se optó
por deshabilitar `cdkDrag`/`cdkDropList` (`[cdkDragDisabled]`/`[cdkDropListDisabled]`)
mientras `hayFiltroActivo`, con un aviso visible en la UI. Las tarjetas filtradas se ocultan
con `[hidden]` (no `*ngIf`), así siguen "ocupando su lugar" en la lista de CDK sin alterar
índices cuando el usuario vuelve a habilitar el drag quitando el filtro.

**Filtros resueltos en el cliente, no contra la Api.** El tablero ya trae todas las tareas
del proyecto en memoria para poder renderizar el Kanban completo (no tiene sentido paginar un
tablero). Filtrar por texto/prioridad/responsable localmente evita una ida y vuelta a la Api
por cada tecla escrita, aunque el backend también soporta estos mismos filtros por
querystring (`GET /api/proyectos/{id}/tareas?...`).

**Reversión ante error: resincronizar contra el servidor, no revertir el array a mano.**
Si `mover`/`reordenar` falla, se llama a `cargarTodo()` en vez de deshacer manualmente el
`moveItemInArray`/`transferArrayItem` ya aplicado de forma optimista. Es una reversión más
simple y más confiable (siempre termina reflejando el estado real de la base de datos) a
costa de un round-trip extra solo en el caso de error, que no es el camino feliz.

**Alta de columna/tarea siempre recarga el tablero completo tras guardar**, en vez de
insertar el nuevo elemento a mano en el array local. Evita tener que replicar en el
frontend la lógica de "dónde cae la nueva columna/tarea" cuando el backend ya la calculó
(orden fraccionario) — se confía en la respuesta de la Api como fuente de verdad.

**`GET /api/usuarios` se consume tal cual** para poblar el `p-dropdown` de responsable en
`tarea-form`, y para mostrar el nombre del responsable en cada tarjeta del tablero
(`obtenerNombreUsuario`, resuelto en memoria contra la lista ya cargada — sin un `GET` por
tarjeta).

## Tiempo real (SignalR)

**El adaptador `SignalRRealtimeNotifier` vive en Api, no en Infrastructure.** Necesita
`IHubContext<TableroHub>`, disponible de forma nativa en un proyecto `Microsoft.NET.Sdk.Web`
sin agregar ningún paquete NuGet extra. Ponerlo en Infrastructure hubiera exigido referenciar
`Microsoft.AspNetCore.SignalR.Core` ahí solo para este adaptador. `Application` solo conoce
el puerto `IRealtimeNotifier`; ni Domain ni Application saben que existe SignalR.

**`IRealtimeNotifier` tipado con los DTOs reales (`TareaDto`/`ColumnaDto`), no `object`.**
Da chequeo de tipos en tiempo de compilación tanto en `TareaService`/`ColumnaService` (quién
llama) como en `SignalRRealtimeNotifier` (quién implementa) — evita que un cambio de forma en
el DTO rompa silenciosamente el payload que le llega al cliente.

**Un grupo de SignalR por proyecto (`TableroHub.NombreGrupo`).** Implementa directamente el
requisito "una sesión no recibe eventos de tableros a los que no está suscrita" (6.7): el
cliente se une al grupo `proyecto-{id}` recién al entrar al tablero (`SuscribirseAProyecto`)
y sale al destruir el componente (`DesuscribirseDeProyecto` + `connection.stop()`). No hace
falta limpiar grupos manualmente en `OnDisconnectedAsync`: SignalR remueve la conexión de
todos sus grupos automáticamente al desconectarse.

**Se notifican también los eventos de Columna, no solo los de Tarea.** El enunciado (6.7)
habla explícitamente de tareas; se extendió el mismo mecanismo a columnas (alta, renombrado,
reordenamiento, baja) para que el tablero se sienta completamente sincronizado — administrar
columnas también es parte del flujo de trabajo colaborativo (6.4), y el costo de agregarlo
con el mismo `IRealtimeNotifier` ya existente fue mínimo.

**Autenticación del Hub: mismo JWT, por query string.** `JwtBearerEvents.OnMessageReceived`
en `Program.cs` acepta `access_token` por query string para paths que empiezan con `/hubs` —
el navegador no puede mandar headers custom en el handshake de WebSocket, así que el cliente
SignalR (`accessTokenFactory`) manda el token por ahí en vez de por el header `Authorization`
habitual.

**Aplicación de eventos entrantes: idempotente, no un simple "recargar todo".** El cliente
Angular (`TableroRealtimeService` + los métodos `aplicar*` de `TableroComponent`) actualiza el
estado local en memoria en vez de volver a pedirle todo el tablero a la Api cada vez que llega
un evento. Cada `aplicar*` primero saca el elemento por id de donde esté y recién después lo
reinserta en su posición correcta: si el evento que llega es el eco del propio cambio que
esta sesión ya aplicó de forma optimista (SignalR reenvía a **todo** el grupo, incluida la
conexión que originó el cambio), el resultado neto es el mismo estado, no un duplicado.

**Reconexión: hay que volver a unirse al grupo.** `withAutomaticReconnect()` del cliente
SignalR reconecta la conexión TCP/WebSocket sola, pero **no** recuerda a qué grupos
pertenecía la conexión anterior (ligados al `ConnectionId` anterior, que cambia).
`TableroRealtimeService` reinvoca `SuscribirseAProyecto` en el handler `onreconnected`.

**Bug real encontrado al integrar: enums viajaban como número (parte 2 — eventos del Hub).**
El `JsonStringEnumConverter` agregado en `AddControllers().AddJsonOptions(...)` (ver más
arriba) **no** cubre los mensajes que emite el Hub de SignalR — `AddSignalR()` tiene su
propio serializador JSON, completamente independiente del de los controllers. Síntoma
concreto: al mover una tarea, la tarjeta y el selector de prioridad del formulario mostraban
un número (`1`, `2`...) en vez del texto, porque el evento `TareaMovida` (que la propia sesión
también recibe, al estar en el grupo) traía `Prioridad` sin convertir y pisaba el estado local
ya correcto. Fix: `builder.Services.AddSignalR().AddJsonProtocol(options =>
options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))` en
`Program.cs` — mismo converter, aplicado al protocolo JSON de SignalR explícitamente, porque
son dos pipelines de serialización distintos que no se configuran juntos.

## Reportes (PDF/Excel)

**"Una sola consulta" es dos round-trips a la base, no un único `SELECT`.**
`EfReporteProyectoQuery` hace una consulta para el encabezado del proyecto (nombre,
descripción) y otra con `JOIN` para las tareas (columna + responsable + prioridad). Se evaluó
forzarlo a un único `JOIN`, pero un proyecto sin tareas todavía haría que ese `JOIN` (Proyecto
⋈ Tarea ⋈ Columna ⋈ Usuario) devuelva cero filas — perdiendo hasta el nombre del proyecto en
el encabezado del reporte. El requisito 6.8 ("una sola consulta... alimentan ambos formatos")
se interpreta como: **una sola consulta compartida por ambos formatos** (no una consulta
distinta para PDF y otra para Excel, ni N+1 por tarea) — no como un único `SELECT` literal.

**QuestPDF en modo Community.** `QuestPDF.Settings.License = LicenseType.Community` se
declara una sola vez al arrancar la Api (`Program.cs`, antes de `WebApplication.CreateBuilder`
para garantizar que corra antes de cualquier `GeneratePdf()`). Community es gratuita para
el uso de este challenge.

**Nombre de archivo armado en el backend, no en el frontend.** `ReporteService` sluggifica el
nombre del proyecto (minúsculas, sin acentos, sin espacios: `reporte-sprint-agil-1.pdf`) y lo
manda en el header `Content-Disposition` de la respuesta HTTP — el frontend solo lo lee y lo
usa para la descarga.

**CORS: hubo que exponer `Content-Disposition` explícitamente.** Por especificación,
`Content-Disposition` no es uno de los headers de respuesta que el navegador expone al
JavaScript de una request cross-origin salvo que el servidor lo declare con
`Access-Control-Expose-Headers`. Sin `WithExposedHeaders("Content-Disposition")` en la
política de CORS, la descarga funcionaba pero el frontend no podía leer el nombre real —
el archivo se bajaba con un nombre genérico.

**Tests de los exportadores contra las clases reales, no fakes.** `PdfReporteExporter` y
`ExcelReporteExporter` no dependen de EF Core ni de una base de datos — son funciones puras
que reciben un DTO y devuelven `byte[]`. Se agregó una referencia de
`ScrumBoard.Application.Tests` a `ScrumBoard.Infrastructure` para poder instanciarlas
directamente en los tests y verificar que generan un PDF/XLSX real (firma de archivo válida).

## Bug corregido: alta de Columna/Tarea generaba UPDATE en vez de INSERT

**Síntoma:** `POST /api/proyectos/{id}/columnas` devolvía 500 con
`DbUpdateConcurrencyException: expected to affect 1 row(s), but actually affected 0` — el SQL
generado era un `UPDATE ... WHERE "Id" = @p3`, no un `INSERT`.

**Causa raíz:** `Proyecto.AgregarColumna`/`AgregarTarea` solo agregan la nueva entidad a la
colección **en memoria** del agregado (`_columnas.Add(...)`); nunca se llama
`_context.Columnas.Add(...)` explícitamente. EF Core recién "descubre" la nueva `Columna`
al hacer `DetectChanges()` durante `SaveChangesAsync()`, por navegación desde el `Proyecto`
ya trackeado — no por un `Add()` explícito. Para entidades descubiertas así, EF Core decide
el estado inicial según si la clave primaria ya tiene un valor distinto del default: como
`Columna`/`Tarea` generan su `Id` en el constructor (`Guid.NewGuid()`), EF las interpreta
como "podría ya existir en la base" y las marca `Modified` en lugar de `Added` → intenta un
`UPDATE` sobre una fila que todavía no existe.

`Proyecto` y `Usuario` no sufren esto porque se agregan con `AddAsync()`/`AddRange()`
explícitos — un `Add()` explícito siempre marca `Added`, sin importar el valor de la clave.

**Fix:** `builder.Property(x => x.Id).ValueGeneratedNever()` en las cuatro configuraciones
de EF Core (`ColumnaConfiguration`, `TareaConfiguration`, y por consistencia también
`ProyectoConfiguration`/`UsuarioConfiguration`, aunque a esas dos no les rompía nada). Le dice
a EF explícitamente que la aplicación siempre genera la clave — nunca la base ni un value
generator de EF — con lo que desaparece la ambigüedad y cualquier entidad nueva descubierta
por navegación se trackea correctamente como `Added`.

**Por qué no fue un parche puntual:** el fix no toca `ColumnaService`, `Proyecto` (agregado)
ni ningún controller — es exclusivamente configuración de mapeo EF Core en Infrastructure.
Se aplicó a las cuatro entidades por consistencia del modelo, no solo a `Columna` — `Tarea`
tenía exactamente el mismo patrón y hubiera fallado igual apenas se probara.

**¿Hizo falta una migración nueva?** No: `ValueGeneratedNever()` es metadata de EF Core
sobre quién genera el valor, no una característica de la columna en PostgreSQL (no había
ningún `DEFAULT` de base de datos configurado para estos `Id`).

## Tests de frontend

**Se instancia el componente directamente (`new TableroComponent(...)`), sin `TestBed`.**
Lo que se prueba (filtros, cálculo de vecinos al reordenar, aplicación de eventos de tiempo
real) es lógica de aplicación pura de la clase, no renderizado de template. Levantar
`TestBed` para esto obligaría a compilar/mockear todos los módulos de PrimeNG que usa el
componente sin que aporte nada a lo que se está verificando. Los servicios inyectados se
pasan como objetos mock mínimos (`as any`) con solo los métodos que cada test necesita.

**El cálculo de vecinos se extrajo a una función pura (`core/utils/reordenamiento.util.ts`,
`calcularVecinos`).** Antes vivía duplicada, inline, en `onDropTarea` y `onDropColumna`
(ambos necesitan "encontrar el elemento anterior/siguiente a uno dado en un array"). Se
extrajo por dos motivos: (1) elimina la duplicación entre tareas y columnas, y (2) permite
testear el cálculo de forma aislada, sin simular un evento `CdkDragDrop` completo ni
instanciar el componente — es una función pura, sin Angular ni RxJS.

**El test obligatorio de "cálculo de nueva posición" (6.9) se cubre de los dos lados.** El
backend lo cubre con el algoritmo numérico real (`CalculadorDeOrdenTests`); el frontend, con
`reordenamiento.util.spec.ts`, cubre la otra mitad del mismo problema: identificar
correctamente los dos vecinos del elemento movido dentro de la lista ya reordenada por el
drag & drop — son esos ids los que el cliente le manda a la Api. Un error ahí rompe el
reordenamiento aunque el algoritmo del backend esté perfecto.

## Pendientes conocidos al momento de la entrega

**Indicador de usuarios conectados al tablero (requisito deseable, sección 7): no
implementado.** Se evaluó trackear las conexiones activas por proyecto en memoria (un
diccionario en el proceso de la Api, ya que es un dato efímero que no tiene sentido
persistir) y emitir un evento adicional del Hub con la cantidad, pero no llegó a
implementarse por cuestiones de tiempo. Filtros y búsqueda de tareas (los otros dos ítems de la sección 7)
sí están completos.

**Diagrama ER embebido en el README: Completo.** El resto de los entregables de la sección 8
(instrucciones, decisiones arquitectónicas, alternativas descartadas, declaración de uso de
IA) están en el README; el diagrama del modelo de base de datos como imagen todavía no se
generó.