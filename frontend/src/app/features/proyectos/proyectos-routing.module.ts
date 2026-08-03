import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ProyectosComponent } from './proyectos.component';

@NgModule({
    imports: [RouterModule.forChild([{ path: '', component: ProyectosComponent }])],
    exports: [RouterModule]
})
export class ProyectosRoutingModule {}
