package sme.dto;

import com.fasterxml.jackson.annotation.JsonFormat;

import java.time.LocalTime;
import java.util.List;

public record ProyectoDetalleResponse(
        Long proyectoId,
        String nombre,
        Integer cantidadPisos,
        Integer frecuenciaIngreso,
        Integer tiempoPermanencia,
        @JsonFormat(pattern = "HH:mm") LocalTime horaInicioSimulacion,
        @JsonFormat(pattern = "HH:mm") LocalTime horaFinSimulacion,
        Boolean conFluctuaciones,
        Integer filasGrilla,
        Integer columnasGrilla,
        List<PiezaResponse> piezas) {
}
