import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { Proyecto } from 'src/app/core/models/proyecto.model';
import { ProyectoService } from 'src/app/core/services/proyecto.service';

@Component({
    selector: 'app-proyectos',
    templateUrl: './proyectos.component.html'
})
export class ProyectosComponent implements OnInit {
    proyectos: Proyecto[] = [];
    total = 0;
    tamanioPagina = 10;
    pagina = 1;
    cargando = false;

    filtroNombre = '';
    private filtro$ = new Subject<string>();

    dialogoVisible = false;
    proyectoSeleccionado: Proyecto | null = null;

    constructor(
        private proyectoService: ProyectoService,
        private confirmationService: ConfirmationService,
        private messageService: MessageService,
        private router: Router
    ) {}

    ngOnInit(): void {
        this.filtro$.pipe(debounceTime(400), distinctUntilChanged()).subscribe(() => {
            this.pagina = 1;
            this.cargar();
        });
        this.cargar();
    }

    onFiltroChange(valor: string): void {
        this.filtroNombre = valor;
        this.filtro$.next(valor);
    }

    /** Paginación server-side: p-table dispara este evento al cambiar de página (requisito 6.3). */
    onPageChange(event: { first?: number; rows?: number }): void {
        this.tamanioPagina = event.rows ?? this.tamanioPagina;
        this.pagina = Math.floor((event.first ?? 0) / this.tamanioPagina) + 1;
        this.cargar();
    }

    cargar(): void {
        this.cargando = true;
        this.proyectoService.listar(this.filtroNombre || null, this.pagina, this.tamanioPagina).subscribe({
            next: (resultado) => {
                this.proyectos = resultado.items;
                this.total = resultado.total;
                this.cargando = false;
            },
            error: () => {
                this.cargando = false;
                this.messageService.add({ severity: 'error', summary: 'No se pudieron cargar los proyectos' });
            }
        });
    }

    nuevoProyecto(): void {
        this.proyectoSeleccionado = null;
        this.dialogoVisible = true;
    }

    editarProyecto(proyecto: Proyecto): void {
        this.proyectoSeleccionado = proyecto;
        this.dialogoVisible = true;
    }

    abrirTablero(proyecto: Proyecto): void {
        this.router.navigate(['/proyectos', proyecto.id, 'tablero']);
    }

    eliminarProyecto(proyecto: Proyecto): void {
        this.confirmationService.confirm({
            header: 'Eliminar proyecto',
            message: `¿Seguro que querés eliminar "${proyecto.nombre}"? Esta acción no se puede deshacer.`,
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.proyectoService.eliminar(proyecto.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Proyecto eliminado' });
                        this.cargar();
                    },
                    error: () => {
                        this.messageService.add({ severity: 'error', summary: 'No se pudo eliminar el proyecto' });
                    }
                });
            }
        });
    }
}
