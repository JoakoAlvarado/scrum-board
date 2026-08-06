import { of } from 'rxjs';
import { CdkDragDrop } from '@angular/cdk/drag-drop';
import { TableroComponent } from './tablero.component';
import { Prioridad, Tarea } from 'src/app/core/models/tarea.model';
import { Columna } from 'src/app/core/models/columna.model';


/**
 * Se instancia el componente directamente (sin TestBed) porque lo que se prueba acá es
 * lógica de aplicación pura del tablero (filtros, cálculo de vecinos al reordenar,
 * aplicación de eventos de tiempo real), no renderizado de template — evita el costo de
 * compilar todos los módulos de PrimeNG solo para testear métodos de clase.
 */
describe('TableroComponent — lógica de aplicación', () => {
    let component: TableroComponent;
    let tareaServiceMock: { mover: jasmine.Spy };
    let columnaServiceMock: { reordenar: jasmine.Spy };

    const observableVacio$ = of();

    function crearTarea(id: string, columnaId: string, orden: number, extra: Partial<Tarea> = {}): Tarea {
        return {
            id,
            titulo: `Tarea ${id}`,
            descripcion: '',
            prioridad: Prioridad.Media,
            responsableId: 'usuario-1',
            columnaId,
            proyectoId: 'proyecto-1',
            orden,
            fechaCreacion: new Date().toISOString(),
            ...extra
        };
    }

    function crearColumna(id: string, orden: number): Columna {
        return { id, nombre: `Columna ${id}`, orden, proyectoId: 'proyecto-1', cantidadTareas: 0 };
    }

    beforeEach(() => {
        tareaServiceMock = { mover: jasmine.createSpy('mover').and.returnValue(of({})) };
        columnaServiceMock = { reordenar: jasmine.createSpy('reordenar').and.returnValue(of({})) };

        const realtimeMock = {
            suscribirseAProyecto: jasmine.createSpy(),
            desuscribirseDeProyecto: jasmine.createSpy(),
            desconectar: jasmine.createSpy(),
            tareaCreada$: observableVacio$,
            tareaActualizada$: observableVacio$,
            tareaMovida$: observableVacio$,
            tareaEliminada$: observableVacio$,
            columnaCreada$: observableVacio$,
            columnaActualizada$: observableVacio$,
            columnaReordenada$: observableVacio$,
            columnaEliminada$: observableVacio$,
            usuariosConectados$: observableVacio$
        };

        component = new TableroComponent(
            { snapshot: { paramMap: { get: () => 'proyecto-1' } } } as any,
            {} as any,
            {} as any,
            columnaServiceMock as any,
            tareaServiceMock as any,
            {} as any,
            realtimeMock as any,
            {} as any,
            {} as any,
            { add: jasmine.createSpy() } as any
        );

        component.proyectoId = 'proyecto-1';
    });

    // --- Filtros (requisito deseable 7) ---

    it('coincideFiltro: filtra por texto en el título, sin importar mayúsculas', () => {
        const tarea = crearTarea('t1', 'c1', 1024, { titulo: 'Implementar login' });

        component.filtroTexto = 'LOGIN';
        expect(component.coincideFiltro(tarea)).toBeTrue();

        component.filtroTexto = 'reportes';
        expect(component.coincideFiltro(tarea)).toBeFalse();
    });

    it('coincideFiltro: combina prioridad y responsable, ambos deben matchear', () => {
        const tarea = crearTarea('t1', 'c1', 1024, { prioridad: Prioridad.Alta, responsableId: 'usuario-1' });

        component.filtroPrioridad = Prioridad.Alta;
        component.filtroResponsableId = 'usuario-2'; // no coincide
        expect(component.coincideFiltro(tarea)).toBeFalse();

        component.filtroResponsableId = 'usuario-1'; // ahora sí
        expect(component.coincideFiltro(tarea)).toBeTrue();
    });

    // --- Drag & drop de tareas: cálculo de la nueva posición (requisito 6.9 obligatorio) ---

    it('onDropTarea: al reordenar dentro de la misma columna, calcula los vecinos correctos y llama a mover()', () => {
        const columna: Columna = crearColumna('c1', 1024);
        const t1 = crearTarea('t1', 'c1', 1024);
        const t2 = crearTarea('t2', 'c1', 2048);
        const t3 = crearTarea('t3', 'c1', 3072);
        const lista = [t1, t2, t3];

        component.columnasVista = [{ columna, tareas: lista }];

        // Mover t1 (índice 0) a la posición 1 (entre t2 y t3): resultado esperado [t2, t1, t3].
        const evento = {
            previousContainer: { data: lista },
            container: { data: lista },
            previousIndex: 0,
            currentIndex: 1
        } as CdkDragDrop<Tarea[]>;

        component.onDropTarea(evento, component.columnasVista[0]);

        expect(tareaServiceMock.mover).toHaveBeenCalledWith('proyecto-1', 't1', {
            columnaDestinoId: 'c1',
            tareaAnteriorId: 't2',
            tareaSiguienteId: 't3'
        });
    });

    it('onDropTarea: al mover a otra columna, informa el columnaDestinoId correcto', () => {
        const columnaOrigen = crearColumna('c1', 1024);
        const columnaDestino = crearColumna('c2', 2048);
        const tareaMovida = crearTarea('t1', 'c1', 1024);
        const tareasOrigen = [tareaMovida];
        const tareasDestino: Tarea[] = [];

        component.columnasVista = [
            { columna: columnaOrigen, tareas: tareasOrigen },
            { columna: columnaDestino, tareas: tareasDestino }
        ];

        const evento = {
            previousContainer: { data: tareasOrigen },
            container: { data: tareasDestino },
            previousIndex: 0,
            currentIndex: 0
        } as CdkDragDrop<Tarea[]>;

        component.onDropTarea(evento, component.columnasVista[1]);

        expect(tareaServiceMock.mover).toHaveBeenCalledWith('proyecto-1', 't1', {
            columnaDestinoId: 'c2',
            tareaAnteriorId: null,
            tareaSiguienteId: null
        });
    });

    // --- Aplicación de eventos de tiempo real (requisito 6.7) ---

    it('aplicarTareaEliminada (privado, vía evento simulado): saca la tarea de la columna que corresponda', () => {
        const columna = crearColumna('c1', 1024);
        const t1 = crearTarea('t1', 'c1', 1024);
        const t2 = crearTarea('t2', 'c1', 2048);
        component.columnasVista = [{ columna, tareas: [t1, t2] }];

        (component as any).aplicarTareaEliminada('t1');

        expect(component.columnasVista[0].tareas.map((t) => t.id)).toEqual(['t2']);
    });

    it('aplicarColumnaReordenada (privado, vía evento simulado): reordena columnasVista según el nuevo orden', () => {
        const c1 = crearColumna('c1', 1024);
        const c2 = crearColumna('c2', 2048);
        component.columnasVista = [
            { columna: c1, tareas: [] },
            { columna: c2, tareas: [] }
        ];

        // Llega un evento de que c2 ahora tiene orden 512 (debería pasar a estar primera).
        (component as any).aplicarColumnaReordenada({ ...c2, orden: 512 });

        expect(component.columnasVista.map((cv) => cv.columna.id)).toEqual(['c2', 'c1']);
    });
});