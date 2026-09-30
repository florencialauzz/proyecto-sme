package sme.controller;

import org.springframework.http.ResponseEntity;
import org.springframework.security.core.Authentication;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import sme.dto.EjecutarSimulacionResponse;
import sme.service.SimulacionService;

@RestController
@RequestMapping("/api/proyectos/{id}/simulacion")
public class SimulacionController {

    private final SimulacionService simulacionService;

    public SimulacionController(SimulacionService simulacionService) {
        this.simulacionService = simulacionService;
    }

    // RF-26 (incluye RF-15, RF-27, RF-28 y RF-29 en la misma corrida)
    @PostMapping("/ejecutar")
    public ResponseEntity<EjecutarSimulacionResponse> ejecutar(@PathVariable Long id, Authentication authentication) {
        Long usuarioId = (Long) authentication.getPrincipal();
        return ResponseEntity.ok(simulacionService.ejecutar(usuarioId, id));
    }
}
