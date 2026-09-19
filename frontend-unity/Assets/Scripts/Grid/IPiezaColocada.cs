namespace Sme.Grid
{
    // RF-21: lo implementan todas las piezas que EditorScreen tiene que poder
    // recolectar de la grilla para armar el JSON de PUT /grilla, sin que
    // EditorScreen necesite conocer la clase concreta de cada una.
    public interface IPiezaColocada
    {
        TipoPieza Tipo { get; }
        string Orientacion { get; }
        bool EsAccesible { get; }
        bool EsCrucePeatonal { get; }

        // CeldaView lo llama en la celda propia y en hasta 4 vecinas cada vez
        // que algo se coloca, mueve o quita (editor/catalogo-piezas.md,
        // autotiling; editor/grafo-circulacion.md, rol de cruce). La mayoría
        // de las piezas no tienen nada que recalcular todavía.
        void Refrescar();
    }
}
