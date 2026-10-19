package sme.dto;

import java.time.LocalDateTime;

public record ProyectoResumenResponse(Long proyectoId, String nombre, LocalDateTime fechaModificacion) {
}
