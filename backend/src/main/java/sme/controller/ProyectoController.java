package sme.controller;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.Authentication;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import sme.dto.CrearProyectoRequest;
import sme.dto.CrearProyectoResponse;
import sme.service.ProyectoService;

@RestController
@RequestMapping("/api/proyectos")
public class ProyectoController {

    private final ProyectoService proyectoService;

    public ProyectoController(ProyectoService proyectoService) {
        this.proyectoService = proyectoService;
    }

    // RF-07: el usuarioId sale del token validado por JwtAuthFilter, no del body.
    @PostMapping
    public ResponseEntity<CrearProyectoResponse> crear(@RequestBody CrearProyectoRequest request,
                                                         Authentication authentication) {
        Long usuarioId = (Long) authentication.getPrincipal();
        return ResponseEntity.status(HttpStatus.CREATED).body(proyectoService.crear(usuarioId, request));
    }
}
