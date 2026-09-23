package sme.dto;

import java.time.LocalTime;
import java.util.List;

public record ProyectoDetalleResponse(
        Long proyectoId,
        String nombre,
        Integer cantidadPisos,
        Integer frecuenciaIngreso,
        Integer tiempoPermanencia,
        LocalTime horaInicioSimulacion,
        LocalTime horaFinSimulacion,
        Integer filasGrilla,
        Integer columnasGrilla,
        List<PiezaResponse> piezas,
        String estado) {
}
