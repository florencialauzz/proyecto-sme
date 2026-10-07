package sme.dto;

import java.math.BigDecimal;
import java.util.List;

// RF-23: POST /simulacion/guardar. El cliente reenvía el mismo resultado que
// recibió de /ejecutar (mismo shape que EjecutarSimulacionResponse), así el
// backend no tiene que recordar nada entre las dos llamadas.
public record GuardarSimulacionRequest(
        BigDecimal eficienciaEspacial,
        String calificacionEficiencia,
        List<PuntoOcupacionResponse> curvaOcupacion,
        BigDecimal demandaSatisfecha,
        Integer vehiculosRechazados,
        List<PeriodoSaturacionResponse> periodosSaturacion,
        BigDecimal puntuacionGeneral,
        String calificacionTexto) {
}
