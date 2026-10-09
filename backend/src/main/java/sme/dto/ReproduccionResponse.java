package sme.dto;

import java.util.List;

// Animación (animacion/reglas.md): lo que Unity reproduce como una grabación.
// Solo viaja en /ejecutar; no se persiste, no va a /guardar ni vuelve en
// GET /simulacion. Detalle de cada campo en contratos/api-contract.md.
//
// - plazas: las usables, en el orden en que el motor las asigna. La posición
//   es el indicePlaza de cada evento.
// - indicePlazaRecorridoRechazados: los rechazados recorren los dos caminos
//   de esa plaza (la más lejana). -1 si no hay plazas usables.
// - eventos: uno por vehículo llegado, en orden de llegada.
public record ReproduccionResponse(
        List<PlazaReproduccionResponse> plazas,
        Integer indicePlazaRecorridoRechazados,
        List<EventoVehiculoResponse> eventos) {
}
