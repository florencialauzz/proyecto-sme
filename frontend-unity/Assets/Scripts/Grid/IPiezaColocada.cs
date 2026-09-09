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
    }
}
