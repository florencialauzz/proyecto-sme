package sme.simulacion;

import org.junit.jupiter.api.Test;
import sme.entity.Direccion;
import sme.entity.Pieza;
import sme.entity.SentidoVertical;
import sme.entity.TipoPieza;

import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertSame;
import static org.junit.jupiter.api.Assertions.assertTrue;

class MotorSimulacionTest {
    private static final int FILAS = 15;
    private static final int COLUMNAS = 15;

    private static Pieza pieza(int piso, int fila, int columna, TipoPieza tipo) {
        Pieza pieza = new Pieza();
        pieza.setPiso(piso);
        pieza.setFila(fila);
        pieza.setColumna(columna);
        pieza.setTipo(tipo);
        return pieza;
    }

    private static Pieza circulacion(int piso, int fila, int columna, TipoPieza tipo, Direccion direccion) {
        Pieza pieza = pieza(piso, fila, columna, tipo);
        pieza.setDireccion(direccion);
        return pieza;
    }

    private static Pieza plaza(int piso, int fila, int columna, Direccion caraAcceso) {
        Pieza pieza = pieza(piso, fila, columna, TipoPieza.PLAZA);
        pieza.setCaraAcceso(caraAcceso);
        return pieza;
    }

    private static Pieza rampa(int piso, int fila, int columna, Direccion direccion, SentidoVertical sentido) {
        Pieza pieza = circulacion(piso, fila, columna, TipoPieza.RAMPA, direccion);
        pieza.setSentidoVertical(sentido);
        return pieza;
    }

    private static List<Pieza> pasilloRecto(int... columnasDePlaza) {
        List<Pieza> piezas = new ArrayList<>();
        piezas.add(circulacion(0, 7, 0, TipoPieza.ENTRADA, Direccion.ESTE));
        for (int columna = 1; columna <= 13; columna++) {
            piezas.add(circulacion(0, 7, columna, TipoPieza.CALLE, Direccion.ESTE));
        }
        piezas.add(circulacion(0, 7, 14, TipoPieza.SALIDA, Direccion.ESTE));
        for (int columna : columnasDePlaza) {
            piezas.add(plaza(0, 6, columna, Direccion.SUR));
        }
        return piezas;
    }

    @Test
    void asignaPrimeroLaPlazaMasCercanaALaEntrada() {
        GrillaSimulacion grilla = new GrillaSimulacion(1, FILAS, COLUMNAS, pasilloRecto(10, 2, 6));
        MotorSimulacion motor = new MotorSimulacion(grilla);

        List<CeldaSimulacion> orden = motor.getPlazasPorCercania();
        assertEquals(3, orden.size());
        assertEquals(2, orden.get(0).getColumna());
        assertEquals(6, orden.get(1).getColumna());
        assertEquals(10, orden.get(2).getColumna());
    }

    @Test
    void plazaSinCalleEnLaBocaNoSeUsa() {
        List<Pieza> piezas = pasilloRecto(2);
        piezas.add(plaza(0, 4, 5, Direccion.NORTE));
        GrillaSimulacion grilla = new GrillaSimulacion(1, FILAS, COLUMNAS, piezas);

        MotorSimulacion motor = new MotorSimulacion(grilla);

        assertEquals(1, motor.getPlazasPorCercania().size());
    }

    @Test
    void plazaQueCuelgaDeLaEntradaEsLaMasCercana() {
        List<Pieza> piezas = pasilloRecto(2);
        piezas.add(plaza(0, 6, 0, Direccion.SUR));
        GrillaSimulacion grilla = new GrillaSimulacion(1, FILAS, COLUMNAS, piezas);

        MotorSimulacion motor = new MotorSimulacion(grilla);

        assertEquals(2, motor.getPlazasPorCercania().size());
        assertEquals(0, motor.getPlazasPorCercania().get(0).getColumna());
    }

