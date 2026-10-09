package sme.service;

import org.junit.jupiter.api.Test;
import sme.dto.EjecutarSimulacionResponse;
import sme.dto.EventoVehiculoResponse;
import sme.dto.GuardarSimulacionRequest;
import sme.dto.PlazaReproduccionResponse;
import sme.dto.ReproduccionResponse;
import sme.dto.ResultadoSimulacionResponse;
import sme.entity.Direccion;
import sme.entity.Pieza;
import sme.entity.Proyecto;
import sme.entity.TipoPieza;
import sme.repository.PeriodoSaturacionRepository;
import sme.repository.PiezaRepository;
import sme.repository.ProyectoRepository;
import sme.repository.PuntoOcupacionRepository;
import sme.repository.ResultadoSimulacionRepository;

import java.lang.reflect.RecordComponent;
import java.time.LocalTime;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

// Tests de la respuesta de /simulacion/ejecutar sin base de datos: los
// repositorios son mocks que devuelven un proyecto y sus piezas armados a
// mano. El cálculo en sí lo cubre MotorSimulacionTest; acá se verifica el
// armado de la respuesta (animacion/plan.md, etapa 3).
class SimulacionServiceTest {

    private static final long USUARIO_ID = 1L;
    private static final long PROYECTO_ID = 10L;

    private static Pieza pieza(int fila, int columna, TipoPieza tipo, Direccion direccion, Direccion caraAcceso) {
        Pieza pieza = new Pieza();
        pieza.setPiso(0);
        pieza.setFila(fila);
        pieza.setColumna(columna);
        pieza.setTipo(tipo);
        pieza.setDireccion(direccion);
        pieza.setCaraAcceso(caraAcceso);
        return pieza;
    }

    // Pasillo recto en la fila 7 (Entrada en la columna 0, Salida en la 14)
    // con 3 plazas arriba, de 8:00 a 12:00 a 30/h con 2 horas de
    // permanencia: hay rechazos.
    private static SimulacionService servicioConPasilloDeTresPlazas() {
        Proyecto proyecto = new Proyecto();
        proyecto.setId(PROYECTO_ID);
        proyecto.setUsuarioId(USUARIO_ID);
        proyecto.setCantidadPisos(1);
        proyecto.setFrecuenciaIngreso(30);
        proyecto.setTiempoPermanencia(120);
        proyecto.setHoraInicioSimulacion(LocalTime.of(8, 0));
        proyecto.setHoraFinSimulacion(LocalTime.of(12, 0));
        proyecto.setConFluctuaciones(true);

        List<Pieza> piezas = new ArrayList<>();
        piezas.add(pieza(7, 0, TipoPieza.ENTRADA, Direccion.ESTE, null));
        for (int columna = 1; columna <= 13; columna++) {
            piezas.add(pieza(7, columna, TipoPieza.CALLE, Direccion.ESTE, null));
        }
        piezas.add(pieza(7, 14, TipoPieza.SALIDA, Direccion.ESTE, null));
        piezas.add(pieza(6, 2, TipoPieza.PLAZA, null, Direccion.SUR));
        piezas.add(pieza(6, 6, TipoPieza.PLAZA, null, Direccion.SUR));
        piezas.add(pieza(6, 10, TipoPieza.PLAZA, null, Direccion.SUR));

        ProyectoRepository proyectoRepository = mock(ProyectoRepository.class);
        PiezaRepository piezaRepository = mock(PiezaRepository.class);
        when(proyectoRepository.findById(PROYECTO_ID)).thenReturn(Optional.of(proyecto));
        when(piezaRepository.findByProyectoId(PROYECTO_ID)).thenReturn(piezas);

        return new SimulacionService(proyectoRepository, piezaRepository,
                mock(ResultadoSimulacionRepository.class),
                mock(PuntoOcupacionRepository.class),
                mock(PeriodoSaturacionRepository.class));
    }

    private static List<String> nombresDeCampos(Class<? extends Record> clase) {
        List<String> nombres = new ArrayList<>();
        for (RecordComponent componente : clase.getRecordComponents()) {
            nombres.add(componente.getName());
        }
        return nombres;
    }

    @Test
    void ejecutarDevuelveLaReproduccionDeLaMismaCorrida() {
        SimulacionService servicio = servicioConPasilloDeTresPlazas();

        EjecutarSimulacionResponse respuesta = servicio.ejecutar(USUARIO_ID, PROYECTO_ID);
        ReproduccionResponse reproduccion = respuesta.reproduccion();

        // Plazas en el orden del motor, con sus caminos desde la Entrada
        // (0) y hasta la Salida (14).
        assertEquals(3, reproduccion.plazas().size());
        assertEquals(List.of(2, 6, 10), reproduccion.plazas().stream().map(PlazaReproduccionResponse::columna).toList());
        for (PlazaReproduccionResponse plaza : reproduccion.plazas()) {
            assertEquals(0, plaza.caminoEntrada().get(0).columna());
            assertEquals(plaza.columna(), plaza.caminoEntrada().get(plaza.caminoEntrada().size() - 1).columna());
            assertEquals(plaza.columna(), plaza.caminoSalida().get(0).columna());
            assertEquals(14, plaza.caminoSalida().get(plaza.caminoSalida().size() - 1).columna());
        }
        assertEquals(2, reproduccion.indicePlazaRecorridoRechazados());

        // Los eventos son de la misma corrida que los indicadores.
        int rechazados = 0;
        for (EventoVehiculoResponse evento : reproduccion.eventos()) {
            if (evento.indicePlaza() == -1) rechazados++;
        }
        assertTrue(respuesta.vehiculosRechazados() > 0, "el caso tiene que tener rechazos");
        assertEquals(respuesta.vehiculosRechazados().intValue(), rechazados);
    }

    @Test
    void guardarYObtenerGuardadoConservanLosOchoCamposSinReproduccion() {
        List<String> ochoCampos = List.of(
                "eficienciaEspacial", "calificacionEficiencia", "curvaOcupacion", "demandaSatisfecha",
                "vehiculosRechazados", "periodosSaturacion", "puntuacionGeneral", "calificacionTexto");

        assertEquals(ochoCampos, nombresDeCampos(GuardarSimulacionRequest.class));
        assertEquals(ochoCampos, nombresDeCampos(ResultadoSimulacionResponse.class));

        List<String> camposDeEjecutar = new ArrayList<>(ochoCampos);
        camposDeEjecutar.add("reproduccion");
        assertEquals(camposDeEjecutar, nombresDeCampos(EjecutarSimulacionResponse.class));
    }
}
