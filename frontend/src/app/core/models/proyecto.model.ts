export enum EstadoProyecto {
    Planificado = 'Planificado',
    EnCurso = 'EnCurso',
    Pausado = 'Pausado',
    Finalizado = 'Finalizado'
}

export const ESTADOS_PROYECTO: { label: string; value: EstadoProyecto }[] = [
    { label: 'Planificado', value: EstadoProyecto.Planificado },
    { label: 'En curso', value: EstadoProyecto.EnCurso },
    { label: 'Pausado', value: EstadoProyecto.Pausado },
    { label: 'Finalizado', value: EstadoProyecto.Finalizado }
];

/** Refleja ProyectoDto (backend). */
export interface Proyecto {
    id: string;
    nombre: string;
    descripcion: string;
    fechaInicio: string;
    fechaFinPrevista: string;
    estado: EstadoProyecto;
    cantidadColumnas: number;
    cantidadTareas: number;
}

/** Refleja CrearProyectoRequest (backend). */
export interface CrearProyectoRequest {
    nombre: string;
    descripcion: string;
    fechaInicio: string;
    fechaFinPrevista: string;
}

/** Refleja ActualizarProyectoRequest (backend). */
export interface ActualizarProyectoRequest {
    nombre: string;
    descripcion: string;
    fechaInicio: string;
    fechaFinPrevista: string;
    estado: EstadoProyecto;
}

/** Refleja PagedResultDto<T> (backend). */
export interface PagedResult<T> {
    items: T[];
    total: number;
    pagina: number;
    tamanioPagina: number;
}
