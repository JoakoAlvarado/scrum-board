import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { PRIORIDADES, Prioridad, Tarea } from 'src/app/core/models/tarea.model';
import { Usuario } from 'src/app/core/models/usuario.model';
import { TareaService } from 'src/app/core/services/tarea.service';

/**
 * Diálogo de alta/edición de tarea. Si `tarea` viene con valor, edita; si es null,
 * crea una nueva en `columnaId`. El propio componente guarda contra la Api.
 */
@Component({
    selector: 'app-tarea-form',
    templateUrl: './tarea-form.component.html'
})
export class TareaFormComponent implements OnChanges {
    @Input() visible = false;
    @Input() proyectoId!: string;
    @Input() columnaId: string | null = null; // columna destino al crear
    @Input() tarea: Tarea | null = null; // null = crear
    @Input() usuarios: Usuario[] = [];

    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() guardado = new EventEmitter<void>();

    prioridades = PRIORIDADES;
    guardando = false;

    form = this.fb.group({
        titulo: ['', [Validators.required, Validators.maxLength(200)]],
        descripcion: ['', [Validators.maxLength(4000)]],
        prioridad: [Prioridad.Media, [Validators.required]],
        responsableId: ['', [Validators.required]]
    });

    constructor(
        private fb: FormBuilder,
        private tareaService: TareaService,
        private messageService: MessageService
    ) {}

    get esEdicion(): boolean {
        return !!this.tarea;
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible']?.currentValue === true) {
            if (this.tarea) {
                this.form.reset({
                    titulo: this.tarea.titulo,
                    descripcion: this.tarea.descripcion,
                    prioridad: this.tarea.prioridad,
                    responsableId: this.tarea.responsableId
                });
            } else {
                this.form.reset({ titulo: '', descripcion: '', prioridad: Prioridad.Media, responsableId: '' });
            }
        }
    }

    guardar(): void {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        this.guardando = true;
        const valores = this.form.getRawValue();
        const payload = {
            titulo: valores.titulo!,
            descripcion: valores.descripcion ?? '',
            prioridad: valores.prioridad!,
            responsableId: valores.responsableId!
        };

        const peticion = this.esEdicion
            ? this.tareaService.actualizar(this.proyectoId, this.tarea!.id, payload)
            : this.tareaService.crear(this.proyectoId, { ...payload, columnaId: this.columnaId! });

        peticion.subscribe({
            next: () => {
                this.guardando = false;
                this.messageService.add({ severity: 'success', summary: this.esEdicion ? 'Tarea actualizada' : 'Tarea creada' });
                this.cerrar();
                this.guardado.emit();
            },
            error: () => {
                this.guardando = false;
                this.messageService.add({ severity: 'error', summary: 'No se pudo guardar la tarea' });
            }
        });
    }

    cerrar(): void {
        this.visible = false;
        this.visibleChange.emit(false);
    }
}
