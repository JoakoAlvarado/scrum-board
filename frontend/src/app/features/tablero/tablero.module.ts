import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { ConfirmationService, MessageService } from 'primeng/api';

import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { DialogModule } from 'primeng/dialog';
import { DropdownModule } from 'primeng/dropdown';
import { TagModule } from 'primeng/tag';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { ProgressSpinnerModule } from 'primeng/progressspinner';

import { TableroRoutingModule } from './tablero-routing.module';
import { TableroComponent } from './tablero.component';
import { TareaFormComponent } from './tarea-form/tarea-form.component';

@NgModule({
    declarations: [TableroComponent, TareaFormComponent],
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        DragDropModule,
        TableroRoutingModule,
        ButtonModule,
        RippleModule,
        InputTextModule,
        InputTextareaModule,
        DialogModule,
        DropdownModule,
        TagModule,
        ConfirmDialogModule,
        ToastModule,
        ProgressSpinnerModule
    ],
    providers: [ConfirmationService, MessageService]
})
export class TableroModule {}
