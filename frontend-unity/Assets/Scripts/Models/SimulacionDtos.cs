using System;

namespace Sme.Models
{
    [Serializable]
    public class EjecutarSimulacionRequest
    {
    }

    [Serializable]
    public class EjecutarSimulacionResponse
    {
        public float eficienciaEspacial;
        public string calificacionEficiencia;
        public PuntoOcupacionDto[] curvaOcupacion;
        public float demandaSatisfecha;
        public int vehiculosRechazados;
        public PeriodoSaturacionDto[] periodosSaturacion;
        public float puntuacionGeneral;
        public string calificacionTexto;
    }

    [Serializable]
    public class EjecutarSimulacionConReproduccionResponse
    {
        public float eficienciaEspacial;
        public string calificacionEficiencia;
        public PuntoOcupacionDto[] curvaOcupacion;
        public float demandaSatisfecha;
        public int vehiculosRechazados;
        public PeriodoSaturacionDto[] periodosSaturacion;
        public float puntuacionGeneral;
        public string calificacionTexto;
        public ReproduccionDto reproduccion;
    }

    [Serializable]
    public class ReproduccionDto
    {
        public PlazaReproduccionDto[] plazas;

        public int indicePlazaRecorridoRechazados;

        public EventoVehiculoDto[] eventos;
    }

    [Serializable]
    public class PlazaReproduccionDto
    {
        public int piso;
        public int fila;
        public int columna;
        public CeldaCaminoDto[] caminoEntrada;
        public CeldaCaminoDto[] caminoSalida;
    }

    [Serializable]
    public class CeldaCaminoDto
    {
        public int piso;
        public int fila;
        public int columna;
    }

    [Serializable]
    public class EventoVehiculoDto
    {
        public int minutoLlegada;
        public int minutoSalida;
        public int indicePlaza;
    }

    [Serializable]
    public class GuardarSimulacionResponse
    {
        public bool guardado;
    }

    [Serializable]
    public class PuntoOcupacionDto
    {
        public int minuto;
        public int cantidadOcupadas;
    }

    [Serializable]
    public class PeriodoSaturacionDto
    {
        public int inicio;
        public int fin;
    }
}
