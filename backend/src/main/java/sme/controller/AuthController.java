package sme.controller;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import sme.dto.LoginRequest;
import sme.dto.LoginResponse;
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
}
