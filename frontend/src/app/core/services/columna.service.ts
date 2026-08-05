import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { ActualizarColumnaRequest, Columna, CrearColumnaRequest, ReordenarColumnaRequest } from '../models/columna.model';

@Injectable({ providedIn: 'root' })
export class ColumnaService {
    private urlBase(proyectoId: string) {
        return `${environment.apiUrl}/proyectos/${proyectoId}/columnas`;
    }

    constructor(private http: HttpClient) {}

    listar(proyectoId: string): Observable<Columna[]> {
        return this.http.get<Columna[]>(this.urlBase(proyectoId));
    }

    crear(proyectoId: string, request: CrearColumnaRequest): Observable<Columna> {
        return this.http.post<Columna>(this.urlBase(proyectoId), request);
    }

    actualizar(proyectoId: string, columnaId: string, request: ActualizarColumnaRequest): Observable<Columna> {
        return this.http.put<Columna>(`${this.urlBase(proyectoId)}/${columnaId}`, request);
    }

    reordenar(proyectoId: string, columnaId: string, request: ReordenarColumnaRequest): Observable<Columna> {
        return this.http.put<Columna>(`${this.urlBase(proyectoId)}/${columnaId}/orden`, request);
    }

    eliminar(proyectoId: string, columnaId: string): Observable<void> {
        return this.http.delete<void>(`${this.urlBase(proyectoId)}/${columnaId}`);
    }
}
