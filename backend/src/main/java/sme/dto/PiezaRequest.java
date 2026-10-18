package sme.dto;

import sme.entity.Direccion;
import sme.entity.TipoPieza;

public record PiezaRequest(
        Integer piso,
        Integer fila,
        Integer columna,
        TipoPieza tipo,
        Direccion caraAcceso,
        Boolean esAccesible,
        Boolean esCrucePeatonal,
        // String y no enum: JsonUtility manda "" en vez de null
        String sentidoVertical) {
}
