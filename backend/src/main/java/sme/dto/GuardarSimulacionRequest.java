package sme.dto;

import java.math.BigDecimal;
import java.util.List;

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
