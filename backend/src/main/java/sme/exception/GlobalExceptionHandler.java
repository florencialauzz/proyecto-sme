package sme.exception;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import sme.dto.ErrorResponse;

@RestControllerAdvice
public class GlobalExceptionHandler {

    @ExceptionHandler(NegocioException.class)
    public ResponseEntity<ErrorResponse> manejarNegocio(NegocioException ex) {
        return ResponseEntity.status(ex.getStatus())
                .body(new ErrorResponse(ex.getMessage(), ex.getCodigo()));
    }
}
