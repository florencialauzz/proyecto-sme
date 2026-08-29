package sme.service;

import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import sme.dto.CrearProyectoRequest;
import sme.dto.CrearProyectoResponse;
import sme.dto.GuardarGrillaRequest;
import sme.dto.GuardarGrillaResponse;
import sme.dto.PiezaRequest;
import sme.dto.ProyectoResumenResponse;
import sme.entity.Pieza;
import sme.entity.Proyecto;
import sme.exception.NegocioException;
import sme.repository.PiezaRepository;
import sme.repository.ProyectoRepository;

import java.util.List;

@Service
public class ProyectoService {

    private final ProyectoRepository proyectoRepository;
    private final PiezaRepository piezaRepository;

    public ProyectoService(ProyectoRepository proyectoRepository, PiezaRepository piezaRepository) {
        this.proyectoRepository = proyectoRepository;
        this.piezaRepository = piezaRepository;
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

    // RF-22
    public List<ProyectoResumenResponse> listar(Long usuarioId) {
        return proyectoRepository.findByUsuarioIdOrderByFechaModificacionDesc(usuarioId).stream()
                .map(proyecto -> new ProyectoResumenResponse(
                        proyecto.getId(),
                        proyecto.getNombre(),
                        proyecto.getEstado().name(),
                        proyecto.getFechaModificacion()))
                .toList();
    }

    // RF-13, RF-21: reemplaza la grilla completa del proyecto — se borran las
    // piezas anteriores y se insertan las nuevas en la misma transacción.
    @Transactional
    public GuardarGrillaResponse guardarGrilla(Long usuarioId, Long proyectoId, GuardarGrillaRequest request) {
        Proyecto proyecto = obtenerProyectoDelUsuario(usuarioId, proyectoId);

        piezaRepository.deleteByProyectoId(proyecto.getId());

        List<Pieza> piezas = request.piezas().stream()
                .map(dto -> aPieza(proyecto.getId(), dto))
                .toList();
        piezaRepository.saveAll(piezas);

        return new GuardarGrillaResponse(true);
    }

    private Pieza aPieza(Long proyectoId, PiezaRequest dto) {
        Pieza pieza = new Pieza();
        pieza.setProyectoId(proyectoId);
        pieza.setPiso(dto.piso());
        pieza.setFila(dto.fila());
        pieza.setColumna(dto.columna());
        pieza.setTipo(dto.tipo());
        pieza.setCaraAcceso(dto.caraAcceso());
        pieza.setEsAccesible(dto.esAccesible());
        return pieza;
    }

    // contratos/api-contract.md, sección 1: un proyecto de otro usuario responde
    // 404, no 403, para no confirmar que existe.
    private Proyecto obtenerProyectoDelUsuario(Long usuarioId, Long proyectoId) {
        Proyecto proyecto = proyectoRepository.findById(proyectoId)
                .orElseThrow(() -> new NegocioException(HttpStatus.NOT_FOUND, "PROYECTO_NO_ENCONTRADO", "Proyecto no encontrado"));

        if (!proyecto.getUsuarioId().equals(usuarioId)) {
            throw new NegocioException(HttpStatus.NOT_FOUND, "PROYECTO_NO_ENCONTRADO", "Proyecto no encontrado");
        }

        return proyecto;
    }
}
