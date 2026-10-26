package sme.dto;

import java.time.LocalTime;

public record GuardarConfiguracionRequest(
        Integer cantidadPisos,
        Integer frecuenciaIngreso,
        Integer tiempoPermanencia,
        LocalTime horaInicioSimulacion,
        LocalTime horaFinSimulacion,
        Boolean conFluctuaciones) {
}
