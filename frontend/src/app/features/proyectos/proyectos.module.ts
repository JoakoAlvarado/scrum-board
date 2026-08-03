import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { TooltipModule } from 'primeng/tooltip';
import { ProyectosRoutingModule } from './proyectos-routing.module';
import { ProyectosComponent } from './proyectos.component';

@NgModule({
    declarations: [ProyectosComponent],
    imports: [CommonModule, ProyectosRoutingModule, ButtonModule, RippleModule, TooltipModule]
})
export class ProyectosModule {}
