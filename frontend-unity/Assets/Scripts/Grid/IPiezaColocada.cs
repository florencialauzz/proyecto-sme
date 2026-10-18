namespace Sme.Grid
{
    public interface IPiezaColocada
    {
        TipoPieza Tipo { get; }
        string Orientacion { get; }
        bool EsAccesible { get; }
        bool EsCrucePeatonal { get; }

        string SentidoVertical { get; }

        void Refrescar();
    }
}
