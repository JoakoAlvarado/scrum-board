import { calcularVecinos } from './reordenamiento.util';

interface Item {
    id: string;
}

/**
 * Cubre el requisito obligatorio del enunciado (6.9): "una de las pruebas debe cubrir
 * obligatoriamente el cálculo de la nueva posición de una tarea al reordenarla".
 *
 * El cálculo numérico del orden fraccionario lo hace el backend
 * (CalculadorDeOrdenTests, ya cubierto ahí); acá se cubre la otra mitad del mismo
 * problema, del lado del cliente: identificar correctamente los dos vecinos del
 * elemento movido dentro de la lista ya reordenada por el drag & drop — son esos dos
 * ids los que el frontend le manda a la Api para que calcule la posición real. Un
 * error acá (ej. mandar el vecino equivocado) rompe el reordenamiento aunque el
 * algoritmo del backend esté perfecto.
 */
describe('calcularVecinos — cálculo de la nueva posición al reordenar (6.9)', () => {
    const lista: Item[] = [{ id: 'a' }, { id: 'b' }, { id: 'c' }, { id: 'd' }];

    it('elemento en el medio de la lista: devuelve el anterior y el siguiente reales', () => {
        const resultado = calcularVecinos(lista, 'b');

        expect(resultado).toEqual({ anteriorId: 'a', siguienteId: 'c' });
    });

    it('elemento al principio de la lista: anteriorId es null', () => {
        const resultado = calcularVecinos(lista, 'a');

        expect(resultado).toEqual({ anteriorId: null, siguienteId: 'b' });
    });

    it('elemento al final de la lista: siguienteId es null', () => {
        const resultado = calcularVecinos(lista, 'd');

        expect(resultado).toEqual({ anteriorId: 'c', siguienteId: null });
    });

    it('única tarea de una columna vacía: ambos vecinos son null', () => {
        const resultado = calcularVecinos([{ id: 'unica' }], 'unica');

        expect(resultado).toEqual({ anteriorId: null, siguienteId: null });
    });

    it('recalcula correctamente tras un reordenamiento (drag & drop simulado)', () => {
        // Simula lo que hace moveItemInArray de Angular CDK: mover "d" a la posición 1.
        const listaReordenada: Item[] = [{ id: 'a' }, { id: 'd' }, { id: 'b' }, { id: 'c' }];

        const resultado = calcularVecinos(listaReordenada, 'd');

        expect(resultado).toEqual({ anteriorId: 'a', siguienteId: 'b' });
    });

    it('elemento que no está en la lista: lanza un error explícito en vez de devolver un resultado incorrecto', () => {
        expect(() => calcularVecinos(lista, 'no-existe')).toThrowError(/no está en la lista/);
    });
});