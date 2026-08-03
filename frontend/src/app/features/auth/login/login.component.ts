import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { LayoutService } from 'src/app/layout/service/app.layout.service';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
    selector: 'app-login',
    templateUrl: './login.component.html',
    styles: [`
        :host ::ng-deep .pi-eye,
        :host ::ng-deep .pi-eye-slash {
            transform:scale(1.6);
            margin-right: 1rem;
            color: var(--primary-color) !important;
        }
    `]
})
export class LoginComponent {
    form = this.fb.group({
        email: ['', [Validators.required, Validators.email]],
        password: ['', [Validators.required]]
    });

    cargando = false;
    errorMensaje: string | null = null;

    constructor(
        public layoutService: LayoutService,
        private fb: FormBuilder,
        private authService: AuthService,
        private router: Router
    ) {}

    ingresar(): void {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        this.cargando = true;
        this.errorMensaje = null;

        const { email, password } = this.form.getRawValue();

        this.authService.login({ email: email!, password: password! }).subscribe({
            next: () => this.router.navigate(['/']),
            error: (err) => {
                this.cargando = false;
                this.errorMensaje =
                    err?.status === 401
                        ? 'Email o contraseña inválidos.'
                        : 'No se pudo conectar con el servidor. Intentá de nuevo.';
            }
        });
    }
}
