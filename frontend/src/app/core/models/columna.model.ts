/** Refleja ColumnaDto (backend). */
export interface Columna {
    id: string;
    nombre: string;
    orden: number;
    proyectoId: string;
    cantidadTareas: number;
}

export interface CrearColumnaRequest {
    nombre: string;
}

export interface ActualizarColumnaRequest {
    nombre: string;
}

/** Refleja ReordenarColumnaRequest (backend): posiciona la columna entre dos vecinas por id. */
export interface ReordenarColumnaRequest {
    columnaAnteriorId: string | null;
    columnaSiguienteId: string | null;
}
