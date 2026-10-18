package sme.controller;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.Authentication;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import sme.dto.CrearProyectoRequest;
import sme.dto.CrearProyectoResponse;
import sme.dto.DuplicarProyectoResponse;
import sme.dto.EliminarProyectoResponse;
import sme.dto.GuardarConfiguracionRequest;
import sme.dto.GuardarConfiguracionResponse;
import sme.dto.GuardarGrillaRequest;
import sme.dto.GuardarGrillaResponse;
import sme.dto.ProyectoDetalleResponse;
import sme.dto.ProyectoResumenResponse;
import sme.dto.RenombrarProyectoRequest;
import sme.dto.RenombrarProyectoResponse;
import sme.service.ProyectoService;

import java.util.List;

@RestController
@RequestMapping("/api/proyectos")
public class ProyectoController {
    private final ProyectoService proyectoService;

    public ProyectoController(ProyectoService proyectoService) {
        this.proyectoService = proyectoService;
    }

    @PostMapping
    public ResponseEntity<CrearProyectoResponse> crear(@RequestBody CrearProyectoRequest request,
                                                         Authentication authentication) {
        Long usuarioId = (Long) authentication.getPrincipal();
        return ResponseEntity.status(HttpStatus.CREATED).body(proyectoService.crear(usuarioId, request));
    }

    @GetMapping
    public ResponseEntity<List<ProyectoResumenResponse>> listar(Authentication authentication) {
        Long usuarioId = (Long) authentication.getPrincipal();
        return ResponseEntity.ok(proyectoService.listar(usuarioId));
    }

    @GetMapping("/{id}")
    public ResponseEntity<ProyectoDetalleResponse> obtenerDetalle(@PathVariable Long id, Authentication authentication) {
        Long usuarioId = (Long) authentication.getPrincipal();
        return ResponseEntity.ok(proyectoService.obtenerDetalle(usuarioId, id));
    }

    @PutMapping("/{id}/grilla")
    public ResponseEntity<GuardarGrillaResponse> guardarGrilla(@PathVariable Long id,
                                                                @RequestBody GuardarGrillaRequest request,
                                                                Authentication authentication) {
        Long usuarioId = (Long) authentication.getPrincipal();
        return ResponseEntity.ok(proyectoService.guardarGrilla(usuarioId, id, request));
    }

    @PostMapping("/{id}/duplicar")
    public ResponseEntity<DuplicarProyectoResponse> duplicar(@PathVariable Long id, Authentication authentication) {
        Long usuarioId = (Long) authentication.getPrincipal();
        return ResponseEntity.status(HttpStatus.CREATED).body(proyectoService.duplicar(usuarioId, id));
    }

    @PutMapping("/{id}/nombre")
    public ResponseEntity<RenombrarProyectoResponse> renombrar(@PathVariable Long id,
                                                               @RequestBody RenombrarProyectoRequest request,
                                                               Authentication authentication) {
        Long usuarioId = (Long) authentication.getPrincipal();
        return ResponseEntity.ok(proyectoService.renombrar(usuarioId, id, request));
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<EliminarProyectoResponse> eliminar(@PathVariable Long id, Authentication authentication) {
        Long usuarioId = (Long) authentication.getPrincipal();
        return ResponseEntity.ok(proyectoService.eliminar(usuarioId, id));
    }

    @PutMapping("/{id}/configuracion")
    public ResponseEntity<GuardarConfiguracionResponse> guardarConfiguracion(@PathVariable Long id,
                                                                              @RequestBody GuardarConfiguracionRequest request,
                                                                              Authentication authentication) {
        Long usuarioId = (Long) authentication.getPrincipal();
        return ResponseEntity.ok(proyectoService.guardarConfiguracion(usuarioId, id, request));
    }
}
