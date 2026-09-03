package sme.exception;

import org.springframework.http.HttpStatus;

// Errores de negocio esperados por los casos de uso (usuario duplicado, credenciales
// inválidas, etc.), distintos de errores internos inesperados. El controller advice
// los traduce al formato de error de contratos/api-contract.md.
public class NegocioException extends RuntimeException {

    private final HttpStatus status;
    private final String codigo;

    public NegocioException(HttpStatus status, String codigo, String mensaje) {
        super(mensaje);
        this.status = status;
        this.codigo = codigo;
    }

    public HttpStatus getStatus() { return status; }
    public String getCodigo() { return codigo; }
}
