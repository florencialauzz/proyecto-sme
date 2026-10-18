package sme.dto;

import java.util.List;

public record ReproduccionResponse(
        List<PlazaReproduccionResponse> plazas,
        Integer indicePlazaRecorridoRechazados,
        List<EventoVehiculoResponse> eventos) {
}
