namespace Sme.Grid
{
    // Modelo puro de una celda: qué pieza hay ahí y hacia dónde apunta, sin
    // depender de recorrer la jerarquía de GameObjects. Java va a espejar
    // esta misma forma para reconstruir el grafo en la simulación pesada
    // (arquitectura/decisiones.md) — son implementaciones separadas a
    // propósito, pero la forma del dato es la misma.
    public class CeldaGrilla
    {
        public int Piso { get; }
        public int Fila { get; }
        public int Columna { get; }

        // null = vacía. La celda de fondo de una Plaza también guarda PLAZA
        // acá (con Direccion null) para poder saber qué celdas cubre una
        // plaza sin necesitar una pieza hija en esa celda (RF-13).
        public TipoPieza? Tipo { get; private set; }

        // caraAcceso o direccion según el tipo (mismo campo en el contrato,
        // ver editor/catalogo-piezas.md). Null en la celda de fondo de una
        // Plaza y en la Escalera, que no tienen orientación propia.
        public CaraAcceso? Direccion { get; private set; }

        // Solo Rampa (RF-18). Las dos celdas de una rampa guardan el mismo
        // sentido; EsEntradaDeRampa dice cuál de las dos es la del piso por
        // donde entra el vehículo — el grafo (RF-20) la necesita, porque
        // esa celda no tiene salida horizontal y la otra no acepta entrada
        // horizontal (editor/grafo-circulacion.md).
        public SentidoVertical? SentidoVertical { get; private set; }
        public bool EsEntradaDeRampa { get; private set; }

        // Solo en la celda ancla de una Plaza (RF-14). RF-20 lo necesita para
        // contar las plazas accesibles contra el mínimo normativo.
        public bool EsAccesible { get; private set; }

        public bool Ocupada => Tipo != null;

        public CeldaGrilla(int piso, int fila, int columna)
        {
            Piso = piso;
            Fila = fila;
            Columna = columna;
        }

        public void Ocupar(TipoPieza tipo, CaraAcceso? direccion)
        {
            Tipo = tipo;
            Direccion = direccion;
            SentidoVertical = null;
            EsEntradaDeRampa = false;
            EsAccesible = false;
        }

        public void MarcarAccesible(bool esAccesible)
        {
            EsAccesible = esAccesible;
        }

        public void OcuparComoRampa(CaraAcceso direccion, SentidoVertical sentidoVertical, bool esEntrada)
        {
            Tipo = TipoPieza.RAMPA;
            Direccion = direccion;
            SentidoVertical = sentidoVertical;
            EsEntradaDeRampa = esEntrada;
            EsAccesible = false;
        }

        public void Liberar()
        {
            Tipo = null;
            Direccion = null;
            SentidoVertical = null;
            EsEntradaDeRampa = false;
            EsAccesible = false;
        }
    }
}
