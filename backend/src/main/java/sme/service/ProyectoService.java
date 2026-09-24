package sme.service;

import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import sme.dto.CrearProyectoRequest;
import sme.dto.CrearProyectoResponse;
import sme.dto.GuardarConfiguracionRequest;
import sme.dto.GuardarConfiguracionResponse;
import sme.dto.GuardarGrillaRequest;
import sme.dto.GuardarGrillaResponse;
import sme.dto.PiezaRequest;
import sme.dto.PiezaResponse;
import sme.dto.ProyectoDetalleResponse;
import sme.dto.ProyectoResumenResponse;
import sme.entity.Direccion;
import sme.entity.Pieza;
import sme.entity.Proyecto;
import sme.entity.SentidoVertical;
import sme.entity.TipoPieza;
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

    // Abrir un proyecto guardado para seguir editándolo (contrato ya preveía
    // este endpoint en la sección 2, no estaba implementado).
    public ProyectoDetalleResponse obtenerDetalle(Long usuarioId, Long proyectoId) {
        Proyecto proyecto = obtenerProyectoDelUsuario(usuarioId, proyectoId);

        List<PiezaResponse> piezas = piezaRepository.findByProyectoId(proyecto.getId()).stream()
                .map(pieza -> {
                    Direccion orientacion = usaColumnaDireccion(pieza.getTipo()) ? pieza.getDireccion() : pieza.getCaraAcceso();
                    SentidoVertical sentidoVertical = pieza.getSentidoVertical();
                    return new PiezaResponse(
                            pieza.getPiso(),
                            pieza.getFila(),
                            pieza.getColumna(),
                            pieza.getTipo().name(),
                            orientacion == null ? null : orientacion.name(),
                            pieza.getEsAccesible(),
                            pieza.getEsCrucePeatonal(),
                            sentidoVertical == null ? null : sentidoVertical.name());
                })
                .toList();

        return new ProyectoDetalleResponse(
                proyecto.getId(),
                proyecto.getNombre(),
                proyecto.getCantidadPisos(),
                proyecto.getFrecuenciaIngreso(),
                proyecto.getTiempoPermanencia(),
                proyecto.getHoraInicioSimulacion(),
                proyecto.getHoraFinSimulacion(),
                proyecto.getFilasGrilla(),
                proyecto.getColumnasGrilla(),
                piezas,
                proyecto.getEstado().name());
    }

    // RF-08 a RF-11: cada validación corresponde al Flujo Alternativo A1 de su
    // propio caso de uso (producto/casos-de-uso-expandidos.md) — se chequean
    // en orden (pisos, frecuencia, permanencia, horario) y se corta en la
    // primera que falle, igual que el resto de los métodos de este service.
    public GuardarConfiguracionResponse guardarConfiguracion(Long usuarioId, Long proyectoId, GuardarConfiguracionRequest request) {
        Proyecto proyecto = obtenerProyectoDelUsuario(usuarioId, proyectoId);

        if (request.cantidadPisos() == null || request.cantidadPisos() < 1) {
            throw new NegocioException(HttpStatus.BAD_REQUEST, "CANTIDAD_PISOS_INVALIDA",
                    "La cantidad de pisos debe ser un número entero mayor o igual a uno");
        }
        // Bajar la cantidad de pisos desde acá dejaría piezas colgando en pisos
        // que ya no existen, sin que el usuario elija cuál perder. Se hace
        // eliminando el piso puntual desde el editor (guardarGrilla).
        if (proyecto.getCantidadPisos() != null && request.cantidadPisos() < proyecto.getCantidadPisos()) {
            throw new NegocioException(HttpStatus.BAD_REQUEST, "CANTIDAD_PISOS_MENOR",
                    "No se puede reducir la cantidad de pisos desde la configuración. "
                            + "Eliminá el piso que quieras quitar desde el editor");
        }
        if (request.frecuenciaIngreso() == null || request.frecuenciaIngreso() <= 0) {
            throw new NegocioException(HttpStatus.BAD_REQUEST, "FRECUENCIA_INVALIDA",
                    "La frecuencia debe ser un valor numérico mayor a cero");
        }
        if (request.tiempoPermanencia() == null || request.tiempoPermanencia() <= 0) {
            throw new NegocioException(HttpStatus.BAD_REQUEST, "TIEMPO_PERMANENCIA_INVALIDO",
                    "El tiempo de permanencia debe ser un valor numérico mayor a cero");
        }
        if (request.horaInicioSimulacion() == null || request.horaFinSimulacion() == null
                || !request.horaFinSimulacion().isAfter(request.horaInicioSimulacion())) {
            throw new NegocioException(HttpStatus.BAD_REQUEST, "HORARIO_INVALIDO",
                    "La hora de fin debe ser posterior a la hora de inicio");
        }

        proyecto.setCantidadPisos(request.cantidadPisos());
        proyecto.setFrecuenciaIngreso(request.frecuenciaIngreso());
        proyecto.setTiempoPermanencia(request.tiempoPermanencia());
        proyecto.setHoraInicioSimulacion(request.horaInicioSimulacion());
        proyecto.setHoraFinSimulacion(request.horaFinSimulacion());
        proyectoRepository.save(proyecto);

        return new GuardarConfiguracionResponse(true);
    }

    // RF-13, RF-21: reemplaza la grilla completa del proyecto — se borran las
    // piezas anteriores y se insertan las nuevas en la misma transacción.
    //
    // RF-18: cantidadPisos solo puede bajar por acá (eliminar un piso desde el
    // editor); subir se hace desde la configuración. Las validaciones de
    // colocación (RF-19) corren en Unity — esto solo evita guardar piezas en
    // un piso que el proyecto no tiene.
    @Transactional
    public GuardarGrillaResponse guardarGrilla(Long usuarioId, Long proyectoId, GuardarGrillaRequest request) {
        Proyecto proyecto = obtenerProyectoDelUsuario(usuarioId, proyectoId);

        int pisosGuardados = proyecto.getCantidadPisos() == null ? 1 : proyecto.getCantidadPisos();
        int pisosNuevos = request.cantidadPisos() == null ? pisosGuardados : request.cantidadPisos();
        if (pisosNuevos < 1 || pisosNuevos > pisosGuardados) {
            throw new NegocioException(HttpStatus.BAD_REQUEST, "CANTIDAD_PISOS_INVALIDA",
                    "La cantidad de pisos no es válida para este proyecto");
        }
        for (PiezaRequest dto : request.piezas()) {
            if (dto.piso() == null || dto.piso() < 0 || dto.piso() >= pisosNuevos) {
                throw new NegocioException(HttpStatus.BAD_REQUEST, "PISO_INEXISTENTE",
                        "Hay piezas en un piso que el proyecto no tiene");
            }
        }

        if (proyecto.getCantidadPisos() != null) {
            proyecto.setCantidadPisos(pisosNuevos);
            proyectoRepository.save(proyecto);
        }

        // flush() fuerza el DELETE a ejecutarse ya, antes del saveAll: sin esto,
        // Hibernate ordena el flush por tipo de acción (todos los INSERT antes
        // que los DELETE, sin importar el orden en que se llamaron acá), y una
        // pieza reinsertada en la misma celda que tenía antes choca con la fila
        // vieja todavía no borrada (uq_pieza_celda).
        piezaRepository.deleteByProyectoId(proyecto.getId());
        piezaRepository.flush();

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
        // La Escalera no tiene orientación (dominio/modelo-clases.md): lo que
        // venga en caraAcceso se descarta en vez de guardarse en una columna.
        if (dto.tipo() == TipoPieza.ESCALERA) {
            pieza.setDireccion(null);
            pieza.setCaraAcceso(null);
        } else if (usaColumnaDireccion(dto.tipo())) {
            pieza.setDireccion(dto.caraAcceso());
        } else {
            pieza.setCaraAcceso(dto.caraAcceso());
        }
        pieza.setEsAccesible(dto.esAccesible());
        pieza.setEsCrucePeatonal(dto.esCrucePeatonal());
        if (dto.tipo() == TipoPieza.RAMPA) {
            pieza.setSentidoVertical(SentidoVertical.valueOf(dto.sentidoVertical()));
        }
        return pieza;
    }

    // El wire contract (contratos/api-contract.md) manda la orientación de
    // cualquier pieza en un solo campo JSON ("caraAcceso"), pero el modelo de
    // dominio la separa en dos columnas según el tipo (dominio/modelo-clases.md):
    // Calle/Entrada/Salida/Rampa orientan su circulación ("direccion"), Plaza/
    // ZonaBicicletasMotos orientan su lado de acceso ("cara_acceso").
    private boolean usaColumnaDireccion(TipoPieza tipo) {
        return tipo == TipoPieza.CALLE || tipo == TipoPieza.ENTRADA || tipo == TipoPieza.SALIDA
                || tipo == TipoPieza.RAMPA;
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
