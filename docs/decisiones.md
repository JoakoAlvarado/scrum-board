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
