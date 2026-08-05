import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TableroComponent } from './tablero.component';

@NgModule({
    imports: [RouterModule.forChild([{ path: '', component: TableroComponent }])],
    exports: [RouterModule]
})
export class TableroRoutingModule {}
