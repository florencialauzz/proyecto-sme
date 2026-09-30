package sme.dto;

import java.math.BigDecimal;
import java.util.List;

// RF-26: respuesta de POST /simulacion/ejecutar (contratos/api-contract.md).
// Viajan todos los indicadores aunque en Iteración 3 Unity muestre solo
// eficienciaEspacial, calificacionEficiencia y curvaOcupacion.
//
// puntuacionGeneral y calificacionTexto (RF-30) van en null hasta que el
// equipo defina los pesos del promedio ponderado y los cortes.
public record EjecutarSimulacionResponse(
        BigDecimal eficienciaEspacial,
        String calificacionEficiencia,
        List<PuntoOcupacionResponse> curvaOcupacion,
        BigDecimal demandaSatisfecha,
        Integer vehiculosRechazados,
        List<PeriodoSaturacionResponse> periodosSaturacion,
        BigDecimal puntuacionGeneral,
        String calificacionTexto) {
}
