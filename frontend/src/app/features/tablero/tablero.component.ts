import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Subscription, forkJoin } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

import { Proyecto } from 'src/app/core/models/proyecto.model';
import { Columna } from 'src/app/core/models/columna.model';
import { Prioridad, Tarea, PRIORIDADES } from 'src/app/core/models/tarea.model';
import { Usuario } from 'src/app/core/models/usuario.model';
import { ProyectoService } from 'src/app/core/services/proyecto.service';
import { ColumnaService } from 'src/app/core/services/columna.service';
import { TareaService } from 'src/app/core/services/tarea.service';
import { UsuarioService } from 'src/app/core/services/usuario.service';
import { TableroRealtimeService } from 'src/app/core/services/tablero-realtime.service';
import { ReporteService } from 'src/app/core/services/reporte.service';

interface ColumnaVista {
    columna: Columna;
    tareas: Tarea[];
}

@Component({
    selector: 'app-tablero',
    templateUrl: './tablero.component.html'
})
export class TableroComponent implements OnInit, OnDestroy {
    proyectoId!: string;
    proyecto: Proyecto | null = null;
    columnasVista: ColumnaVista[] = [];
    usuarios: Usuario[] = [];
    cargando = false;

    private suscripciones = new Subscription();

    descargandoPdf = false;
    descargandoExcel = false;

    // Filtros client-side (requisito deseable 7). Mientras hay un filtro activo se
    // deshabilita el drag & drop: reordenar sobre una lista filtrada rompe el cálculo
    // de índices/vecinos que espera la Api — ver docs/decisiones.md.
    prioridades = PRIORIDADES;
    filtroTexto = '';
    filtroPrioridad: Prioridad | null = null;
    filtroResponsableId: string | null = null;

    dialogoTareaVisible = false;
    tareaSeleccionada: Tarea | null = null;
    columnaDestinoNuevaTarea: string | null = null;

