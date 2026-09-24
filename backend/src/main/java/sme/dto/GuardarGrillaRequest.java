package sme.dto;

import java.util.List;

// cantidadPisos viaja con la grilla porque eliminar un piso se hace desde el
// editor y se guarda junto con el resto de los cambios, al tocar Guardar.
public record GuardarGrillaRequest(Integer cantidadPisos, List<PiezaRequest> piezas) {
}
