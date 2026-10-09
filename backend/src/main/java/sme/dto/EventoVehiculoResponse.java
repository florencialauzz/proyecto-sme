package sme.dto;

// Qué le pasó a un vehículo: indicePlaza es la posición en
// ReproduccionResponse.plazas, o -1 si fue rechazado (en ese caso
// minutoSalida = minutoLlegada).
public record EventoVehiculoResponse(Integer minutoLlegada, Integer minutoSalida, Integer indicePlaza) {
}
