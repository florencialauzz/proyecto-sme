package sme.dto;

import sme.entity.Direccion;
import sme.entity.TipoPieza;

// sentidoVertical viaja como String y no como el enum SentidoVertical: Unity
// (JsonUtility) manda "" en vez de null para las piezas que no son Rampa, y
// Jackson rechaza "" para un enum. ProyectoService lo convierte.
public record PiezaRequest(
        Integer piso,
        Integer fila,
        Integer columna,
        TipoPieza tipo,
        Direccion caraAcceso,
        Boolean esAccesible,
        Boolean esCrucePeatonal,
        String sentidoVertical) {
}
