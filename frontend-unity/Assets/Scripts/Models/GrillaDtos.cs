using System;

namespace Sme.Models
{
    [Serializable]
    public class PiezaDto
    {
        public int piso;
        public int fila;
        public int columna;
        public string tipo;
        public string caraAcceso;
        public bool esAccesible;
        public bool esCrucePeatonal;
        public string sentidoVertical;
    }

    [Serializable]
    public class GuardarGrillaRequest
    {
        public int cantidadPisos;
        public PiezaDto[] piezas;
    }

    [Serializable]
    public class GuardarGrillaResponse
    {
        public bool guardado;
    }
}
