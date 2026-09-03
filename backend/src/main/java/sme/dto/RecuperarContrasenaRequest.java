package sme.dto;

public record RecuperarContrasenaRequest(
        String nombreUsuario,
        String respuestaSeguridad,
        String nuevaContrasena,
        String confirmacionNuevaContrasena
) {
}
