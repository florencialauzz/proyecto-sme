package sme.simulacion;

import java.util.List;

public record ResultadoMotor(List<PuntoCurva> curva, int vehiculosLlegados, int vehiculosRechazados,
                             int plazasUsables, List<EventoVehiculo> eventos) {
    public ResultadoMotor(List<PuntoCurva> curva, int vehiculosLlegados, int vehiculosRechazados,
                          int plazasUsables) {
        this(curva, vehiculosLlegados, vehiculosRechazados, plazasUsables, List.of());
    }
}
