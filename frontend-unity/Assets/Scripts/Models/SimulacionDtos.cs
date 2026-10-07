using System;

namespace Sme.Models
{
    // Espejo de contratos/api-contract.md — POST /api/proyectos/{id}/simulacion/ejecutar
    // (RF-26). Nombres de campo iguales al JSON; arrays en vez de List porque
    // así los deserializa JsonUtility.

    // El endpoint no lee body: se manda "{}" solo porque ApiClient.Post
    // siempre serializa un cuerpo.
    [Serializable]
    public class EjecutarSimulacionRequest
    {
    }

    // Mismo shape para las tres rutas de simulación: lo que devuelve
    // /ejecutar, lo que se reenvía a /guardar (RF-23) y lo que devuelve
    // GET /simulacion con el último resultado guardado (RF-24, RF-25).
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

    // RF-23: POST /api/proyectos/{id}/simulacion/guardar.
    [Serializable]
    public class GuardarSimulacionResponse
    {
        public bool guardado;
    }

    // RF-27: plazas ocupadas en ese minuto, contando desde la hora de inicio.
    [Serializable]
    public class PuntoOcupacionDto
    {
        public int minuto;
        public int cantidadOcupadas;
    }

    // RF-28: minutos desde la hora de inicio, los dos incluidos.
    [Serializable]
    public class PeriodoSaturacionDto
    {
        public int inicio;
        public int fin;
    }
}
