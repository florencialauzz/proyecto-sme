package sme.simulacion;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.util.ArrayList;
import java.util.List;

public final class Indicadores {
    private static final int CELDAS_POR_PLAZA = 2;

    private static final int CELDAS_ENTRADA_Y_SALIDA = 2;

    private static final double FRACCION_PLAZA_DOBLE_CARGA = 0.80;
    private static final double FRACCION_PLAZA_BORDE = 0.65;

    private static final int PLAZAS_POR_ZONA_BICI_MOTO = 25;

    private static final int CELDAS_RAMPA_POR_CONEXION = 4;

    private static final double UMBRAL_BUENO = 0.90;
    private static final double UMBRAL_REGULAR = 0.70;

    // Pesos provisorios, tienen que sumar 1
    private static final double PESO_EFICIENCIA = 0.50;
    private static final double PESO_DEMANDA = 0.50;
    private static final double PUNTAJE_BUENO = 80;
    private static final double PUNTAJE_REGULAR = 60;

    private Indicadores() {
    }

    public static BigDecimal eficienciaEspacial(GrillaSimulacion grilla) {
        int celdasDePlaza = grilla.getPlazas().size() * CELDAS_POR_PLAZA;
        int celdasTotales = grilla.getFilas() * grilla.getColumnas() * grilla.getCantidadPisos();
        return porcentaje(celdasDePlaza, celdasTotales);
    }

    public static String calificacionEficiencia(GrillaSimulacion grilla) {
        double celdasDePlaza = grilla.getPlazas().size() * CELDAS_POR_PLAZA;
        double fraccionDelTecho = celdasDePlaza / techoCeldasDePlaza(grilla);

        if (fraccionDelTecho >= UMBRAL_BUENO) {
            return "Bueno";
        }
        if (fraccionDelTecho >= UMBRAL_REGULAR) {
            return "Regular";
        }
        return "Deficiente";
    }

    public static double techoCeldasDePlaza(GrillaSimulacion grilla) {
        int pisos = grilla.getCantidadPisos();

        int celdasTotales = grilla.getFilas() * grilla.getColumnas() * pisos;

        int sinEntradaYSalida = celdasTotales - CELDAS_ENTRADA_Y_SALIDA;

        int overheadVertical = 0;
        if (pisos > 1) {
            int conexiones = pisos - 1;
            int celdasRampa = conexiones * CELDAS_RAMPA_POR_CONEXION;
            int celdasEscalera = pisos;
            overheadVertical = celdasEscalera + celdasRampa;
        }
        int utilizablesPrimeraPasada = sinEntradaYSalida - overheadVertical;

        double plazasDisenoBorde = utilizablesPrimeraPasada * FRACCION_PLAZA_BORDE / CELDAS_POR_PLAZA;
        double plazasDisenoCompacto = utilizablesPrimeraPasada * FRACCION_PLAZA_DOBLE_CARGA / CELDAS_POR_PLAZA;
        double plazasEstimadas = (plazasDisenoBorde + plazasDisenoCompacto) / 2;
        int zonasBiciMoto = (int) Math.ceil(plazasEstimadas / PLAZAS_POR_ZONA_BICI_MOTO);
        int utilizables = utilizablesPrimeraPasada - zonasBiciMoto;

        return utilizables * FRACCION_PLAZA_DOBLE_CARGA;
    }

    public static BigDecimal demandaSatisfecha(ResultadoMotor resultado) {
        int llegados = resultado.vehiculosLlegados();
        if (llegados == 0) {
            return porcentaje(1, 1);
        }
        int atendidos = llegados - resultado.vehiculosRechazados();
        return porcentaje(atendidos, llegados);
    }

    public static List<IntervaloSaturacion> periodosSaturacion(List<PuntoCurva> curva, int totalPlazas) {
        List<IntervaloSaturacion> intervalos = new ArrayList<>();
        Integer inicioTramo = null;
        int ultimoMinutoSaturado = 0;

        for (PuntoCurva punto : curva) {
            boolean saturado = punto.cantidadOcupadas() >= totalPlazas;
            if (saturado) {
                if (inicioTramo == null) {
                    inicioTramo = punto.minuto();
                }
                ultimoMinutoSaturado = punto.minuto();
            } else if (inicioTramo != null) {
                intervalos.add(new IntervaloSaturacion(inicioTramo, ultimoMinutoSaturado));
                inicioTramo = null;
            }
        }

        if (inicioTramo != null) {
            intervalos.add(new IntervaloSaturacion(inicioTramo, ultimoMinutoSaturado));
        }

        return intervalos;
    }

    public static BigDecimal puntuacionGeneral(GrillaSimulacion grilla, BigDecimal demandaSatisfecha) {
        double celdasDePlaza = grilla.getPlazas().size() * CELDAS_POR_PLAZA;
        double eficienciaRelativaAlTecho = celdasDePlaza / techoCeldasDePlaza(grilla) * 100;
        if (eficienciaRelativaAlTecho > 100) {
            eficienciaRelativaAlTecho = 100;
        }

        double puntaje = PESO_EFICIENCIA * eficienciaRelativaAlTecho
                + PESO_DEMANDA * demandaSatisfecha.doubleValue();
        return BigDecimal.valueOf(puntaje).setScale(2, RoundingMode.HALF_UP);
    }

    public static String calificacionPuntuacion(BigDecimal puntuacionGeneral) {
        double puntaje = puntuacionGeneral.doubleValue();
        if (puntaje >= PUNTAJE_BUENO) {
            return "Bueno";
        }
        if (puntaje >= PUNTAJE_REGULAR) {
            return "Regular";
        }
        return "Deficiente";
    }

    private static BigDecimal porcentaje(int parte, int total) {
        return BigDecimal.valueOf(parte)
                .multiply(BigDecimal.valueOf(100))
                .divide(BigDecimal.valueOf(total), 2, RoundingMode.HALF_UP);
    }
}
