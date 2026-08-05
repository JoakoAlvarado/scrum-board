import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ESTADOS_PROYECTO, EstadoProyecto, Proyecto } from 'src/app/core/models/proyecto.model';
import { ProyectoService } from 'src/app/core/services/proyecto.service';

/**
 * Diálogo de alta/edición de proyecto. Si `proyecto` viene con valor, edita; si es
 * null, crea uno nuevo. El propio componente hace el guardado contra la Api y avisa
 * al padre con (guardado) para que refresque el listado.
 */
@Component({
    selector: 'app-proyecto-form',
    templateUrl: './proyecto-form.component.html'
})
export class ProyectoFormComponent implements OnChanges {
    @Input() visible = false;
    @Input() proyecto: Proyecto | null = null;

    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() guardado = new EventEmitter<void>();

    estados = ESTADOS_PROYECTO;
    guardando = false;

    form = this.fb.group({
        nombre: ['', [Validators.required, Validators.maxLength(200)]],
        descripcion: ['', [Validators.maxLength(2000)]],
        fechaInicio: [new Date(), [Validators.required]],
        fechaFinPrevista: [new Date(), [Validators.required]],
        estado: [EstadoProyecto.Planificado]
    });

    constructor(
        private fb: FormBuilder,
        private proyectoService: ProyectoService,
        private messageService: MessageService
    ) {}

    get esEdicion(): boolean {
        return !!this.proyecto;
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible']?.currentValue === true) {
            this.inicializarFormulario();
        }
    }

    private inicializarFormulario(): void {
        if (this.proyecto) {
            this.form.reset({
                nombre: this.proyecto.nombre,
                descripcion: this.proyecto.descripcion,
                fechaInicio: new Date(this.proyecto.fechaInicio),
                fechaFinPrevista: new Date(this.proyecto.fechaFinPrevista),
                estado: this.proyecto.estado
            });
        } else {
            this.form.reset({
                nombre: '',
                descripcion: '',
                fechaInicio: new Date(),
                fechaFinPrevista: new Date(),
                estado: EstadoProyecto.Planificado
            });
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
            nombre: valores.nombre!,
            descripcion: valores.descripcion ?? '',
            fechaInicio: (valores.fechaInicio as Date).toISOString(),
            fechaFinPrevista: (valores.fechaFinPrevista as Date).toISOString()
        };

        const peticion = this.esEdicion
            ? this.proyectoService.actualizar(this.proyecto!.id, { ...payload, estado: valores.estado! })
            : this.proyectoService.crear(payload);

        peticion.subscribe({
            next: () => {
                this.guardando = false;
                this.messageService.add({
                    severity: 'success',
                    summary: this.esEdicion ? 'Proyecto actualizado' : 'Proyecto creado'
                });
                this.cerrar();
                this.guardado.emit();
            },
            error: () => {
                this.guardando = false;
                this.messageService.add({ severity: 'error', summary: 'No se pudo guardar el proyecto' });
            }
        });
    }

    cerrar(): void {
        this.visible = false;
        this.visibleChange.emit(false);
    }
}
