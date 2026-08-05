export enum Prioridad {
    Baja = 'Baja',
    Media = 'Media',
    Alta = 'Alta',
    Urgente = 'Urgente'
}

export const PRIORIDADES: { label: string; value: Prioridad; severity: string }[] = [
    { label: 'Baja', value: Prioridad.Baja, severity: 'info' },
    { label: 'Media', value: Prioridad.Media, severity: 'success' },
    { label: 'Alta', value: Prioridad.Alta, severity: 'warning' },
    { label: 'Urgente', value: Prioridad.Urgente, severity: 'danger' }
];

/** Refleja TareaDto (backend). */
export interface Tarea {
    id: string;
    titulo: string;
    descripcion: string;
    prioridad: Prioridad;
    responsableId: string;
    columnaId: string;
    proyectoId: string;
    orden: number;
    fechaCreacion: string;
}

export interface CrearTareaRequest {
    columnaId: string;
    titulo: string;
    descripcion: string;
    prioridad: Prioridad;
    responsableId: string;
}

export interface ActualizarTareaRequest {
    titulo: string;
    descripcion: string;
    prioridad: Prioridad;
    responsableId: string;
}

/** Refleja MoverTareaRequest (backend): posiciona la tarea entre dos vecinas de la columna destino. */
export interface MoverTareaRequest {
    columnaDestinoId: string;
    tareaAnteriorId: string | null;
    tareaSiguienteId: string | null;
}
