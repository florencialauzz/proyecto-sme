package sme.simulacion;

// Un vehículo de la demanda: en qué minuto del período llega y cuántos minutos
// se queda si consigue plaza. Lo arma GeneradorDemanda antes de simular.
public record Vehiculo(int minutoLlegada, int tiempoPermanencia) {
}
