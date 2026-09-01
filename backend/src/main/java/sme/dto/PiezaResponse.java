package sme.dto;

public record PiezaResponse(
        Integer piso,
        Integer fila,
        Integer columna,
        String tipo,
        String caraAcceso,
        Boolean esAccesible) {
}
