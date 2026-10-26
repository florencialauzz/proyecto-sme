package sme.dto;

import java.util.List;

public record GuardarGrillaRequest(Integer cantidadPisos, List<PiezaRequest> piezas) {
}