    @Test
    void llegaAUnaPlazaDelPisoDeArribaPorLaRampa() {
        List<Pieza> piezas = new ArrayList<>();
        piezas.add(circulacion(0, 7, 0, TipoPieza.ENTRADA, Direccion.ESTE));
        piezas.add(circulacion(0, 7, 1, TipoPieza.CALLE, Direccion.ESTE));
        piezas.add(rampa(0, 7, 2, Direccion.ESTE, SentidoVertical.SUBE));
        piezas.add(rampa(1, 7, 2, Direccion.ESTE, SentidoVertical.SUBE));
        piezas.add(circulacion(1, 7, 3, TipoPieza.CALLE, Direccion.ESTE));
        piezas.add(circulacion(1, 7, 4, TipoPieza.CALLE, Direccion.ESTE));
        piezas.add(rampa(1, 7, 5, Direccion.ESTE, SentidoVertical.BAJA));
        piezas.add(rampa(0, 7, 5, Direccion.ESTE, SentidoVertical.BAJA));
        piezas.add(circulacion(0, 7, 6, TipoPieza.SALIDA, Direccion.ESTE));
        piezas.add(plaza(1, 6, 3, Direccion.SUR));
        GrillaSimulacion grilla = new GrillaSimulacion(2, FILAS, COLUMNAS, piezas);

        MotorSimulacion motor = new MotorSimulacion(grilla);

        assertEquals(1, motor.getPlazasPorCercania().size());
        assertEquals(1, motor.getPlazasPorCercania().get(0).getPiso());
    }

    @Test
    void plazaDesdeLaQueNoSePuedeSalirNoSeUsa() {
        List<Pieza> piezas = new ArrayList<>();
        piezas.add(circulacion(0, 7, 0, TipoPieza.ENTRADA, Direccion.ESTE));
        piezas.add(circulacion(0, 7, 1, TipoPieza.CALLE, Direccion.ESTE));
        piezas.add(rampa(0, 7, 2, Direccion.ESTE, SentidoVertical.SUBE));
        piezas.add(rampa(1, 7, 2, Direccion.ESTE, SentidoVertical.SUBE));
        piezas.add(circulacion(1, 7, 3, TipoPieza.CALLE, Direccion.ESTE));
        piezas.add(circulacion(1, 7, 4, TipoPieza.CALLE, Direccion.SUR));
        piezas.add(circulacion(0, 8, 0, TipoPieza.SALIDA, Direccion.OESTE));
        piezas.add(plaza(1, 6, 3, Direccion.SUR));
        GrillaSimulacion grilla = new GrillaSimulacion(2, FILAS, COLUMNAS, piezas);

        MotorSimulacion motor = new MotorSimulacion(grilla);

        assertEquals(0, motor.getPlazasPorCercania().size());
    }

    @Test
    void conTreintaPorHoraLlegaUnoCadaDosMinutos() {
        GrillaSimulacion grilla = new GrillaSimulacion(1, FILAS, COLUMNAS, pasilloRecto(2, 4));
        MotorSimulacion motor = new MotorSimulacion(grilla);

        ResultadoMotor resultado = motor.ejecutar(GeneradorDemanda.sinFluctuaciones(30, 45, 10), 10);

        assertEquals(5, resultado.vehiculosLlegados());
        assertEquals(3, resultado.vehiculosRechazados());
        assertEquals(10, resultado.curva().size());
        assertEquals(0, resultado.curva().get(0).cantidadOcupadas());
        assertEquals(1, resultado.curva().get(1).cantidadOcupadas());
        assertEquals(1, resultado.curva().get(2).cantidadOcupadas());
        assertEquals(2, resultado.curva().get(3).cantidadOcupadas());
        assertEquals(2, resultado.curva().get(9).cantidadOcupadas());
    }

    @Test
    void laPlazaSeLiberaAlCumplirseElTiempoDePermanencia() {
        GrillaSimulacion grilla = new GrillaSimulacion(1, FILAS, COLUMNAS, pasilloRecto(2));
        MotorSimulacion motor = new MotorSimulacion(grilla);

        ResultadoMotor resultado = motor.ejecutar(GeneradorDemanda.sinFluctuaciones(60, 1, 30), 30);

        assertEquals(30, resultado.vehiculosLlegados());
        assertEquals(0, resultado.vehiculosRechazados());
    }

