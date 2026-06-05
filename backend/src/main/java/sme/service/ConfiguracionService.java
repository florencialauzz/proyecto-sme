package sme.service;

import org.springframework.stereotype.Service;
import sme.model.Configuracion;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.concurrent.atomic.AtomicLong;

@Service
public class ConfiguracionService {

    private final List<Configuracion> configuraciones = new ArrayList<>();
    private final AtomicLong contadorId = new AtomicLong(1);

    public Configuracion crear(Configuracion configuracion) {
        configuracion.setId(contadorId.getAndIncrement());
        configuraciones.add(configuracion);
        return configuracion;
    }

    public List<Configuracion> listar() {
        return configuraciones;
    }

    public Optional<Configuracion> buscarPorId(Long id) {
        return configuraciones.stream()
                .filter(c -> c.getId().equals(id))
                .findFirst();
    }

    public double calcularDensidad(Configuracion configuracion) {
        if (configuracion.getMetrosCuadrados() <= 0) {
            return 0;
        }
        return configuracion.getPlazas() / configuracion.getMetrosCuadrados();
    }
}