package sme.dto;

public record CambiarContrasenaRequest(
        String contrasenaActual,
        String contrasenaNueva,
        String confirmacionContrasenaNueva
) {
}
