package sme.dto;

import java.util.List;

public record ProyectoDetalleResponse(
        Long proyectoId,
        String nombre,
        Integer filasGrilla,
        Integer columnasGrilla,
        List<PiezaResponse> piezas,
        String estado) {
}