    nuevaColumnaNombre = '';
    agregandoColumna = false;
    columnaEnEdicionId: string | null = null;
    nombreColumnaEdicion = '';

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private proyectoService: ProyectoService,
        private columnaService: ColumnaService,
        private tareaService: TareaService,
        private usuarioService: UsuarioService,
        private tableroRealtime: TableroRealtimeService,
        private reporteService: ReporteService,
        private confirmationService: ConfirmationService,
        private messageService: MessageService
    ) {}

    ngOnInit(): void {
        this.proyectoId = this.route.snapshot.paramMap.get('id')!;
        this.cargarTodo();
        this.conectarTiempoReal();
    }

    ngOnDestroy(): void {
        this.suscripciones.unsubscribe();
        // Requisito 6.7: cierre correcto de la conexión y de las suscripciones al
        // destruir el componente, sin conexiones huérfanas.
        this.tableroRealtime.desuscribirseDeProyecto(this.proyectoId);
        this.tableroRealtime.desconectar();
    }

    private conectarTiempoReal(): void {
        this.tableroRealtime.suscribirseAProyecto(this.proyectoId);

        this.suscripciones.add(
            this.tableroRealtime.tareaCreada$.subscribe((tarea) => this.aplicarTareaCreadaOMovida(tarea))
        );
        this.suscripciones.add(
            this.tableroRealtime.tareaActualizada$.subscribe((tarea) => this.aplicarTareaActualizada(tarea))
        );
        this.suscripciones.add(
            this.tableroRealtime.tareaMovida$.subscribe((tarea) => this.aplicarTareaCreadaOMovida(tarea))
        );
        this.suscripciones.add(
            this.tableroRealtime.tareaEliminada$.subscribe((tareaId) => this.aplicarTareaEliminada(tareaId))
        );

        this.suscripciones.add(
            this.tableroRealtime.columnaCreada$.subscribe((columna) => this.aplicarColumnaCreada(columna))
        );
        this.suscripciones.add(
            this.tableroRealtime.columnaActualizada$.subscribe((columna) => this.aplicarColumnaActualizada(columna))
        );
        this.suscripciones.add(
            this.tableroRealtime.columnaReordenada$.subscribe((columna) => this.aplicarColumnaReordenada(columna))
        );
        this.suscripciones.add(
            this.tableroRealtime.columnaEliminada$.subscribe((columnaId) => this.aplicarColumnaEliminada(columnaId))
        );
    }

    // --- Aplicación de eventos entrantes al estado local ---
    // Todas estas funciones son idempotentes (buscan y sacan por id antes de volver a
    // insertar): si el evento que llega es el eco de un cambio que esta misma sesión ya
    // aplicó de forma optimista, el resultado neto es el mismo estado, no un duplicado.

    private aplicarTareaCreadaOMovida(tarea: Tarea): void {
        for (const cv of this.columnasVista) {
            const indiceExistente = cv.tareas.findIndex((t) => t.id === tarea.id);
            if (indiceExistente !== -1) cv.tareas.splice(indiceExistente, 1);
        }

        const columnaDestino = this.columnasVista.find((cv) => cv.columna.id === tarea.columnaId);
        if (!columnaDestino) return;

        columnaDestino.tareas.push(tarea);
        columnaDestino.tareas.sort((a, b) => a.orden - b.orden);
    }

    private aplicarTareaActualizada(tarea: Tarea): void {
        for (const cv of this.columnasVista) {
            const indice = cv.tareas.findIndex((t) => t.id === tarea.id);
            if (indice !== -1) {
                cv.tareas[indice] = tarea;
                return;
            }
        }
    }

    private aplicarTareaEliminada(tareaId: string): void {
        for (const cv of this.columnasVista) {
            cv.tareas = cv.tareas.filter((t) => t.id !== tareaId);
        }
    }

    private aplicarColumnaCreada(columna: Columna): void {
        if (this.columnasVista.some((cv) => cv.columna.id === columna.id)) return;

        this.columnasVista.push({ columna, tareas: [] });
        this.columnasVista.sort((a, b) => a.columna.orden - b.columna.orden);
    }

    private aplicarColumnaActualizada(columna: Columna): void {
        const cv = this.columnasVista.find((c) => c.columna.id === columna.id);
        if (cv) cv.columna = { ...cv.columna, nombre: columna.nombre };
    }

    private aplicarColumnaReordenada(columna: Columna): void {
        const cv = this.columnasVista.find((c) => c.columna.id === columna.id);
        if (!cv) return;

        cv.columna = { ...cv.columna, orden: columna.orden };
        this.columnasVista.sort((a, b) => a.columna.orden - b.columna.orden);
    }

    private aplicarColumnaEliminada(columnaId: string): void {
        this.columnasVista = this.columnasVista.filter((cv) => cv.columna.id !== columnaId);
    }

    get hayFiltroActivo(): boolean {
        return !!this.filtroTexto || !!this.filtroPrioridad || !!this.filtroResponsableId;
    }

    get idsColumnasParaConexion(): string[] {
        return this.columnasVista.map((c) => 'columna-' + c.columna.id);
    }

    cargarTodo(): void {
        this.cargando = true;

        forkJoin({
            proyecto: this.proyectoService.obtenerPorId(this.proyectoId),
            columnas: this.columnaService.listar(this.proyectoId),
            tareas: this.tareaService.listar(this.proyectoId),
            usuarios: this.usuarioService.listar()
        }).subscribe({
            next: ({ proyecto, columnas, tareas, usuarios }) => {
                this.proyecto = proyecto;
                this.usuarios = usuarios;
                this.columnasVista = columnas
                    .sort((a, b) => a.orden - b.orden)
                    .map((columna) => ({
                        columna,
                        tareas: tareas
                            .filter((t) => t.columnaId === columna.id)
                            .sort((a, b) => a.orden - b.orden)
                    }));
                this.cargando = false;
            },
            error: () => {
                this.cargando = false;
                this.messageService.add({ severity: 'error', summary: 'No se pudo cargar el tablero' });
            }
        });
    }

    volver(): void {
        this.router.navigate(['/proyectos']);
    }

    // --- Filtro visual de tarjetas (no toca el array real que usa el drag & drop) ---
    coincideFiltro(tarea: Tarea): boolean {
        const texto = this.filtroTexto.trim().toLowerCase();
        const coincideTexto = !texto || tarea.titulo.toLowerCase().includes(texto);
        const coincidePrioridad = !this.filtroPrioridad || tarea.prioridad === this.filtroPrioridad;
        const coincideResponsable = !this.filtroResponsableId || tarea.responsableId === this.filtroResponsableId;
        return coincideTexto && coincidePrioridad && coincideResponsable;
    }

    obtenerNombreUsuario(id: string): string {
        return this.usuarios.find((u) => u.id === id)?.nombre ?? '—';
    }

    // --- Drag & drop de tareas ---
    onDropTarea(event: CdkDragDrop<Tarea[]>, columnaDestino: ColumnaVista): void {
        if (this.hayFiltroActivo) return;

        const tareaMovida = event.previousContainer.data[event.previousIndex];

        if (event.previousContainer === event.container) {
            moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
        } else {
            transferArrayItem(event.previousContainer.data, event.container.data, event.previousIndex, event.currentIndex);
        }

        const lista = columnaDestino.tareas;
        const indice = lista.findIndex((t) => t.id === tareaMovida.id);
        const anterior = lista[indice - 1]?.id ?? null;
        const siguiente = lista[indice + 1]?.id ?? null;

        this.tareaService
            .mover(this.proyectoId, tareaMovida.id, {
                columnaDestinoId: columnaDestino.columna.id,
                tareaAnteriorId: anterior,
                tareaSiguienteId: siguiente
            })
            .subscribe({
                error: (err: HttpErrorResponse) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'No se pudo mover la tarea',
                        detail: err.error?.mensaje
                    });
                    this.cargarTodo(); // reversión visible: resincroniza contra el servidor
                }
            });
    }

    // --- Drag & drop de columnas ---
    onDropColumna(event: CdkDragDrop<ColumnaVista[]>): void {
        if (this.hayFiltroActivo) return;

        moveItemInArray(this.columnasVista, event.previousIndex, event.currentIndex);

        const columnaMovida = this.columnasVista[event.currentIndex].columna;
        const anterior = this.columnasVista[event.currentIndex - 1]?.columna.id ?? null;
        const siguiente = this.columnasVista[event.currentIndex + 1]?.columna.id ?? null;

        this.columnaService
            .reordenar(this.proyectoId, columnaMovida.id, { columnaAnteriorId: anterior, columnaSiguienteId: siguiente })
            .subscribe({
                error: (err: HttpErrorResponse) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'No se pudo reordenar la columna',
                        detail: err.error?.mensaje
                    });
                    this.cargarTodo();
                }
            });
    }

    // --- Alta / edición / borrado de columnas ---
    agregarColumna(): void {
        if (!this.nuevaColumnaNombre.trim()) return;

        this.agregandoColumna = true;
        this.columnaService.crear(this.proyectoId, { nombre: this.nuevaColumnaNombre.trim() }).subscribe({
            next: () => {
                this.nuevaColumnaNombre = '';
                this.agregandoColumna = false;
                this.cargarTodo();
            },
            error: () => {
                this.agregandoColumna = false;
                this.messageService.add({ severity: 'error', summary: 'No se pudo crear la columna' });
            }
        });
    }

    iniciarEdicionColumna(columna: Columna): void {
        this.columnaEnEdicionId = columna.id;
        this.nombreColumnaEdicion = columna.nombre;
    }

    confirmarEdicionColumna(columna: Columna): void {
        if (!this.nombreColumnaEdicion.trim() || this.nombreColumnaEdicion === columna.nombre) {
            this.columnaEnEdicionId = null;
            return;
        }

        this.columnaService.actualizar(this.proyectoId, columna.id, { nombre: this.nombreColumnaEdicion.trim() }).subscribe({
            next: (actualizada) => {
                columna.nombre = actualizada.nombre;
                this.columnaEnEdicionId = null;
            },
            error: () => {
                this.columnaEnEdicionId = null;
                this.messageService.add({ severity: 'error', summary: 'No se pudo renombrar la columna' });
            }
        });
    }

    eliminarColumna(columnaVista: ColumnaVista): void {
        this.confirmationService.confirm({
            header: 'Eliminar columna',
            message: `¿Eliminar la columna "${columnaVista.columna.nombre}"?`,
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.columnaService.eliminar(this.proyectoId, columnaVista.columna.id).subscribe({
                    next: () => this.cargarTodo(),
                    error: (err: HttpErrorResponse) => {
                        // 409: la columna todavía tiene tareas (regla de negocio 6.4).
                        this.messageService.add({
                            severity: 'error',
                            summary: 'No se pudo eliminar la columna',
                            detail: err.error?.mensaje
                        });
                    }
                });
            }
        });
    }

    // --- Alta / edición / borrado de tareas ---
    nuevaTarea(columnaId: string): void {
        this.tareaSeleccionada = null;
        this.columnaDestinoNuevaTarea = columnaId;
        this.dialogoTareaVisible = true;
    }

    editarTarea(tarea: Tarea): void {
        this.tareaSeleccionada = tarea;
        this.columnaDestinoNuevaTarea = null;
        this.dialogoTareaVisible = true;
    }

    eliminarTarea(tarea: Tarea): void {
        this.confirmationService.confirm({
            header: 'Eliminar tarea',
            message: `¿Eliminar la tarea "${tarea.titulo}"?`,
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.tareaService.eliminar(this.proyectoId, tarea.id).subscribe({
                    next: () => this.cargarTodo(),
                    error: () => this.messageService.add({ severity: 'error', summary: 'No se pudo eliminar la tarea' })
                });
            }
        });
    }

    // --- Reportes (requisito 6.8) ---
    descargarPdf(): void {
        this.descargandoPdf = true;
        this.reporteService.descargarPdf(this.proyectoId).subscribe({
            next: (archivo) => {
                this.descargandoPdf = false;
                this.disparaDescarga(archivo.blob, archivo.nombreArchivo);
            },
            error: () => {
                this.descargandoPdf = false;
                this.messageService.add({ severity: 'error', summary: 'No se pudo generar el PDF' });
            }
        });
    }

    descargarExcel(): void {
        this.descargandoExcel = true;
        this.reporteService.descargarExcel(this.proyectoId).subscribe({
            next: (archivo) => {
                this.descargandoExcel = false;
                this.disparaDescarga(archivo.blob, archivo.nombreArchivo);
            },
            error: () => {
                this.descargandoExcel = false;
                this.messageService.add({ severity: 'error', summary: 'No se pudo generar el Excel' });
            }
        });
    }

    private disparaDescarga(blob: Blob, nombreArchivo: string): void {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = nombreArchivo;
        link.click();
        window.URL.revokeObjectURL(url);
    }
}