    @Test
    void conMasDeSesentaPorHoraLleganVariosEnElMismoMinuto() {
        GrillaSimulacion grilla = new GrillaSimulacion(1, FILAS, COLUMNAS, pasilloRecto(2, 4, 6));
        MotorSimulacion motor = new MotorSimulacion(grilla);

        ResultadoMotor resultado = motor.ejecutar(GeneradorDemanda.sinFluctuaciones(120, 100, 1), 1);

        assertEquals(2, resultado.vehiculosLlegados());
        assertEquals(2, resultado.curva().get(0).cantidadOcupadas());
    }

    @Test
    void cadaVehiculoLiberaLaPlazaSegunSuPropiaPermanencia() {
        GrillaSimulacion grilla = new GrillaSimulacion(1, FILAS, COLUMNAS, pasilloRecto(2, 4));
        MotorSimulacion motor = new MotorSimulacion(grilla);

        List<Vehiculo> vehiculos = List.of(new Vehiculo(0, 3), new Vehiculo(0, 5));
        ResultadoMotor resultado = motor.ejecutar(vehiculos, 6);

        assertEquals(2, resultado.curva().get(2).cantidadOcupadas());
        assertEquals(1, resultado.curva().get(3).cantidadOcupadas());
        assertEquals(0, resultado.curva().get(5).cantidadOcupadas());
    }

    @Test
    void laSaturacionSeMideContraLasPlazasUsables() {
        List<Pieza> piezas = pasilloRecto(2);
        piezas.add(plaza(0, 4, 5, Direccion.NORTE));
        GrillaSimulacion grilla = new GrillaSimulacion(1, FILAS, COLUMNAS, piezas);
        MotorSimulacion motor = new MotorSimulacion(grilla);

        ResultadoMotor resultado = motor.ejecutar(GeneradorDemanda.sinFluctuaciones(60, 100, 4), 4);

        assertEquals(1, resultado.plazasUsables());
        List<IntervaloSaturacion> intervalos = Indicadores.periodosSaturacion(resultado.curva(),
                resultado.plazasUsables());
        assertEquals(List.of(new IntervaloSaturacion(0, 3)), intervalos);
    }

    @Test
    void conFluctuacionesElSorteoSaleSiempreIgual() {
        List<Vehiculo> primera = GeneradorDemanda.conFluctuaciones(30, 120, 600);
        List<Vehiculo> segunda = GeneradorDemanda.conFluctuaciones(30, 120, 600);

        assertEquals(primera, segunda);
    }

    @Test
    void conFluctuacionesSeRespetaLaFrecuenciaEnPromedio() {
        List<Vehiculo> vehiculos = GeneradorDemanda.conFluctuaciones(30, 120, 600);

        assertTrue(vehiculos.size() >= 285 && vehiculos.size() <= 315,
                "llegaron " + vehiculos.size());
    }

    @Test
    void conFluctuacionesLosIntervalosQuedanDentroDelMargen() {
        List<Vehiculo> vehiculos = GeneradorDemanda.conFluctuaciones(6, 120, 600);

        assertTrue(vehiculos.get(0).minutoLlegada() >= 5 && vehiculos.get(0).minutoLlegada() < 15);
        boolean hayIntervalosDistintos = false;
        for (int i = 1; i < vehiculos.size(); i++) {
            int intervalo = vehiculos.get(i).minutoLlegada() - vehiculos.get(i - 1).minutoLlegada();
            assertTrue(intervalo >= 4 && intervalo <= 15, "intervalo de " + intervalo);
            if (intervalo != 10) hayIntervalosDistintos = true;
        }
        assertTrue(hayIntervalosDistintos);
    }

    @Test
    void conFluctuacionesLaPermanenciaQuedaDentroDelMargenYRespetaElPromedio() {
        List<Vehiculo> vehiculos = GeneradorDemanda.conFluctuaciones(30, 120, 600);

        int suma = 0;
        for (Vehiculo vehiculo : vehiculos) {
            assertTrue(vehiculo.tiempoPermanencia() >= 60 && vehiculo.tiempoPermanencia() <= 180,
                    "permanencia de " + vehiculo.tiempoPermanencia());
            suma += vehiculo.tiempoPermanencia();
        }
        double promedio = (double) suma / vehiculos.size();
        assertTrue(promedio >= 114 && promedio <= 126, "promedio de " + promedio);
    }

