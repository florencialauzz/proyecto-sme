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

    // puntuacionGeneral y calificacionTexto (RF-30) no están acá a propósito:
    // el backend los manda en null hasta que se definan los pesos, y
    // JsonUtility no tiene cómo representar un número null (lo leería como
    // 0). Los campos del JSON que no existen en la clase se ignoran. Se
    // agregan en Iteración 4, junto con la pantalla que los muestra.
    [Serializable]
    public class EjecutarSimulacionResponse
    {
        public float eficienciaEspacial;
        public string calificacionEficiencia;
        public PuntoOcupacionDto[] curvaOcupacion;
        public float demandaSatisfecha;
        public int vehiculosRechazados;
        public PeriodoSaturacionDto[] periodosSaturacion;
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
