namespace Sme.Grid
{
    public class CeldaGrilla
    {
        public int Piso { get; }
        public int Fila { get; }
        public int Columna { get; }

        public TipoPieza? Tipo { get; private set; }

        public CaraAcceso? Direccion { get; private set; }

        public SentidoVertical? SentidoVertical { get; private set; }
        public bool EsEntradaDeRampa { get; private set; }

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