    @Test
    void sinFluctuacionesTodosSeQuedanLoMismo() {
        List<Vehiculo> vehiculos = GeneradorDemanda.sinFluctuaciones(30, 120, 60);

        assertEquals(30, vehiculos.size());
        for (Vehiculo vehiculo : vehiculos) {
            assertEquals(120, vehiculo.tiempoPermanencia());
        }
    }

    private static GrillaSimulacion grillaConPlazas(int pisos, int cantidadPlazas) {
        List<Pieza> piezas = new ArrayList<>();
        int celdasPorPiso = FILAS * COLUMNAS;
        for (int i = 0; i < cantidadPlazas; i++) {
            int piso = i / celdasPorPiso;
            int fila = (i % celdasPorPiso) / COLUMNAS;
            int columna = i % COLUMNAS;
            piezas.add(plaza(piso, fila, columna, Direccion.SUR));
        }
        return new GrillaSimulacion(pisos, FILAS, COLUMNAS, piezas);
    }

    @Test
    void elTechoDeUnPisoUsaElFactorDeAprovechamiento() {
        assertEquals(155.49, Indicadores.techoCeldasDePlaza(grillaConPlazas(1, 0)), 0.0001);
    }

    @Test
    void elTechoDeDosPisosDescuentaRampasYEscaleras() {
        assertEquals(308.85, Indicadores.techoCeldasDePlaza(grillaConPlazas(2, 0)), 0.0001);
    }

    @Test
    void elTechoDeTresPisosDescuentaRampasYEscaleras() {
        assertEquals(462.92, Indicadores.techoCeldasDePlaza(grillaConPlazas(3, 0)), 0.0001);
    }

    @Test
    void eficienciaEsLaCercaniaAlTechoTeorico() {
        BigDecimal eficiencia = Indicadores.eficienciaEspacial(grillaConPlazas(1, 69));

        assertEquals(new BigDecimal("88.75"), eficiencia);
        assertEquals("Bueno", Indicadores.calificacionEficiencia(eficiencia));
    }

    @Test
    void eficienciaPorDebajoDeSetentaEsDeficiente() {
        BigDecimal eficiencia = Indicadores.eficienciaEspacial(grillaConPlazas(1, 50));

        assertEquals(new BigDecimal("64.31"), eficiencia);
        assertEquals("Deficiente", Indicadores.calificacionEficiencia(eficiencia));
    }

    @Test
    void eficienciaNoPasaDeCien() {
        assertEquals(new BigDecimal("100.00"), Indicadores.eficienciaEspacial(grillaConPlazas(1, 78)));
    }

    @Test
    void eficienciaDeDosYTresPisos() {
        assertEquals(new BigDecimal("99.72"), Indicadores.eficienciaEspacial(grillaConPlazas(2, 154)));
        assertEquals(new BigDecimal("99.80"), Indicadores.eficienciaEspacial(grillaConPlazas(3, 231)));
    }

    @Test
    void calificacionDeLaEficienciaSegunLosCortes() {
        assertEquals("Bueno", Indicadores.calificacionEficiencia(new BigDecimal("85.00")));
        assertEquals("Regular", Indicadores.calificacionEficiencia(new BigDecimal("84.99")));
        assertEquals("Regular", Indicadores.calificacionEficiencia(new BigDecimal("70.00")));
        assertEquals("Deficiente", Indicadores.calificacionEficiencia(new BigDecimal("69.99")));
    }

    @Test
    void demandaSatisfechaSinLlegadasEsCien() {
        ResultadoMotor resultado = new ResultadoMotor(List.of(), 0, 0, 0);

        assertEquals(new BigDecimal("100.00"), Indicadores.demandaSatisfecha(resultado));
    }

    @Test
    void demandaSatisfechaEsElPorcentajeDeAtendidos() {
        ResultadoMotor resultado = new ResultadoMotor(List.of(), 5, 3, 0);

        assertEquals(new BigDecimal("40.00"), Indicadores.demandaSatisfecha(resultado));
    }

