package sme.dto;

public record RegistroRequest(
        String nombreUsuario,
        String contrasena,
        String confirmacionContrasena,
        String preguntaSeguridad,
        String respuestaSeguridad
) {
}
