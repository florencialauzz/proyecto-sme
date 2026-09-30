package sme.service;

import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import sme.dto.EjecutarSimulacionResponse;
import sme.dto.PeriodoSaturacionResponse;
import sme.dto.PuntoOcupacionResponse;
import sme.entity.Pieza;
import sme.entity.Proyecto;
import sme.exception.NegocioException;
import sme.repository.PiezaRepository;
import sme.repository.ProyectoRepository;
import sme.simulacion.GrillaSimulacion;
import sme.simulacion.Indicadores;
import sme.simulacion.IntervaloSaturacion;
import sme.simulacion.MotorSimulacion;
import sme.simulacion.ResultadoMotor;

import java.math.BigDecimal;
import java.time.Duration;
import java.util.List;

@Service
public class SimulacionService {

    private final ProyectoRepository proyectoRepository;
    private final PiezaRepository piezaRepository;

    public SimulacionService(ProyectoRepository proyectoRepository, PiezaRepository piezaRepository) {
        this.proyectoRepository = proyectoRepository;
        this.piezaRepository = piezaRepository;
    }

    // RF-26: simula sobre la grilla guardada y devuelve el resultado sin
    // persistir nada — el usuario puede ejecutar varias veces antes de
    // decidir guardar (RF-23, Iteración 4).
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

        MotorSimulacion motor = new MotorSimulacion(grilla);
        ResultadoMotor resultado = motor.ejecutar(proyecto.getFrecuenciaIngreso(), proyecto.getTiempoPermanencia(),
                duracionMinutos);

        List<PuntoOcupacionResponse> curva = resultado.curva().stream()
                .map(punto -> new PuntoOcupacionResponse(punto.minuto(), punto.cantidadOcupadas()))
                .toList();

        List<IntervaloSaturacion> intervalos = Indicadores.periodosSaturacion(resultado.curva(),
                grilla.getPlazas().size());
        List<PeriodoSaturacionResponse> periodosSaturacion = intervalos.stream()
                .map(intervalo -> new PeriodoSaturacionResponse(intervalo.inicio(), intervalo.fin()))
                .toList();

        BigDecimal puntuacionGeneral = null;
        String calificacionTexto = null;

        return new EjecutarSimulacionResponse(
                Indicadores.eficienciaEspacial(grilla),
                Indicadores.calificacionEficiencia(grilla),
                curva,
                Indicadores.demandaSatisfecha(resultado),
                resultado.vehiculosRechazados(),
                periodosSaturacion,
                puntuacionGeneral,
                calificacionTexto);
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