    @Test
    void periodosDeSaturacionSonLosTramosConTodasLasPlazasOcupadas() {
        List<PuntoCurva> curva = List.of(
                new PuntoCurva(0, 0),
                new PuntoCurva(1, 1),
                new PuntoCurva(2, 2),
                new PuntoCurva(3, 2),
                new PuntoCurva(4, 1),
                new PuntoCurva(5, 2));

        List<IntervaloSaturacion> intervalos = Indicadores.periodosSaturacion(curva, 2);

        assertEquals(List.of(new IntervaloSaturacion(2, 3), new IntervaloSaturacion(5, 5)), intervalos);
    }

    @Test
    void sinSaturacionLaListaQuedaVacia() {
        List<PuntoCurva> curva = List.of(new PuntoCurva(0, 0), new PuntoCurva(1, 1));

        assertTrue(Indicadores.periodosSaturacion(curva, 2).isEmpty());
    }

    @Test
    void puntuacionGeneralPonderaEficienciaYDemandaPorIgual() {
        BigDecimal puntuacion = Indicadores.puntuacionGeneral(new BigDecimal("80.00"), new BigDecimal("90.00"));

        assertEquals(new BigDecimal("85.00"), puntuacion);
        assertEquals("Bueno", Indicadores.calificacionPuntuacion(puntuacion));
    }

    @Test
    void calificacionDeLaPuntuacionSegunLosCortes() {
        assertEquals("Bueno", Indicadores.calificacionPuntuacion(new BigDecimal("85.00")));
        assertEquals("Regular", Indicadores.calificacionPuntuacion(new BigDecimal("84.99")));
        assertEquals("Regular", Indicadores.calificacionPuntuacion(new BigDecimal("70.00")));
        assertEquals("Deficiente", Indicadores.calificacionPuntuacion(new BigDecimal("69.99")));
    }

    private static ResultadoMotor simulacionConRechazos() {
        GrillaSimulacion grilla = new GrillaSimulacion(1, FILAS, COLUMNAS, pasilloRecto(2, 4, 6));
        MotorSimulacion motor = new MotorSimulacion(grilla);
        return motor.ejecutar(GeneradorDemanda.conFluctuaciones(30, 120, 600), 600);
    }

    @Test
    void hayUnEventoPorVehiculoLlegado() {
        ResultadoMotor resultado = simulacionConRechazos();

        assertEquals(resultado.vehiculosLlegados(), resultado.eventos().size());
    }

    @Test
    void losEventosSinPlazaSonLosRechazados() {
        ResultadoMotor resultado = simulacionConRechazos();

        int sinPlaza = 0;
        for (EventoVehiculo evento : resultado.eventos()) {
            if (evento.indicePlaza() == EventoVehiculo.SIN_PLAZA) {
                sinPlaza++;
                assertEquals(evento.minutoLlegada(), evento.minutoSalida());
            }
        }

        assertTrue(resultado.vehiculosRechazados() > 0, "el caso tiene que tener rechazos");
        assertEquals(resultado.vehiculosRechazados(), sinPlaza);
    }

    @Test
    void laOcupacionReconstruidaDesdeLosEventosCoincideConLaCurva() {
        ResultadoMotor resultado = simulacionConRechazos();

        for (PuntoCurva punto : resultado.curva()) {
            int minuto = punto.minuto();
            int estacionados = 0;
            for (EventoVehiculo evento : resultado.eventos()) {
                boolean tienePlaza = evento.indicePlaza() != EventoVehiculo.SIN_PLAZA;
                if (tienePlaza && evento.minutoLlegada() <= minuto && minuto < evento.minutoSalida()) {
                    estacionados++;
                }
            }
            assertEquals(punto.cantidadOcupadas(), estacionados, "minuto " + minuto);
        }
    }

