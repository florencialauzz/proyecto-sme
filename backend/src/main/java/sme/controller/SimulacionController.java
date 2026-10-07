package sme.controller;

import org.springframework.http.ResponseEntity;
import org.springframework.security.core.Authentication;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import sme.dto.EjecutarSimulacionResponse;
import sme.dto.GuardarSimulacionRequest;
import sme.dto.GuardarSimulacionResponse;
import sme.service.SimulacionService;

@RestController
@RequestMapping("/api/proyectos/{id}/simulacion")
public class SimulacionController {

    private final SimulacionService simulacionService;

    public SimulacionController(SimulacionService simulacionService) {
        this.simulacionService = simulacionService;
    }

    // RF-26 (incluye RF-15, RF-27, RF-28, RF-29 y RF-30 en la misma corrida)
    @PostMapping("/ejecutar")
    public ResponseEntity<EjecutarSimulacionResponse> ejecutar(@PathVariable Long id, Authentication authentication) {
        Long usuarioId = (Long) authentication.getPrincipal();
        return ResponseEntity.ok(simulacionService.ejecutar(usuarioId, id));
    }

    // RF-23
    @PostMapping("/guardar")
    public ResponseEntity<GuardarSimulacionResponse> guardar(@PathVariable Long id,
                                                             @RequestBody GuardarSimulacionRequest request,
                                                             Authentication authentication) {
        Long usuarioId = (Long) authentication.getPrincipal();
        return ResponseEntity.ok(simulacionService.guardar(usuarioId, id, request));
    }

    // RF-24 (reabrir resultados guardados) y RF-25 (comparar)
    @GetMapping
    public ResponseEntity<EjecutarSimulacionResponse> obtenerGuardado(@PathVariable Long id,
                                                                      Authentication authentication) {
        Long usuarioId = (Long) authentication.getPrincipal();
        return ResponseEntity.ok(simulacionService.obtenerGuardado(usuarioId, id));
    }
}
