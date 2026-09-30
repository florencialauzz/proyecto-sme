package sme.simulacion;

import org.junit.jupiter.api.Test;
import sme.entity.Direccion;
import sme.entity.Pieza;
import sme.entity.SentidoVertical;
import sme.entity.TipoPieza;

import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.List;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

// Tests del motor sin Spring ni base de datos: la grilla se arma a mano con
// las mismas filas de Pieza que guardaría PUT /grilla.
class MotorSimulacionTest {

    private static final int FILAS = 15;
    private static final int COLUMNAS = 15;

    // --- Armado de grillas ---

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

    // Un pasillo recto en la fila 7 de planta baja: Entrada en la columna 0,
    // calles hacia el este y Salida en la 14. Las plazas se cuelgan arriba
    // del pasillo, con la boca al sur.
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

    // --- Búsqueda de plaza ---

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
        // Boca al norte, contra una celda vacía.
        piezas.add(plaza(0, 4, 5, Direccion.NORTE));
        GrillaSimulacion grilla = new GrillaSimulacion(1, FILAS, COLUMNAS, piezas);

        MotorSimulacion motor = new MotorSimulacion(grilla);

        assertEquals(1, motor.getPlazasPorCercania().size());
    }

    @Test
    void plazaQueCuelgaDeLaEntradaEsLaMasCercana() {
        // La celda de Entrada es donde arranca el recorrido, así que una
        // plaza con la boca sobre ella es alcanzable — igual que en
        // ValidadorDiseno de Unity — y queda primera.
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
        // Planta baja: Entrada → calle → rampa que sube.
        piezas.add(circulacion(0, 7, 0, TipoPieza.ENTRADA, Direccion.ESTE));
        piezas.add(circulacion(0, 7, 1, TipoPieza.CALLE, Direccion.ESTE));
        piezas.add(rampa(0, 7, 2, Direccion.ESTE, SentidoVertical.SUBE));
        piezas.add(rampa(1, 7, 2, Direccion.ESTE, SentidoVertical.SUBE));
        // Piso 1: calles → rampa que baja.
        piezas.add(circulacion(1, 7, 3, TipoPieza.CALLE, Direccion.ESTE));
        piezas.add(circulacion(1, 7, 4, TipoPieza.CALLE, Direccion.ESTE));
        piezas.add(rampa(1, 7, 5, Direccion.ESTE, SentidoVertical.BAJA));
        piezas.add(rampa(0, 7, 5, Direccion.ESTE, SentidoVertical.BAJA));
        // Planta baja: Salida después de la rampa que baja.
        piezas.add(circulacion(0, 7, 6, TipoPieza.SALIDA, Direccion.ESTE));
        // La única plaza está en el piso 1.
        piezas.add(plaza(1, 6, 3, Direccion.SUR));
        GrillaSimulacion grilla = new GrillaSimulacion(2, FILAS, COLUMNAS, piezas);

        MotorSimulacion motor = new MotorSimulacion(grilla);

        assertEquals(1, motor.getPlazasPorCercania().size());
        assertEquals(1, motor.getPlazasPorCercania().get(0).getPiso());
    }

    @Test
    void plazaDesdeLaQueNoSePuedeSalirNoSeUsa() {
        // Rampa que sube sin rampa que baja: el auto llega arriba y queda
        // atrapado, así que la plaza de arriba no se usa.
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

    // --- Llegadas, permanencia y rechazos ---

    @Test
    void conTreintaPorHoraLlegaUnoCadaDosMinutos() {
        GrillaSimulacion grilla = new GrillaSimulacion(1, FILAS, COLUMNAS, pasilloRecto(2, 4));
        MotorSimulacion motor = new MotorSimulacion(grilla);

        // Llegadas en los minutos 1, 3, 5, 7 y 9; solo hay 2 plazas y nadie
        // se va antes del minuto 46.
        ResultadoMotor resultado = motor.ejecutar(30, 45, 10);

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

        // Uno por minuto, cada uno se queda 1 minuto: el que llega libera la
        // plaza del anterior justo a tiempo, nadie es rechazado.
        ResultadoMotor resultado = motor.ejecutar(60, 1, 30);

        assertEquals(30, resultado.vehiculosLlegados());
        assertEquals(0, resultado.vehiculosRechazados());
    }

    @Test
    void conMasDeSesentaPorHoraLleganVariosEnElMismoMinuto() {
        GrillaSimulacion grilla = new GrillaSimulacion(1, FILAS, COLUMNAS, pasilloRecto(2, 4, 6));
        MotorSimulacion motor = new MotorSimulacion(grilla);

        ResultadoMotor resultado = motor.ejecutar(120, 100, 1);

        assertEquals(2, resultado.vehiculosLlegados());
        assertEquals(2, resultado.curva().get(0).cantidadOcupadas());
    }

    // --- Indicadores ---

    @Test
    void elTechoDeUnaGrillaDeQuinceDeUnPisoEsElDelDocumento() {
        GrillaSimulacion grilla = new GrillaSimulacion(1, FILAS, COLUMNAS, pasilloRecto());

        assertEquals(175.2, Indicadores.techoCeldasDePlaza(grilla), 0.0001);
    }

    @Test
    void elTechoDescuentaRampasYEscaleraConVariosPisos() {
        GrillaSimulacion grilla = new GrillaSimulacion(3, FILAS, COLUMNAS, pasilloRecto());

        // 675 - 2 - V(3)=11 = 662; plazas estimadas (662*0.65/2 + 662*0.8/2)/2
        // = 239.9 → 10 zonas de bici/moto; (662 - 10) * 0.8 = 521.6
        assertEquals(521.6, Indicadores.techoCeldasDePlaza(grilla), 0.0001);
    }

    @Test
    void eficienciaEsElPorcentajeDeCeldasDePlaza() {
        GrillaSimulacion grilla = new GrillaSimulacion(1, FILAS, COLUMNAS, pasilloRecto(2, 4));

        // 2 plazas = 4 celdas sobre 225.
        assertEquals(new BigDecimal("1.78"), Indicadores.eficienciaEspacial(grilla));
        assertEquals("Deficiente", Indicadores.calificacionEficiencia(grilla));
    }

    @Test
    void demandaSatisfechaSinLlegadasEsCien() {
        ResultadoMotor resultado = new ResultadoMotor(List.of(), 0, 0);

        assertEquals(new BigDecimal("100.00"), Indicadores.demandaSatisfecha(resultado));
    }

    @Test
    void demandaSatisfechaEsElPorcentajeDeAtendidos() {
        ResultadoMotor resultado = new ResultadoMotor(List.of(), 5, 3);

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
}
