package sme.dto;

import java.util.List;

// Una plaza usable con sus dos caminos. piso, fila y columna son los del
// ancla. caminoEntrada va de la Entrada a la boca; caminoSalida, de la boca
// (repetida) a la Salida más cercana.
public record PlazaReproduccionResponse(
        Integer piso,
        Integer fila,
        Integer columna,
        List<CeldaResponse> caminoEntrada,
        List<CeldaResponse> caminoSalida) {
}
