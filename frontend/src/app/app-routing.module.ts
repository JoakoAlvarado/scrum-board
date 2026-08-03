import { RouterModule } from '@angular/router';
import { NgModule } from '@angular/core';
import { NotfoundComponent } from './shared/not-found/notfound.component';
import { AppLayoutComponent } from './layout/app.layout.component';
import { authGuard } from './core/guards/auth.guard';

@NgModule({
    imports: [
        RouterModule.forRoot(
            [
                {
                    path: '',
                    component: AppLayoutComponent,
                    canActivate: [authGuard],
                    children: [
                        { path: '', redirectTo: 'proyectos', pathMatch: 'full' },
                        { path: 'proyectos', loadChildren: () => import('./features/proyectos/proyectos.module').then((m) => m.ProyectosModule) }
                    ]
                },
                { path: 'auth', loadChildren: () => import('./features/auth/auth.module').then((m) => m.AuthModule) },
                { path: 'notfound', component: NotfoundComponent },
                { path: '**', redirectTo: '/notfound' }
            ],
            { scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled', onSameUrlNavigation: 'reload' }
        )
    ],
    exports: [RouterModule]
})
export class AppRoutingModule {}