    @Test
    void cadaEventoRegistraLaPlazaQueAsignoElMotor() {
        GrillaSimulacion grilla = new GrillaSimulacion(1, FILAS, COLUMNAS, pasilloRecto(2, 4));
        MotorSimulacion motor = new MotorSimulacion(grilla);

        ResultadoMotor resultado = motor.ejecutar(GeneradorDemanda.sinFluctuaciones(30, 45, 10), 10);

        assertEquals(List.of(
                new EventoVehiculo(1, 46, 0),
                new EventoVehiculo(3, 48, 1),
                new EventoVehiculo(5, 5, EventoVehiculo.SIN_PLAZA),
                new EventoVehiculo(7, 7, EventoVehiculo.SIN_PLAZA),
                new EventoVehiculo(9, 9, EventoVehiculo.SIN_PLAZA)), resultado.eventos());
    }

    private static List<Pieza> disenoConPlazaArriba() {
        List<Pieza> piezas = new ArrayList<>();
        piezas.add(circulacion(0, 7, 0, TipoPieza.ENTRADA, Direccion.ESTE));
        piezas.add(circulacion(0, 7, 1, TipoPieza.CALLE, Direccion.ESTE));
        piezas.add(rampa(0, 7, 2, Direccion.ESTE, SentidoVertical.SUBE));
        piezas.add(rampa(1, 7, 2, Direccion.ESTE, SentidoVertical.SUBE));
        piezas.add(circulacion(1, 7, 3, TipoPieza.CALLE, Direccion.ESTE));
        piezas.add(circulacion(1, 7, 4, TipoPieza.CALLE, Direccion.ESTE));
        piezas.add(rampa(1, 7, 5, Direccion.ESTE, SentidoVertical.BAJA));
        piezas.add(rampa(0, 7, 5, Direccion.ESTE, SentidoVertical.BAJA));
        piezas.add(circulacion(0, 7, 6, TipoPieza.SALIDA, Direccion.ESTE));
        piezas.add(plaza(1, 6, 3, Direccion.SUR));
        return piezas;
    }

    private static void assertCadaPasoEsUnaArista(GrafoCirculacion grafo, List<CeldaSimulacion> camino) {
        for (int i = 1; i < camino.size(); i++) {
            CeldaSimulacion anterior = camino.get(i - 1);
            CeldaSimulacion actual = camino.get(i);
            assertTrue(grafo.sucesores(anterior).contains(actual),
                    "paso " + i + ": (" + anterior.getPiso() + "," + anterior.getFila() + "," + anterior.getColumna()
                            + ") → (" + actual.getPiso() + "," + actual.getFila() + "," + actual.getColumna() + ")");
        }
    }

    @Test
    void elLargoDelCaminoDeEntradaEsLaDistanciaQueUsaElMotor() {
        GrillaSimulacion grilla = new GrillaSimulacion(1, FILAS, COLUMNAS, pasilloRecto(2, 6, 10));
        MotorSimulacion motor = new MotorSimulacion(grilla);
        GrafoCirculacion grafo = new GrafoCirculacion(grilla);
        CeldaSimulacion entrada = grilla.getEntradas().get(0);
        Map<CeldaSimulacion, Integer> distancias = grafo.distanciasDesde(entrada);

        List<CaminosDePlaza> caminos = motor.calcularCaminosDePlazas();

        assertEquals(motor.getPlazasPorCercania().size(), caminos.size());
        for (int i = 0; i < caminos.size(); i++) {
            CaminosDePlaza caminosDePlaza = caminos.get(i);
            CeldaSimulacion plaza = caminosDePlaza.plaza();
            CeldaSimulacion boca = grilla.obtenerVecina(plaza, plaza.getCaraAcceso());
            List<CeldaSimulacion> caminoEntrada = caminosDePlaza.caminoEntrada();

            assertSame(motor.getPlazasPorCercania().get(i), plaza);
            assertSame(entrada, caminoEntrada.get(0));
            assertSame(boca, caminoEntrada.get(caminoEntrada.size() - 1));
            assertEquals(distancias.get(boca).intValue(), caminoEntrada.size() - 1);
        }
    }

