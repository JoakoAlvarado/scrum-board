import { Component } from '@angular/core';
import { AuthService } from 'src/app/core/services/auth.service';

/**
 * Placeholder funcional de la sección de Proyectos. El CRUD real (listado
 * paginado + filtro, alta/edición/eliminación) se conecta a la Api cuando el
 * endpoint de Proyectos esté implementado — ver plan de ejecución, Día 2/3.
 */
@Component({
    selector: 'app-proyectos',
    templateUrl: './proyectos.component.html'
})
export class ProyectosComponent {
    constructor(public authService: AuthService) {}
}
