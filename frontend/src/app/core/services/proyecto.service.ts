import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { ActualizarProyectoRequest, CrearProyectoRequest, PagedResult, Proyecto } from '../models/proyecto.model';

@Injectable({ providedIn: 'root' })
export class ProyectoService {
    private readonly baseUrl = `${environment.apiUrl}/proyectos`;

    constructor(private http: HttpClient) {}

    listar(filtroNombre: string | null, pagina: number, tamanioPagina: number): Observable<PagedResult<Proyecto>> {
        let params = new HttpParams().set('pagina', pagina).set('tamanioPagina', tamanioPagina);
        if (filtroNombre) params = params.set('nombre', filtroNombre);

        return this.http.get<PagedResult<Proyecto>>(this.baseUrl, { params });
    }

    obtenerPorId(id: string): Observable<Proyecto> {
        return this.http.get<Proyecto>(`${this.baseUrl}/${id}`);
    }

    crear(request: CrearProyectoRequest): Observable<Proyecto> {
        return this.http.post<Proyecto>(this.baseUrl, request);
    }

    actualizar(id: string, request: ActualizarProyectoRequest): Observable<Proyecto> {
        return this.http.put<Proyecto>(`${this.baseUrl}/${id}`, request);
    }

    eliminar(id: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }
}
