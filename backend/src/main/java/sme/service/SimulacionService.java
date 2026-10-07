package sme.service;

import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import sme.dto.EjecutarSimulacionResponse;
import sme.dto.GuardarSimulacionRequest;
import sme.dto.GuardarSimulacionResponse;
import sme.dto.PeriodoSaturacionResponse;
import sme.dto.PuntoOcupacionResponse;
import sme.entity.PeriodoSaturacion;
import sme.entity.Pieza;
import sme.entity.Proyecto;
import sme.entity.PuntoOcupacion;
import sme.entity.ResultadoSimulacion;
import sme.exception.NegocioException;
import sme.repository.PeriodoSaturacionRepository;
import sme.repository.PiezaRepository;
import sme.repository.ProyectoRepository;
import sme.repository.PuntoOcupacionRepository;
import sme.repository.ResultadoSimulacionRepository;
import sme.simulacion.GeneradorDemanda;
import sme.simulacion.GrillaSimulacion;
import sme.simulacion.Indicadores;
import sme.simulacion.IntervaloSaturacion;
import sme.simulacion.MotorSimulacion;
import sme.simulacion.ResultadoMotor;
import sme.simulacion.Vehiculo;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.time.Duration;
import java.util.List;

@Service
public class SimulacionService {

    private final ProyectoRepository proyectoRepository;
    private final PiezaRepository piezaRepository;
    private final ResultadoSimulacionRepository resultadoSimulacionRepository;
    private final PuntoOcupacionRepository puntoOcupacionRepository;
    private final PeriodoSaturacionRepository periodoSaturacionRepository;

    public SimulacionService(ProyectoRepository proyectoRepository,
                             PiezaRepository piezaRepository,
                             ResultadoSimulacionRepository resultadoSimulacionRepository,
                             PuntoOcupacionRepository puntoOcupacionRepository,
                             PeriodoSaturacionRepository periodoSaturacionRepository) {
        this.proyectoRepository = proyectoRepository;
        this.piezaRepository = piezaRepository;
        this.resultadoSimulacionRepository = resultadoSimulacionRepository;
        this.puntoOcupacionRepository = puntoOcupacionRepository;
        this.periodoSaturacionRepository = periodoSaturacionRepository;
    }

