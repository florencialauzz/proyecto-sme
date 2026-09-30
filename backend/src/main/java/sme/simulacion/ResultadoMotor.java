package sme.simulacion;

import java.util.List;

// Lo que registra el motor al recorrer el período. Los indicadores
// (eficiencia, saturación, demanda) se calculan después a partir de esto, en
// Indicadores.
public record ResultadoMotor(List<PuntoCurva> curva, int vehiculosLlegados, int vehiculosRechazados) {
}
