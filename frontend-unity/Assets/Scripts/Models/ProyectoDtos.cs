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

    // Duplicar un proyecto: POST /proyectos/{id}/duplicar. El endpoint no lee
    // body: se manda "{}" solo porque ApiClient.Post siempre serializa uno.
    [Serializable]
    public class DuplicarProyectoRequest
    {
    }

    [Serializable]
    public class DuplicarProyectoResponse
    {
        public long proyectoId;
        public string nombre;
    }

    // Borrar un proyecto: DELETE /proyectos/{id}. Mismo caso que el anterior,
    // sin body.
    [Serializable]
    public class EliminarProyectoRequest
    {
    }

    [Serializable]
    public class EliminarProyectoResponse
    {
        public bool eliminado;
    }

    // Abrir un proyecto guardado: GET /proyectos/{id}.
    [Serializable]
    public class ProyectoDetalleResponse
    {
        public long proyectoId;
        public string nombre;
        public int cantidadPisos;
        public int frecuenciaIngreso;
        public int tiempoPermanencia;
        public string horaInicioSimulacion;
        public string horaFinSimulacion;
        public bool conFluctuaciones;
        public int filasGrilla;
        public int columnasGrilla;
        public PiezaDto[] piezas;
        public string estado;
    }

    // RF-08 a RF-11: PUT /proyectos/{id}/configuracion.
    [Serializable]
    public class GuardarConfiguracionRequest
    {
        public int cantidadPisos;
        public int frecuenciaIngreso;
        public int tiempoPermanencia;
        public string horaInicioSimulacion;
        public string horaFinSimulacion;
        public bool conFluctuaciones;
    }

    [Serializable]
    public class GuardarConfiguracionResponse
    {
        public bool guardado;
    }
}
