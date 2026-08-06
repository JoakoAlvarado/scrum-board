import { AuthService } from './auth.service';
import { LoginResult } from '../models/auth.models';

const TOKEN_KEY = 'scrumboard_token';
const USER_KEY = 'scrumboard_user';

function guardarUsuario(usuario: LoginResult): void {
    localStorage.setItem(TOKEN_KEY, usuario.token);
    localStorage.setItem(USER_KEY, JSON.stringify(usuario));
}

/**
 * AuthService solo usa HttpClient en login(); el resto de la lógica que se prueba acá
 * (expiración de sesión, logout) no dispara ningún HTTP, así que alcanza con un mock
 * vacío en vez de HttpClientTestingModule completo.
 */
describe('AuthService — expiración de sesión y logout', () => {
    let service: AuthService;

    beforeEach(() => {
        localStorage.clear();
        service = new AuthService({} as any);
    });

    afterEach(() => localStorage.clear());

    it('isAuthenticated: false cuando no hay ningún usuario guardado', () => {
        expect(service.isAuthenticated()).toBeFalse();
    });

    it('isAuthenticated: true cuando el token todavía no expiró', () => {
        const expiraEnUnaHora = new Date(Date.now() + 60 * 60 * 1000).toISOString();
        guardarUsuario({ usuarioId: 'u1', nombre: 'Ana', email: 'ana@demo.local', token: 'jwt-falso', expiraUtc: expiraEnUnaHora });

        // AuthService lee localStorage recién en el constructor (BehaviorSubject inicial),
        // así que hay que crear la instancia después de guardar el usuario.
        service = new AuthService({} as any);

        expect(service.isAuthenticated()).toBeTrue();
    });

    it('isAuthenticated: false cuando el token ya expiró', () => {
        const expiroHaceUnaHora = new Date(Date.now() - 60 * 60 * 1000).toISOString();
        guardarUsuario({ usuarioId: 'u1', nombre: 'Ana', email: 'ana@demo.local', token: 'jwt-falso', expiraUtc: expiroHaceUnaHora });

        service = new AuthService({} as any);

        expect(service.isAuthenticated()).toBeFalse();
    });

    it('logout: limpia el token, el usuario y notifica currentUser$ con null', (done) => {
        const expiraEnUnaHora = new Date(Date.now() + 60 * 60 * 1000).toISOString();
        guardarUsuario({ usuarioId: 'u1', nombre: 'Ana', email: 'ana@demo.local', token: 'jwt-falso', expiraUtc: expiraEnUnaHora });
        service = new AuthService({} as any);

        service.logout();

        expect(localStorage.getItem(TOKEN_KEY)).toBeNull();
        expect(localStorage.getItem(USER_KEY)).toBeNull();
        service.currentUser$.subscribe((usuario) => {
            expect(usuario).toBeNull();
            done();
        });
    });
});