    // RF-26: simula sobre la grilla guardada y devuelve el resultado sin
    // persistir nada — el usuario puede ejecutar varias veces antes de
    // decidir guardar (RF-23).
    //
    // Precondición del caso de uso: configuración completa y diseño que pasa
    // RF-20. La validación de consistencia corre en Unity (el botón de
    // simular está bloqueado mientras haya advertencias); acá solo se
    // rechaza lo que haría fallar al motor, sin volver a validar el diseño.
    public EjecutarSimulacionResponse ejecutar(Long usuarioId, Long proyectoId) {
        Proyecto proyecto = obtenerProyectoDelUsuario(usuarioId, proyectoId);

        boolean configuracionCompleta = proyecto.getCantidadPisos() != null
                && proyecto.getFrecuenciaIngreso() != null
                && proyecto.getTiempoPermanencia() != null
                && proyecto.getHoraInicioSimulacion() != null
                && proyecto.getHoraFinSimulacion() != null;
        if (!configuracionCompleta) {
            throw new NegocioException(HttpStatus.BAD_REQUEST, "CONFIGURACION_INCOMPLETA",
                    "Completá la configuración del proyecto antes de simular");
        }

        List<Pieza> piezas = piezaRepository.findByProyectoId(proyecto.getId());
        GrillaSimulacion grilla = new GrillaSimulacion(
                proyecto.getCantidadPisos(),
                proyecto.getFilasGrilla(),
                proyecto.getColumnasGrilla(),
                piezas);

        if (grilla.getEntradas().size() != 1 || grilla.getSalidas().isEmpty()) {
            throw new NegocioException(HttpStatus.BAD_REQUEST, "DISENO_INVALIDO",
                    "El diseño guardado tiene que tener una Entrada y una Salida");
        }
        if (grilla.getPlazas().isEmpty()) {
            throw new NegocioException(HttpStatus.BAD_REQUEST, "DISENO_INVALIDO",
                    "El diseño guardado no tiene plazas");
        }

        int duracionMinutos = (int) Duration.between(proyecto.getHoraInicioSimulacion(),
                proyecto.getHoraFinSimulacion()).toMinutes();

        List<Vehiculo> vehiculos;
        if (proyecto.getConFluctuaciones()) {
            vehiculos = GeneradorDemanda.conFluctuaciones(proyecto.getFrecuenciaIngreso(),
                    proyecto.getTiempoPermanencia(), duracionMinutos);
        } else {
            vehiculos = GeneradorDemanda.sinFluctuaciones(proyecto.getFrecuenciaIngreso(),
                    proyecto.getTiempoPermanencia(), duracionMinutos);
        }

        MotorSimulacion motor = new MotorSimulacion(grilla);
        ResultadoMotor resultado = motor.ejecutar(vehiculos, duracionMinutos);

        List<PuntoOcupacionResponse> curva = resultado.curva().stream()
                .map(punto -> new PuntoOcupacionResponse(punto.minuto(), punto.cantidadOcupadas()))
                .toList();

        List<IntervaloSaturacion> intervalos = Indicadores.periodosSaturacion(resultado.curva(),
                resultado.plazasUsables());
        List<PeriodoSaturacionResponse> periodosSaturacion = intervalos.stream()
                .map(intervalo -> new PeriodoSaturacionResponse(intervalo.inicio(), intervalo.fin()))
                .toList();

        BigDecimal demandaSatisfecha = Indicadores.demandaSatisfecha(resultado);
        BigDecimal puntuacionGeneral = Indicadores.puntuacionGeneral(grilla, demandaSatisfecha);
        String calificacionTexto = Indicadores.calificacionPuntuacion(puntuacionGeneral);

        return new EjecutarSimulacionResponse(
                Indicadores.eficienciaEspacial(grilla),
                Indicadores.calificacionEficiencia(grilla),
                curva,
                demandaSatisfecha,
                resultado.vehiculosRechazados(),
                periodosSaturacion,
                puntuacionGeneral,
                calificacionTexto);
    }

    // RF-23: guarda el resultado que el cliente recibió de /ejecutar y
    // reenvía. Si el proyecto ya tenía resultados, se reemplazan (A2): se
    // borra el anterior (la curva y los períodos se van en cascada) y se
    // inserta el nuevo, en la misma transacción.
    @Transactional
    public GuardarSimulacionResponse guardar(Long usuarioId, Long proyectoId, GuardarSimulacionRequest request) {
        Proyecto proyecto = obtenerProyectoDelUsuario(usuarioId, proyectoId);

        // A1: sin una simulación ejecutada no hay nada que guardar. En Unity
        // el botón ni se habilita; esto cubre un request incompleto.
        boolean resultadoCompleto = request.eficienciaEspacial() != null
                && request.calificacionEficiencia() != null
                && request.curvaOcupacion() != null
                && request.demandaSatisfecha() != null
                && request.vehiculosRechazados() != null
                && request.periodosSaturacion() != null
                && request.puntuacionGeneral() != null
                && request.calificacionTexto() != null;
        if (!resultadoCompleto) {
            throw new NegocioException(HttpStatus.BAD_REQUEST, "SIN_SIMULACION",
                    "Ejecutá una simulación antes de guardar resultados");
        }

        resultadoSimulacionRepository.deleteByProyectoId(proyecto.getId());

        // Unity manda los porcentajes como float (28.5 puede llegar como
        // 28.499999): se redondean a los dos decimales de DECIMAL(5,2).
        ResultadoSimulacion resultado = new ResultadoSimulacion();
        resultado.setProyectoId(proyecto.getId());
        resultado.setEficienciaEspacial(dosDecimales(request.eficienciaEspacial()));
        resultado.setCalificacionEficiencia(request.calificacionEficiencia());
        resultado.setDemandaSatisfecha(dosDecimales(request.demandaSatisfecha()));
        resultado.setVehiculosRechazados(request.vehiculosRechazados());
        resultado.setPuntuacionGeneral(dosDecimales(request.puntuacionGeneral()));
        resultado.setCalificacionTexto(request.calificacionTexto());
        resultado = resultadoSimulacionRepository.save(resultado);

        Long resultadoId = resultado.getId();
        List<PuntoOcupacion> puntos = request.curvaOcupacion().stream()
                .map(dto -> {
                    PuntoOcupacion punto = new PuntoOcupacion();
                    punto.setResultadoSimulacionId(resultadoId);
                    punto.setMinuto(dto.minuto());
                    punto.setCantidadOcupadas(dto.cantidadOcupadas());
                    return punto;
                })
                .toList();
        puntoOcupacionRepository.saveAll(puntos);

        List<PeriodoSaturacion> periodos = request.periodosSaturacion().stream()
                .map(dto -> {
                    PeriodoSaturacion periodo = new PeriodoSaturacion();
                    periodo.setResultadoSimulacionId(resultadoId);
                    periodo.setInicio(dto.inicio());
                    periodo.setFin(dto.fin());
                    return periodo;
                })
                .toList();
        periodoSaturacionRepository.saveAll(periodos);

        return new GuardarSimulacionResponse(true);
    }

