package sme.exception;

import org.junit.jupiter.api.Test;
import org.springframework.http.HttpInputMessage;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.http.converter.HttpMessageNotReadableException;
import org.springframework.web.HttpRequestMethodNotSupportedException;
import sme.dto.ErrorResponse;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.mockito.Mockito.mock;

class GlobalExceptionHandlerTest {
    private final GlobalExceptionHandler handler = new GlobalExceptionHandler();

    @Test
    void errorInesperadoRespondeErrorInternoSinDetallesTecnicos() {
        RuntimeException ex = new RuntimeException("Connection refused: jdbc:mysql://localhost:3306/sme");

        ResponseEntity<ErrorResponse> respuesta = handler.manejarInesperado(ex);

        assertEquals(500, respuesta.getStatusCode().value());
        assertEquals("ERROR_INTERNO", respuesta.getBody().codigo());
        assertFalse(respuesta.getBody().error().contains("Connection refused"));
        assertFalse(respuesta.getBody().toString().contains("jdbc"));
    }

    @Test
    void jsonMalFormadoRespondeSolicitudInvalidaCon400() {
        HttpMessageNotReadableException ex = new HttpMessageNotReadableException("JSON parse error", mock(HttpInputMessage.class));

        ResponseEntity<ErrorResponse> respuesta = handler.manejarInesperado(ex);

        assertEquals(400, respuesta.getStatusCode().value());
        assertEquals("SOLICITUD_INVALIDA", respuesta.getBody().codigo());
        assertEquals("La solicitud no es válida.", respuesta.getBody().error());
    }

    @Test
    void errorDeSpringConservaSuPropioCodigoHttp() {
        HttpRequestMethodNotSupportedException ex = new HttpRequestMethodNotSupportedException("DELETE");

        ResponseEntity<ErrorResponse> respuesta = handler.manejarInesperado(ex);

        assertEquals(405, respuesta.getStatusCode().value());
        assertEquals("SOLICITUD_INVALIDA", respuesta.getBody().codigo());
    }

    @Test
    void errorDeNegocioSigueRespondiendoComoAntes() {
        NegocioException ex = new NegocioException(HttpStatus.UNAUTHORIZED, "CONTRASENA_INCORRECTA", "La contraseña es incorrecta.");

        ResponseEntity<ErrorResponse> respuesta = handler.manejarNegocio(ex);

        assertEquals(401, respuesta.getStatusCode().value());
        assertEquals("CONTRASENA_INCORRECTA", respuesta.getBody().codigo());
        assertEquals("La contraseña es incorrecta.", respuesta.getBody().error());
    }
}
