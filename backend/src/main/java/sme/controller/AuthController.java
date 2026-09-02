package sme.controller;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.Authentication;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;
import sme.dto.CambiarContrasenaRequest;
import sme.dto.CambiarContrasenaResponse;
import sme.dto.EliminarCuentaRequest;
import sme.dto.EliminarCuentaResponse;
import sme.dto.LoginRequest;
import sme.dto.LoginResponse;
import sme.dto.PreguntaSeguridadResponse;
import sme.dto.RecuperarContrasenaRequest;
import sme.dto.RecuperarContrasenaResponse;
import sme.dto.RegistroRequest;
import sme.dto.RegistroResponse;
import sme.service.AuthService;

@RestController
@RequestMapping("/api/auth")
public class AuthController {

    private final AuthService authService;

    public AuthController(AuthService authService) {
        this.authService = authService;
    }

    // RF-01
    @PostMapping("/registro")
    public ResponseEntity<RegistroResponse> registrar(@RequestBody RegistroRequest request) {
        return ResponseEntity.status(HttpStatus.CREATED).body(authService.registrar(request));
    }

    // RF-02
    @PostMapping("/login")
    public LoginResponse login(@RequestBody LoginRequest request) {
        return authService.iniciarSesion(request);
    }

    // RF-03, punto 5: se consulta antes de pedir la respuesta, para mostrarla en pantalla.
    @GetMapping("/pregunta-seguridad")
    public PreguntaSeguridadResponse obtenerPreguntaSeguridad(@RequestParam String nombreUsuario) {
        return authService.obtenerPreguntaSeguridad(nombreUsuario);
    }

    // RF-03
    @PostMapping("/recuperar-contrasena")
    public RecuperarContrasenaResponse recuperarContrasena(@RequestBody RecuperarContrasenaRequest request) {
        return authService.recuperarContrasena(request);
    }

    // RF-04: autenticado, el usuarioId sale del token validado por JwtAuthFilter.
    @PutMapping("/contrasena")
    public CambiarContrasenaResponse cambiarContrasena(@RequestBody CambiarContrasenaRequest request,
                                                         Authentication authentication) {
        Long usuarioId = (Long) authentication.getPrincipal();
        return authService.cambiarContrasena(usuarioId, request);
    }

    // RF-05: autenticado, el usuarioId sale del token validado por JwtAuthFilter.
    @DeleteMapping("/cuenta")
    public EliminarCuentaResponse eliminarCuenta(@RequestBody EliminarCuentaRequest request,
                                                  Authentication authentication) {
        Long usuarioId = (Long) authentication.getPrincipal();
        return authService.eliminarCuenta(usuarioId, request);
    }
}
