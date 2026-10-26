package sme.simulacion;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.util.ArrayList;
import java.util.List;

public final class Indicadores {
    private static final int CELDAS_POR_PLAZA = 2;

    private static final int CELDAS_ENTRADA_Y_SALIDA = 2;

    private static final double FACTOR_APROVECHAMIENTO = 0.71;

    private static final int PLAZAS_POR_ZONA_BICI_MOTO = 25;

    private static final int CELDAS_RAMPA_POR_CONEXION = 4;

    private static final double EFICIENCIA_MAXIMA = 100;

    private static final double PESO_EFICIENCIA = 0.50;
    private static final double PESO_DEMANDA = 0.50;

    private static final double UMBRAL_BUENO = 85;
    private static final double UMBRAL_REGULAR = 70;

    private Indicadores() {
    }

    public static BigDecimal eficienciaEspacial(GrillaSimulacion grilla) {
        double celdasDePlaza = grilla.getPlazas().size() * CELDAS_POR_PLAZA;
        double eficiencia = celdasDePlaza / techoCeldasDePlaza(grilla) * 100;
        if (eficiencia > EFICIENCIA_MAXIMA) {
            eficiencia = EFICIENCIA_MAXIMA;
        }
        return BigDecimal.valueOf(eficiencia).setScale(2, RoundingMode.HALF_UP);
    }

    public static String calificacionEficiencia(BigDecimal eficienciaEspacial) {
        return calificar(eficienciaEspacial.doubleValue());
    }

    public static double techoCeldasDePlaza(GrillaSimulacion grilla) {
        int pisos = grilla.getCantidadPisos();

        int celdasTotales = grilla.getFilas() * grilla.getColumnas() * pisos;

        int celdasFijas = CELDAS_ENTRADA_Y_SALIDA;
        if (pisos > 1) {
            int celdasEscalera = pisos;
            int celdasRampa = CELDAS_RAMPA_POR_CONEXION * (pisos - 1);
            celdasFijas = celdasFijas + celdasEscalera + celdasRampa;
        }

        int celdasUtiles = celdasTotales - celdasFijas;

        double plazasEstimadas = celdasUtiles * FACTOR_APROVECHAMIENTO / CELDAS_POR_PLAZA;
        int zonasBiciMoto = (int) Math.ceil(plazasEstimadas / PLAZAS_POR_ZONA_BICI_MOTO);
        celdasUtiles = celdasUtiles - zonasBiciMoto;

        return celdasUtiles * FACTOR_APROVECHAMIENTO;
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

    public static BigDecimal puntuacionGeneral(BigDecimal eficienciaEspacial, BigDecimal demandaSatisfecha) {
        double puntaje = PESO_EFICIENCIA * eficienciaEspacial.doubleValue()
                + PESO_DEMANDA * demandaSatisfecha.doubleValue();
        return BigDecimal.valueOf(puntaje).setScale(2, RoundingMode.HALF_UP);
    }

    public static String calificacionPuntuacion(BigDecimal puntuacionGeneral) {
        return calificar(puntuacionGeneral.doubleValue());
    }

    private static String calificar(double valor) {
        if (valor >= UMBRAL_BUENO) {
            return "Bueno";
        }
        if (valor >= UMBRAL_REGULAR) {
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
