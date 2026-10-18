package sme.simulacion;

import java.util.List;

public record CaminosDePlaza(CeldaSimulacion plaza, List<CeldaSimulacion> caminoEntrada,
                             List<CeldaSimulacion> caminoSalida) {
}
