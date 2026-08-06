import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface ArchivoDescargado {
    blob: Blob;
    nombreArchivo: string;
}

/**
 * Descarga de reportes (requisito 6.8). El nombre de archivo real lo decide el backend
 * (ReporteService, con el nombre del proyecto sluggificado) y viaja en el header
 * Content-Disposition — ver CORS "WithExposedHeaders" en Program.cs, sin eso el
 * navegador no deja leer ese header en una request cross-origin.
 */
@Injectable({ providedIn: 'root' })
export class ReporteService {
    constructor(private http: HttpClient) {}

    descargarPdf(proyectoId: string): Observable<ArchivoDescargado> {
        return this.descargar(`${environment.apiUrl}/proyectos/${proyectoId}/reportes/pdf`);
    }

    descargarExcel(proyectoId: string): Observable<ArchivoDescargado> {
        return this.descargar(`${environment.apiUrl}/proyectos/${proyectoId}/reportes/excel`);
    }

    private descargar(url: string): Observable<ArchivoDescargado> {
        return this.http.get(url, { observe: 'response', responseType: 'blob' }).pipe(
            map((response) => ({
                blob: response.body as Blob,
                nombreArchivo: this.extraerNombreArchivo(response.headers.get('content-disposition')) ?? 'reporte'
            }))
        );
    }

    private extraerNombreArchivo(contentDisposition: string | null): string | null {
        if (!contentDisposition) return null;
        const match = /filename="?([^"]+)"?/.exec(contentDisposition);
        return match ? match[1] : null;
    }
}
