package sme.controller;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.Authentication;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import sme.dto.CrearProyectoRequest;
import sme.dto.CrearProyectoResponse;
import sme.dto.GuardarConfiguracionRequest;
import sme.dto.GuardarConfiguracionResponse;
import sme.dto.GuardarGrillaRequest;
import sme.dto.GuardarGrillaResponse;
import sme.dto.ProyectoDetalleResponse;
import sme.dto.ProyectoResumenResponse;
import sme.service.ProyectoService;

import java.util.List;

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

    // RF-22
    @GetMapping
    public ResponseEntity<List<ProyectoResumenResponse>> listar(Authentication authentication) {
        Long usuarioId = (Long) authentication.getPrincipal();
        return ResponseEntity.ok(proyectoService.listar(usuarioId));
    }

    // Abrir un proyecto guardado para seguir editándolo.
    @GetMapping("/{id}")
    public ResponseEntity<ProyectoDetalleResponse> obtenerDetalle(@PathVariable Long id, Authentication authentication) {
        Long usuarioId = (Long) authentication.getPrincipal();
        return ResponseEntity.ok(proyectoService.obtenerDetalle(usuarioId, id));
    }

    // RF-13, RF-21
    @PutMapping("/{id}/grilla")
    public ResponseEntity<GuardarGrillaResponse> guardarGrilla(@PathVariable Long id,
                                                                @RequestBody GuardarGrillaRequest request,
                                                                Authentication authentication) {
        Long usuarioId = (Long) authentication.getPrincipal();
        return ResponseEntity.ok(proyectoService.guardarGrilla(usuarioId, id, request));
    }

    // RF-08 a RF-11
    @PutMapping("/{id}/configuracion")
    public ResponseEntity<GuardarConfiguracionResponse> guardarConfiguracion(@PathVariable Long id,
                                                                              @RequestBody GuardarConfiguracionRequest request,
                                                                              Authentication authentication) {
        Long usuarioId = (Long) authentication.getPrincipal();
        return ResponseEntity.ok(proyectoService.guardarConfiguracion(usuarioId, id, request));
    }
}
