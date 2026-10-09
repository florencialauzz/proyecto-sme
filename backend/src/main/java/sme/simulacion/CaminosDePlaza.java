package sme.simulacion;

import java.util.List;

// Por dónde circula un vehículo que estaciona en esta plaza, para la
// reproducción animada (animacion/reglas.md). Se calcula una vez por diseño:
// no depende de la demanda ni de qué plazas estén ocupadas.
//
// - caminoEntrada: de la Entrada (primera) a la boca de la plaza (última).
// - caminoSalida: de la boca (primera, repetida a propósito para que cada
//   tramo se pueda usar solo) a la Salida más cercana (última).
public record CaminosDePlaza(CeldaSimulacion plaza, List<CeldaSimulacion> caminoEntrada,
                             List<CeldaSimulacion> caminoSalida) {
}
