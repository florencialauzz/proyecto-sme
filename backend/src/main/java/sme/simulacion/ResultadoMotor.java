package sme.simulacion;

import java.util.List;

// Lo que registra el motor al recorrer el período. Los indicadores
// (eficiencia, saturación, demanda) se calculan después a partir de esto, en
// Indicadores.
//
// plazasUsables son las plazas que el motor pudo asignar: las alcanzables
// desde la Entrada y con camino a una Salida. Con un diseño que pasó RF-20
// son todas las de la grilla, pero la saturación se mide contra estas — si
// una plaza no se puede usar, el estacionamiento se llena sin que esa plaza
// llegue a ocuparse nunca.
//
// eventos: uno por vehículo llegado, en orden de llegada, para la
// reproducción animada (animacion/reglas.md). No intervienen en ningún
// indicador.
public record ResultadoMotor(List<PuntoCurva> curva, int vehiculosLlegados, int vehiculosRechazados,
                             int plazasUsables, List<EventoVehiculo> eventos) {

    // Para los tests de Indicadores, que arman el resultado a mano sin
    // simular y no necesitan eventos.
    public ResultadoMotor(List<PuntoCurva> curva, int vehiculosLlegados, int vehiculosRechazados,
                          int plazasUsables) {
        this(curva, vehiculosLlegados, vehiculosRechazados, plazasUsables, List.of());
    }
}
