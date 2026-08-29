using System;

namespace Sme.Models
{
    // Espejo de contratos/api-contract.md — nombres de campo iguales al JSON,
    // JsonUtility serializa por nombre de campo público.

    [Serializable]
    public class CrearProyectoRequest
    {
        public string nombre;
    }

    [Serializable]
    public class CrearProyectoResponse
    {
        public long proyectoId;
        public int filasGrilla;
        public int columnasGrilla;
        public string estado;
    }

    // RF-22: un item de GET /proyectos.
    [Serializable]
    public class ProyectoResumenDto
    {
        public long proyectoId;
        public string nombre;
        public string estado;
        public string fechaModificacion;
    }
}
