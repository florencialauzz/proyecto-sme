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
        // El contrato fija "HH:mm" para horas solas; sin esto Jackson manda
        // "HH:mm:ss" y el formulario de configuración lo rechaza al reabrir.
        @JsonFormat(pattern = "HH:mm") LocalTime horaInicioSimulacion,
        @JsonFormat(pattern = "HH:mm") LocalTime horaFinSimulacion,
        Integer filasGrilla,
        Integer columnasGrilla,
        List<PiezaResponse> piezas,
        String estado) {
}
