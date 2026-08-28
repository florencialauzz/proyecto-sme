package sme.service;

import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import sme.dto.CrearProyectoRequest;
import sme.dto.CrearProyectoResponse;
import sme.entity.Proyecto;
import sme.exception.NegocioException;
import sme.repository.ProyectoRepository;

@Service
public class ProyectoService {

    private final ProyectoRepository proyectoRepository;

    public ProyectoService(ProyectoRepository proyectoRepository) {
        this.proyectoRepository = proyectoRepository;
    }

    // RF-07: valida nombre vacío (A1) y nombre repetido para el mismo usuario (A2)
    // antes de crear el proyecto. La grilla (RF-12) sale con los valores por
    // defecto de la entidad (15x15) — no hace falta pedirlos.
    public CrearProyectoResponse crear(Long usuarioId, CrearProyectoRequest request) {
        if (request.nombre() == null || request.nombre().isBlank()) {
            throw new NegocioException(HttpStatus.BAD_REQUEST, "NOMBRE_VACIO", "El nombre del proyecto no puede estar vacío");
        }
        if (proyectoRepository.existsByUsuarioIdAndNombre(usuarioId, request.nombre())) {
            throw new NegocioException(HttpStatus.CONFLICT, "PROYECTO_DUPLICADO", "Ya tiene un proyecto con ese nombre");
        }

        Proyecto proyecto = new Proyecto();
        proyecto.setUsuarioId(usuarioId);
        proyecto.setNombre(request.nombre());

        proyecto = proyectoRepository.save(proyecto);
        return new CrearProyectoResponse(proyecto.getId(), proyecto.getFilasGrilla(), proyecto.getColumnasGrilla(),
                proyecto.getEstado().name());
    }
}
