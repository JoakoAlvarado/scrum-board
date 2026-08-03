export interface LoginRequest {
    email: string;
    password: string;
}

/** Refleja el LoginResultDto que devuelve la Api (.NET) en POST /api/auth/login. */
export interface LoginResult {
    usuarioId: string;
    nombre: string;
    email: string;
    token: string;
    expiraUtc: string;
}
