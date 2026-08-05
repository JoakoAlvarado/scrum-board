import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { ActualizarTareaRequest, CrearTareaRequest, MoverTareaRequest, Tarea } from '../models/tarea.model';

@Injectable({ providedIn: 'root' })
export class TareaService {
    private urlBase(proyectoId: string) {
        return `${environment.apiUrl}/proyectos/${proyectoId}/tareas`;
    }

    constructor(private http: HttpClient) {}

    listar(proyectoId: string): Observable<Tarea[]> {
        return this.http.get<Tarea[]>(this.urlBase(proyectoId));
    }

    crear(proyectoId: string, request: CrearTareaRequest): Observable<Tarea> {
        return this.http.post<Tarea>(this.urlBase(proyectoId), request);
    }

    actualizar(proyectoId: string, tareaId: string, request: ActualizarTareaRequest): Observable<Tarea> {
        return this.http.put<Tarea>(`${this.urlBase(proyectoId)}/${tareaId}`, request);
    }

    mover(proyectoId: string, tareaId: string, request: MoverTareaRequest): Observable<Tarea> {
        return this.http.put<Tarea>(`${this.urlBase(proyectoId)}/${tareaId}/mover`, request);
    }

    eliminar(proyectoId: string, tareaId: string): Observable<void> {
        return this.http.delete<void>(`${this.urlBase(proyectoId)}/${tareaId}`);
    }
}
