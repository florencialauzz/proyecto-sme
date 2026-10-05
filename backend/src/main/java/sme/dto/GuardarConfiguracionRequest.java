package sme.dto;

import java.time.LocalTime;

// RF-08 a RF-11: PUT /proyectos/{id}/configuracion (contratos/api-contract.md, sección 4).
public record GuardarConfiguracionRequest(
        Integer cantidadPisos,
        Integer frecuenciaIngreso,
        Integer tiempoPermanencia,
        LocalTime horaInicioSimulacion,
        LocalTime horaFinSimulacion,
        // null se toma como true, el valor por defecto del checkbox: no es un
        // dato que falte.
        Boolean conFluctuaciones) {
}
