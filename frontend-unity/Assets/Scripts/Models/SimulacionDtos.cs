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

    // Los indicadores, con el mismo shape en las tres rutas de simulación:
    // lo que devuelve /ejecutar (sin reproduccion), lo que se reenvía a
    // /guardar (RF-23) y lo que devuelve GET /simulacion con el último
    // resultado guardado (RF-24, RF-25).
    //
    // reproduccion no está acá a propósito: JsonUtility no omite campos
    // (serializa un array null como [] y un objeto null con sus valores por
    // defecto), así que si esta clase lo tuviera viajaría a /guardar.
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

    // La respuesta completa de /ejecutar: los mismos indicadores que
    // EjecutarSimulacionResponse más reproduccion, para la reproducción
    // animada (animacion/reglas.md). Se usa solo para recibirla; lo que se
    // guarda en ProyectoManager y se reenvía a /guardar es la parte de los
    // indicadores (EditorScreen.SoloIndicadores).
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

    // Animación: qué pasó con cada vehículo y por dónde circula, calculado
    // en el backend. Unity solo lo reproduce, no decide nada. Detalle de
    // cada campo en contratos/api-contract.md.
    [Serializable]
    public class ReproduccionDto
    {
        // Las plazas usables, en el orden en que el motor las asigna: la
        // posición es el indicePlaza de cada evento.
        public PlazaReproduccionDto[] plazas;

        // Los rechazados recorren los dos caminos de esa plaza (la más
        // lejana). -1 si no hay plazas usables.
        public int indicePlazaRecorridoRechazados;

        // Uno por vehículo llegado, en orden de llegada.
        public EventoVehiculoDto[] eventos;
    }

    // piso, fila y columna son los del ancla de la Plaza. caminoEntrada va
    // de la Entrada a la boca; caminoSalida, de la boca (repetida) a la
    // Salida más cercana.
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

    // indicePlaza: posición en ReproduccionDto.plazas, o -1 si fue
    // rechazado (en ese caso minutoSalida = minutoLlegada). El vehículo
    // ocupa la plaza en los minutos m con minutoLlegada <= m < minutoSalida.
    [Serializable]
    public class EventoVehiculoDto
    {
        public int minutoLlegada;
        public int minutoSalida;
        public int indicePlaza;
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
