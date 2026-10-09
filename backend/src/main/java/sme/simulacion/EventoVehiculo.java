package sme.simulacion;

// Qué le pasó a un vehículo de la demanda durante la simulación, para que
// Unity lo reproduzca como una grabación (animacion/reglas.md). El motor lo
// registra en el mismo momento en que decide; la reproducción no decide nada.
//
// - indicePlaza: posición de la plaza en el orden de asignación del motor (de
//   la más cercana a la Entrada a la más lejana), o SIN_PLAZA si fue
//   rechazado. Es -1 y no null por JsonUtility (contratos/api-contract.md).
// - minutoSalida: minutoLlegada + permanencia. Puede caer después del fin del
//   período: el vehículo queda estacionado hasta el final. Un rechazado no se
//   queda, así que su minutoSalida es el de llegada.
public record EventoVehiculo(int minutoLlegada, int minutoSalida, int indicePlaza) {

    public static final int SIN_PLAZA = -1;
}
