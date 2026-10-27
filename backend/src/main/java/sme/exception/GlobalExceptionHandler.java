package sme.exception;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.TypeMismatchException;
import org.springframework.http.HttpStatus;
import org.springframework.http.HttpStatusCode;
import org.springframework.http.ResponseEntity;
import org.springframework.http.converter.HttpMessageNotReadableException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import sme.dto.ErrorResponse;

@RestControllerAdvice
public class GlobalExceptionHandler {
    private static final Logger log = LoggerFactory.getLogger(GlobalExceptionHandler.class);

    @ExceptionHandler(NegocioException.class)
    public ResponseEntity<ErrorResponse> manejarNegocio(NegocioException ex) {
        return ResponseEntity.status(ex.getStatus())
                .body(new ErrorResponse(ex.getMessage(), ex.getCodigo()));
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ErrorResponse> manejarInesperado(Exception ex) {
        HttpStatusCode statusSolicitud = obtenerStatusDeSolicitudInvalida(ex);

        if (statusSolicitud != null) {
            log.warn("Solicitud inválida ({}): {}", statusSolicitud.value(), ex.getMessage());
            return ResponseEntity.status(statusSolicitud)
                    .body(new ErrorResponse("La solicitud no es válida.", "SOLICITUD_INVALIDA"));
        }

        log.error("Error inesperado al procesar la solicitud", ex);
        return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                .body(new ErrorResponse("Ocurrió un error inesperado. Intentá de nuevo más tarde.", "ERROR_INTERNO"));
    }

    // Errores de Spring MVC causados por la solicitud. Devuelve null si la excepción no es una de ellas.
    private HttpStatusCode obtenerStatusDeSolicitudInvalida(Exception ex) {
        if (ex instanceof org.springframework.web.ErrorResponse errorDeSpring) {
            return errorDeSpring.getStatusCode();
        }
        // Estas dos no implementan ErrorResponse, pero Spring también las responde con 400
        if (ex instanceof HttpMessageNotReadableException || ex instanceof TypeMismatchException) {
            return HttpStatus.BAD_REQUEST;
        }
        return null;
    }
}
