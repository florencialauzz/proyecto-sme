package sme.dto;

import java.math.BigDecimal;
import java.util.List;

// RF-24 y RF-25: respuesta de GET /simulacion con el último resultado
// guardado (contratos/api-contract.md). Son los mismos 8 campos que
// /ejecutar, sin reproduccion: lo de la animación no se persiste.
public record ResultadoSimulacionResponse(
        BigDecimal eficienciaEspacial,
        String calificacionEficiencia,
        List<PuntoOcupacionResponse> curvaOcupacion,
        BigDecimal demandaSatisfecha,
        Integer vehiculosRechazados,
        List<PeriodoSaturacionResponse> periodosSaturacion,
        BigDecimal puntuacionGeneral,
        String calificacionTexto) {
}
