package sme.controllers;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import sme.model.Configuracion;
import sme.service.ConfiguracionService;

import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/configuraciones")
public class ConfiguracionController {

    private final ConfiguracionService service;

    public ConfiguracionController(ConfiguracionService service) {
        this.service = service;
    }

    // RF-01: crear una configuración
    @PostMapping
    public Configuracion crear(@RequestBody Configuracion configuracion) {
        return service.crear(configuracion);
    }

    // RF-02: listar todas
    @GetMapping
    public List<Configuracion> listar() {
        return service.listar();
    }

    // RF-03: obtener una por id, con la densidad calculada
    @GetMapping("/{id}")
    public ResponseEntity<?> obtener(@PathVariable Long id) {
        return service.buscarPorId(id)
                .map(c -> ResponseEntity.ok(Map.of(
                        "configuracion", c,
                        "densidad", service.calcularDensidad(c))))
                .orElse(ResponseEntity.notFound().build());
    }
}