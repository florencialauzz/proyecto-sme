namespace Sme.Grid
{
    // Modelo puro de una celda: qué pieza hay ahí y hacia dónde apunta, sin
    // depender de recorrer la jerarquía de GameObjects. Java va a espejar
    // esta misma forma para reconstruir el grafo en la simulación pesada
    // (arquitectura/decisiones.md) — son implementaciones separadas a
    // propósito, pero la forma del dato es la misma.
    public class CeldaGrilla
    {
        public int Fila { get; }
        public int Columna { get; }

        // null = vacía. La celda de fondo de una Plaza también guarda PLAZA
        // acá (con Direccion null) para poder saber qué celdas cubre una
        // plaza sin necesitar una pieza hija en esa celda (RF-13).
        public TipoPieza? Tipo { get; private set; }

        // caraAcceso o direccion según el tipo (mismo campo en el contrato,
        // ver editor/catalogo-piezas.md). Null en la celda de fondo de una
        // Plaza, que no tiene orientación propia.
        public CaraAcceso? Direccion { get; private set; }

        public bool Ocupada => Tipo != null;

        public CeldaGrilla(int fila, int columna)
        {
            Fila = fila;
            Columna = columna;
        }

        public void Ocupar(TipoPieza tipo, CaraAcceso? direccion)
        {
            Tipo = tipo;
            Direccion = direccion;
        }

        public void Liberar()
        {
            Tipo = null;
            Direccion = null;
        }
    }
}
