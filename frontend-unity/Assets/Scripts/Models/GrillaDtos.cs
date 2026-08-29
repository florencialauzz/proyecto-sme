using System;

namespace Sme.Models
{
    // Espejo de contratos/api-contract.md — PUT /api/proyectos/{id}/grilla (RF-13, RF-21).
    // En esta iteración solo existe la pieza Plaza, así que "tipo" siempre es
    // "PLAZA" y "esAccesible" siempre false (RF-14 es Iteración 2).

    [Serializable]
    public class PiezaDto
    {
        public int piso;
        public int fila;
        public int columna;
        public string tipo;
        public string caraAcceso;
        public bool esAccesible;
    }

    [Serializable]
    public class GuardarGrillaRequest
    {
        public PiezaDto[] piezas;
    }

    [Serializable]
    public class GuardarGrillaResponse
    {
        public bool guardado;
    }
}