    @Test
    void cadaPasoDeLosCaminosEsUnaAristaDelGrafo() {
        GrillaSimulacion grilla = new GrillaSimulacion(1, FILAS, COLUMNAS, pasilloRecto(2, 6, 10));
        MotorSimulacion motor = new MotorSimulacion(grilla);
        GrafoCirculacion grafo = new GrafoCirculacion(grilla);
        CeldaSimulacion salida = grilla.getSalidas().get(0);

        for (CaminosDePlaza caminosDePlaza : motor.calcularCaminosDePlazas()) {
            List<CeldaSimulacion> caminoEntrada = caminosDePlaza.caminoEntrada();
            List<CeldaSimulacion> caminoSalida = caminosDePlaza.caminoSalida();

            assertCadaPasoEsUnaArista(grafo, caminoEntrada);
            assertCadaPasoEsUnaArista(grafo, caminoSalida);
            assertSame(caminoEntrada.get(caminoEntrada.size() - 1), caminoSalida.get(0));
            assertSame(salida, caminoSalida.get(caminoSalida.size() - 1));
        }
    }

    @Test
    void conRampaElCaminoCambiaDePisoPorElParDeRampa() {
        GrillaSimulacion grilla = new GrillaSimulacion(2, FILAS, COLUMNAS, disenoConPlazaArriba());
        MotorSimulacion motor = new MotorSimulacion(grilla);

        List<CaminosDePlaza> caminos = motor.calcularCaminosDePlazas();

        assertEquals(1, caminos.size());
        assertEquals(List.of(
                grilla.obtenerCelda(0, 7, 0),
                grilla.obtenerCelda(0, 7, 1),
                grilla.obtenerCelda(0, 7, 2),
                grilla.obtenerCelda(1, 7, 2),
                grilla.obtenerCelda(1, 7, 3)), caminos.get(0).caminoEntrada());
        assertEquals(List.of(
                grilla.obtenerCelda(1, 7, 3),
                grilla.obtenerCelda(1, 7, 4),
                grilla.obtenerCelda(1, 7, 5),
                grilla.obtenerCelda(0, 7, 5),
                grilla.obtenerCelda(0, 7, 6)), caminos.get(0).caminoSalida());
    }

    @Test
    void elRecorridoDeLosRechazadosPasaPorLaPlazaMasLejana() {
        GrillaSimulacion grilla = new GrillaSimulacion(1, FILAS, COLUMNAS, pasilloRecto(2, 6, 10));
        MotorSimulacion motor = new MotorSimulacion(grilla);

        int indice = motor.indicePlazaRecorridoRechazados();

        assertEquals(2, indice);
        assertEquals(10, motor.calcularCaminosDePlazas().get(indice).plaza().getColumna());
    }

    @Test
    void sinPlazasUsablesNoHayRecorridoDeRechazados() {
        List<Pieza> piezas = new ArrayList<>();
        piezas.add(circulacion(0, 7, 0, TipoPieza.ENTRADA, Direccion.ESTE));
        piezas.add(circulacion(0, 7, 1, TipoPieza.CALLE, Direccion.ESTE));
        piezas.add(rampa(0, 7, 2, Direccion.ESTE, SentidoVertical.SUBE));
        piezas.add(rampa(1, 7, 2, Direccion.ESTE, SentidoVertical.SUBE));
        piezas.add(circulacion(1, 7, 3, TipoPieza.CALLE, Direccion.ESTE));
        piezas.add(circulacion(1, 7, 4, TipoPieza.CALLE, Direccion.SUR));
        piezas.add(circulacion(0, 8, 0, TipoPieza.SALIDA, Direccion.OESTE));
        piezas.add(plaza(1, 6, 3, Direccion.SUR));
        GrillaSimulacion grilla = new GrillaSimulacion(2, FILAS, COLUMNAS, piezas);
        MotorSimulacion motor = new MotorSimulacion(grilla);

        assertEquals(EventoVehiculo.SIN_PLAZA, motor.indicePlazaRecorridoRechazados());
        assertTrue(motor.calcularCaminosDePlazas().isEmpty());
    }

    @Test
    void caminoHastaUnDestinoInalcanzableEsVacio() {
        GrillaSimulacion grilla = new GrillaSimulacion(1, FILAS, COLUMNAS, pasilloRecto());
        GrafoCirculacion grafo = new GrafoCirculacion(grilla);

        List<CeldaSimulacion> camino = grafo.caminoHasta(grilla.getSalidas().get(0), grilla.getEntradas().get(0));

        assertTrue(camino.isEmpty());
    }
}
