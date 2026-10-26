package sme.dto;

import java.util.List;

public record PlazaReproduccionResponse(
        Integer piso,
        Integer fila,
        Integer columna,
        List<CeldaResponse> caminoEntrada,
        List<CeldaResponse> caminoSalida) {
}