    // RF-24 al reabrir un proyecto, y RF-25 (una llamada por proyecto). Mismo
    // shape que /ejecutar. Sin resultados guardados, 404 SIN_RESULTADOS: RF-25
    // lo usa para su Flujo Alternativo A1.
    public EjecutarSimulacionResponse obtenerGuardado(Long usuarioId, Long proyectoId) {
        Proyecto proyecto = obtenerProyectoDelUsuario(usuarioId, proyectoId);

        ResultadoSimulacion resultado = resultadoSimulacionRepository.findByProyectoId(proyecto.getId())
                .orElseThrow(() -> new NegocioException(HttpStatus.NOT_FOUND, "SIN_RESULTADOS",
                        "El proyecto no tiene resultados de simulación guardados"));

        List<PuntoOcupacionResponse> curva = puntoOcupacionRepository
                .findByResultadoSimulacionIdOrderByMinutoAsc(resultado.getId()).stream()
                .map(punto -> new PuntoOcupacionResponse(punto.getMinuto(), punto.getCantidadOcupadas()))
                .toList();

        List<PeriodoSaturacionResponse> periodosSaturacion = periodoSaturacionRepository
                .findByResultadoSimulacionIdOrderByInicioAsc(resultado.getId()).stream()
                .map(periodo -> new PeriodoSaturacionResponse(periodo.getInicio(), periodo.getFin()))
                .toList();

        return new EjecutarSimulacionResponse(
                resultado.getEficienciaEspacial(),
                resultado.getCalificacionEficiencia(),
                curva,
                resultado.getDemandaSatisfecha(),
                resultado.getVehiculosRechazados(),
                periodosSaturacion,
                resultado.getPuntuacionGeneral(),
                resultado.getCalificacionTexto());
    }

    private static BigDecimal dosDecimales(BigDecimal valor) {
        return valor.setScale(2, RoundingMode.HALF_UP);
    }

    // Misma regla que ProyectoService (contratos/api-contract.md, sección 1):
    // un proyecto de otro usuario responde 404, no 403.
    private Proyecto obtenerProyectoDelUsuario(Long usuarioId, Long proyectoId) {
        Proyecto proyecto = proyectoRepository.findById(proyectoId)
                .orElseThrow(() -> new NegocioException(HttpStatus.NOT_FOUND, "PROYECTO_NO_ENCONTRADO", "Proyecto no encontrado"));

        if (!proyecto.getUsuarioId().equals(usuarioId)) {
            throw new NegocioException(HttpStatus.NOT_FOUND, "PROYECTO_NO_ENCONTRADO", "Proyecto no encontrado");
        }

        return proyecto;
    }
}
