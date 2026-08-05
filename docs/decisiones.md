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

## Frontend (Angular 17 + PrimeNG Sakai)

**Versión del template: tag `17.0.0` de `primefaces/sakai-ng`, no `master`.** El repositorio
de Sakai en su rama principal ya está en Angular 21/PrimeNG 21; el enunciado pide
específicamente Angular 17. Se usó el tag `17.0.0` (Angular 17.0.5, PrimeNG 17.2.0), que es la
versión del template contemporánea a esa versión de Angular. Licencia MIT verificada en
`LICENSE.md` del template (no es la línea comercial de PrimeNG, pese a que el `package.json`
original traía por error `"license": "PrimeNG Commercial"` — se corrigió a `MIT` en nuestro
`package.json`).

**Tema fijo `lara-light-blue`, sin selector de temas.** Sakai trae por defecto un panel
flotante para cambiar tema/color en caliente (`app-config`, botón engranaje). Se quitó
completamente: el enunciado deja el diseño visual a criterio del candidato y pide centrarse en
funcionalidad, no en aspecto gráfico — un selector de 15 temas no aporta valor funcional y
suma superficie de código sin usar. Paleta fija: azul/celeste + blanco.

**Reestructuración en capas (`core/` / `features/` / `shared/` / `layout/`).** El template
trae todo el código de negocio bajo `demo/components/*` (dashboard, uikit, prime blocks,
utilidades, páginas de ejemplo). Se eliminó todo lo que no es parte del challenge y se
reorganizó lo que sí se usa:
- `layout/` — el "chrome" de Sakai (topbar, sidebar, menú, footer), sin tocar su mecánica interna.
- `core/` — servicios transversales: `AuthService`, `authGuard` (funcional, Angular 17),
  `AuthInterceptor` (adjunta el JWT y maneja 401).
- `features/auth/login` — login real conectado a `POST /api/auth/login` (reemplaza el demo
  hardcodeado "Welcome, Isabel!").
- `features/proyectos` — placeholder honesto (sin datos ni funcionalidad inventada) hasta que
  el CRUD de proyectos esté implementado en el backend.

**`PathLocationStrategy` en vez de `HashLocationStrategy`.** El template trae por defecto
rutas con `#/` (pensado para hosting estático tipo GitHub Pages). Se quitó ese override: la
app se sirve desde nuestro propio nginx, así que tiene sentido usar rutas normales. Esto
exige un `try_files` de fallback en la config de nginx para que rutas como `/proyectos`
resueltas del lado del cliente no devuelvan 404 al refrescar — ver `frontend/nginx.conf`.

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
`nginx:alpine`, con `frontend/nginx.conf` agregando el `try_files` de SPA mencionado arriba.

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
agregado `Proyecto` (`RenombrarColumna`, `ReordenarColumna`, `EditarTarea`, `EliminarTarea`,
agregados en este paso). Objetivo: que **toda** mutación de columnas/tareas tenga un único
punto de entrada auditable, sin excepciones — ni siquiera Application puede mutarlas
"por el costado" llamando directo a la entidad hija.

**El cálculo de la nueva posición vive en Application, no en el agregado.** `ColumnaService`
y `TareaService` buscan los vecinos (por id, dentro del agregado ya cargado en memoria),
llaman a `CalculadorDeOrden.CalcularOrden` (dominio puro, sin dependencias) y recién con el
valor ya calculado invocan `Proyecto.ReordenarColumna`/`MoverTarea`. El agregado nunca
recibe "muévase entre estos dos ids": recibe directamente el `decimal` ya calculado. Mantiene
al agregado simple y a `CalculadorDeOrden` testeable de forma aislada (como ya lo estaba desde
el día 1).

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
`IUsuarioRepository` primero y devuelve un 404 explícito ("Usuario con id... no fue
encontrado").

**Nuevo endpoint de solo lectura `GET /api/usuarios`.** No está en el modelo de dominio
mínimo del enunciado, pero sin una forma de listar los usuarios precargados, el frontend no
tendría cómo poblar el selector de "responsable" al crear/editar una tarea (requisito 6.5).
Se agregó `IUsuarioRepository.ListarTodosAsync` + `UsuarioService` + `UsuariosController`,
sin alta/edición/baja de usuarios (fuera de alcance del challenge).

**Sin cambios de esquema de base de datos** (de nuevo): Columna y Tarea ya estaban modeladas
y migradas desde el día 1; este paso fue enteramente de Application/Api.

## Nivelación del frontend: CRUD de Proyectos y Tablero Kanban

**Bug real encontrado al integrar: enums viajaban como número, no como texto.**
`System.Text.Json` serializa `enum` como su valor numérico por defecto; el frontend (por
diseño, ver modelos en `core/models/`) espera/envía `Prioridad`/`EstadoProyecto` como string
(`"Media"`, `"Planificado"`). Se agregó `JsonStringEnumConverter` en `Program.cs`
(`AddControllers().AddJsonOptions(...)`). Importante: esto es independiente de
`.HasConversion<string>()` en las configuraciones de EF Core — esa conversión controla cómo
se guarda el enum en PostgreSQL; el converter de `AddJsonOptions` controla cómo viaja en el
JSON de la Api. Son dos serializadores distintos para dos capas distintas.

**Los componentes de formulario (`proyecto-form`, `tarea-form`) se guardan a sí mismos.**
En vez de que el componente padre reciba los datos del formulario y haga el `POST`/`PUT`, el
propio diálogo llama al servicio HTTP y emite `(guardado)` recién cuando la petición
resuelve. El padre solo necesita reaccionar a `(guardado)` recargando su listado — no conoce
la forma del payload ni maneja el estado de carga/error del formulario. Mismo patrón para
ambos formularios, así que es fácil de reconocer y replicar si se agrega un tercero.

**Los filtros del tablero deshabilitan el drag & drop mientras están activos.** El cálculo de
"vecino anterior/siguiente" para el reordenamiento fraccionario (`CalculadorDeOrden` en el
backend) asume que el índice dentro del array **completo** de la columna coincide con el
índice visual. Ocultar tarjetas filtradas con `*ngIf` rompería esa correspondencia (el índice
visual ya no sería el índice real). En vez de resolverlo con una estructura paralela
filtrada + lógica de mapeo de índices — complejidad innecesaria para un requisito opcional
(7, filtros) que no puede comprometer uno obligatorio (6.6, drag & drop) —, se optó por
deshabilitar `cdkDrag`/`cdkDropList` (`[cdkDragDisabled]`/`[cdkDropListDisabled]`) mientras
`hayFiltroActivo`, con un aviso visible en la UI. Las tarjetas filtradas se ocultan con
`[hidden]` (no `*ngIf`), así siguen "ocupando su lugar" en la lista de CDK sin alterar
índices cuando el usuario vuelve a habilitar el drag quitando el filtro.

**Filtros resueltos en el cliente, no contra la Api.** El tablero ya trae todas las tareas
del proyecto en memoria para poder renderizar el Kanban completo (no tiene sentido paginar un
tablero). Filtrar por texto/prioridad/responsable localmente evita una ida y vuelta a la Api
por cada tecla escrita, aunque el backend también soporta estos mismos filtros por
querystring (`GET /api/proyectos/{id}/tareas?...`) para el caso en que se necesiten resueltos
en el servidor (por ejemplo, si más adelante el tablero pagina).

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
