/**
 * Lógica de aplicación del drag & drop del tablero (requisito 6.6/6.9).
 *
 * El cálculo numérico del nuevo orden fraccionario lo hace el backend
 * (ScrumBoard.Domain.Services.CalculadorDeOrden, sobre PUT .../mover y .../orden) — acá
 * solo se identifica, dentro del array ya reordenado localmente por el drag & drop
 * (actualización optimista), cuáles son los dos elementos vecinos del que se acaba de
 * mover. Esos dos ids son los que el frontend le manda a la Api para que calcule la
 * posición real.
 *
 * Se extrajo a una función pura (antes vivía duplicada, inline, en
 * TableroComponent.onDropTarea y onDropColumna) para poder testearla de forma aislada,
 * sin necesidad de simular un evento de Angular CDK ni instanciar el componente.
 */

export interface ElementoOrdenable {
    id: string;
}

export interface Vecinos {
    anteriorId: string | null;
    siguienteId: string | null;
}

/**
 * Devuelve los ids del elemento anterior y siguiente a `idElemento` dentro de `lista`.
 * `null` en un extremo significa "no hay vecino de ese lado" (el elemento quedó al
 * principio o al final de la lista).
 *
 * @throws Error si `idElemento` no está presente en `lista`.
 */
export function calcularVecinos<T extends ElementoOrdenable>(lista: T[], idElemento: string): Vecinos {
    const indice = lista.findIndex((el) => el.id === idElemento);

    if (indice === -1) {
        throw new Error(`calcularVecinos: el elemento con id "${idElemento}" no está en la lista.`);
    }

    return {
        anteriorId: lista[indice - 1]?.id ?? null,
        siguienteId: lista[indice + 1]?.id ?? null
    };
}