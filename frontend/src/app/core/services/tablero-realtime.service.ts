import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from 'src/environments/environment';
import { AuthService } from './auth.service';
import { Tarea } from '../models/tarea.model';
import { Columna } from '../models/columna.model';

/**
 * Cliente del canal de tiempo real del tablero (requisito 6.7). Un solo grupo de
 * SignalR por proyecto del lado del servidor; acá nos suscribimos/desuscribimos
 * explícitamente al entrar/salir de un tablero.
 */
@Injectable({ providedIn: 'root' })
export class TableroRealtimeService {
    private connection: signalR.HubConnection | null = null;
    private proyectoSuscritoId: string | null = null;

    tareaCreada$ = new Subject<Tarea>();
    tareaActualizada$ = new Subject<Tarea>();
    tareaMovida$ = new Subject<Tarea>();
    tareaEliminada$ = new Subject<string>();

    columnaCreada$ = new Subject<Columna>();
    columnaActualizada$ = new Subject<Columna>();
    columnaReordenada$ = new Subject<Columna>();
    columnaEliminada$ = new Subject<string>();

    constructor(private authService: AuthService) {}

    async suscribirseAProyecto(proyectoId: string): Promise<void> {
        await this.asegurarConexion();
        this.proyectoSuscritoId = proyectoId;
        await this.connection?.invoke('SuscribirseAProyecto', proyectoId);
    }

    async desuscribirseDeProyecto(proyectoId: string): Promise<void> {
        if (!this.connection) return;
        try {
            await this.connection.invoke('DesuscribirseDeProyecto', proyectoId);
        } finally {
            this.proyectoSuscritoId = null;
        }
    }

    /** Cierra la conexión (requisito 6.7: sin conexiones huérfanas al salir del tablero). */
    async desconectar(): Promise<void> {
        if (!this.connection) return;
        await this.connection.stop();
        this.connection = null;
    }

    private async asegurarConexion(): Promise<void> {
        if (this.connection) return;

        // El token viaja como query string ("access_token"), tal como espera
        // JwtBearerEvents.OnMessageReceived en el backend para el path /hubs — el
        // navegador no puede mandar headers custom en el handshake de WebSocket.
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(environment.signalRUrl, {
                accessTokenFactory: () => this.authService.getToken() ?? ''
            })
            .withAutomaticReconnect()
            .build();

        this.registrarHandlers();
        await this.connection.start();
    }

    private registrarHandlers(): void {
        if (!this.connection) return;

        this.connection.on('TareaCreada', (tarea: Tarea) => this.tareaCreada$.next(tarea));
        this.connection.on('TareaActualizada', (tarea: Tarea) => this.tareaActualizada$.next(tarea));
        // this.connection.on('TareaMovida', (tarea: Tarea) => this.tareaMovida$.next(tarea));
        this.connection.on('TareaMovida', (tarea: Tarea) => {
            this.tareaMovida$.next(tarea);
        });
        this.connection.on('TareaEliminada', (tareaId: string) => this.tareaEliminada$.next(tareaId));

        this.connection.on('ColumnaCreada', (columna: Columna) => this.columnaCreada$.next(columna));
        this.connection.on('ColumnaActualizada', (columna: Columna) => this.columnaActualizada$.next(columna));
        this.connection.on('ColumnaReordenada', (columna: Columna) => this.columnaReordenada$.next(columna));
        this.connection.on('ColumnaEliminada', (columnaId: string) => this.columnaEliminada$.next(columnaId));

        // SignalR no recuerda membresías de grupo entre reconexiones: hay que
        // volver a unirse al grupo del proyecto si se cae y se recupera la conexión.
        this.connection.onreconnected(() => {
            if (this.proyectoSuscritoId) {
                this.connection?.invoke('SuscribirseAProyecto', this.proyectoSuscritoId);
            }
        });
    }
}
