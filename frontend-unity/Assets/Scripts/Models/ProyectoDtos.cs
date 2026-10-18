using System;

namespace Sme.Models
{
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

    [Serializable]
    public class ProyectoResumenDto
    {
        public long proyectoId;
        public string nombre;
        public string estado;
        public string fechaModificacion;
    }

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

    [Serializable]
    public class RenombrarProyectoRequest
    {
        public string nombre;
    }

    [Serializable]
    public class RenombrarProyectoResponse
    {
        public long proyectoId;
        public string nombre;
    }

    [Serializable]
    public class EliminarProyectoRequest
    {
    }

    [Serializable]
    public class EliminarProyectoResponse
    {
        public bool eliminado;
    }

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
