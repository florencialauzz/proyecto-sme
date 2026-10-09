package sme.dto;

import java.math.BigDecimal;
import java.util.List;

// RF-26: respuesta de POST /simulacion/ejecutar (contratos/api-contract.md).
// Viajan todos los indicadores en una sola respuesta, más lo que necesita
// Unity para la reproducción animada (reproduccion), que sale de la misma
// corrida. GET /simulacion devuelve los mismos indicadores sin reproduccion
// (ResultadoSimulacionResponse).
public record EjecutarSimulacionResponse(
        BigDecimal eficienciaEspacial,
        String calificacionEficiencia,
        List<PuntoOcupacionResponse> curvaOcupacion,
        BigDecimal demandaSatisfecha,
        Integer vehiculosRechazados,
        List<PeriodoSaturacionResponse> periodosSaturacion,
        BigDecimal puntuacionGeneral,
        String calificacionTexto,
        ReproduccionResponse reproduccion) {
}
