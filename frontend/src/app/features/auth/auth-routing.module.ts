import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';

@NgModule({
    imports: [
        RouterModule.forChild([
            { path: 'login', loadChildren: () => import('./login/login.module').then((m) => m.LoginModule) },
            { path: '', redirectTo: 'login', pathMatch: 'full' }
        ])
    ],
    exports: [RouterModule]
})
export class AuthRoutingModule {}
