import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from 'src/environments/environment';
import { LoginRequest, LoginResult } from '../models/auth.models';

const TOKEN_KEY = 'scrumboard_token';
const USER_KEY = 'scrumboard_user';

/**
 * Autenticación contra la Api. El token JWT y los datos básicos del usuario se
 * guardan en localStorage para sobrevivir a un refresh del navegador; el guard
 * de ruta y el interceptor HTTP leen el estado a través de este mismo servicio.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
    private currentUserSubject = new BehaviorSubject<LoginResult | null>(this.leerUsuarioGuardado());
    currentUser$ = this.currentUserSubject.asObservable();

    constructor(private http: HttpClient) {}

    login(credenciales: LoginRequest): Observable<LoginResult> {
        return this.http
            .post<LoginResult>(`${environment.apiUrl}/auth/login`, credenciales)
            .pipe(
                tap((resultado) => {
                    localStorage.setItem(TOKEN_KEY, resultado.token);
                    localStorage.setItem(USER_KEY, JSON.stringify(resultado));
                    this.currentUserSubject.next(resultado);
                })
            );
    }

    logout(): void {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
        this.currentUserSubject.next(null);
    }

    getToken(): string | null {
        return localStorage.getItem(TOKEN_KEY);
    }

    get currentUser(): LoginResult | null {
        return this.currentUserSubject.value;
    }

    isAuthenticated(): boolean {
        const usuario = this.currentUser;
        if (!usuario) return false;

        // El JWT también expira solo del lado del servidor; esta comprobación de
        // expiraUtc evita mandar requests con un token que ya sabemos vencido.
        return new Date(usuario.expiraUtc).getTime() > Date.now();
    }

    private leerUsuarioGuardado(): LoginResult | null {
        const raw = localStorage.getItem(USER_KEY);
        if (!raw) return null;

        try {
            return JSON.parse(raw) as LoginResult;
        } catch {
            return null;
        }
    }
}
