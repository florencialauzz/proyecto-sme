package sme.simulacion;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.util.ArrayList;
import java.util.List;

// Los indicadores que se calculan a partir de la grilla y de lo que registró
// el motor. Se calculan todos en la misma corrida (arquitectura/decisiones.md);
// en Iteración 3 Unity muestra solo la eficiencia y la curva.
public final class Indicadores {

    // Cada plaza ocupa 2 celdas (ancla + fondo) y se guarda como una sola fila.
    private static final int CELDAS_POR_PLAZA = 2;

    // --- Techo teórico (SME_calculo_eficiencia_espacial.pdf, en el Drive del equipo) ---

    // Entrada y Salida: exactamente una de cada una en todo el proyecto.
    private static final int CELDAS_ENTRADA_Y_SALIDA = 2;

    // Una calle con plazas de los dos lados da 4 celdas de plaza por cada
    // celda de calle (80%); contra el borde, 2 por cada una (~65%). El techo
    // toma el extremo alcanzable, 80%. El 65% solo se usa para estimar la
    // cantidad de plazas con la que se calcula el mínimo de bicis/motos.
    private static final double FRACCION_PLAZA_DOBLE_CARGA = 0.80;
    private static final double FRACCION_PLAZA_BORDE = 0.65;

    // Una ZonaBicicletasMotos (5 espacios) cada 25 plazas de auto:
    // editor/validaciones.md, 7 (1 espacio cada 5 plazas).
    private static final int PLAZAS_POR_ZONA_BICI_MOTO = 25;

    // Celdas de rampa por cada par de pisos consecutivos: una que sube y una
    // que baja, cada una en los 2 pisos que conecta.
    private static final int CELDAS_RAMPA_POR_CONEXION = 4;

    // Cortes de calificación, como fracción del techo.
    private static final double UMBRAL_BUENO = 0.90;
    private static final double UMBRAL_REGULAR = 0.70;

    private Indicadores() {
    }

    // --- RF-15: eficiencia espacial ---

    // Porcentaje de celdas de la grilla ocupadas por plazas respecto al total
    // de celdas de todos los pisos. Solo cuentan las Plazas: una
    // ZonaBicicletasMotos no suma, si sumara un diseño podría inflar su
    // eficiencia poniendo zonas de más (editor/catalogo-piezas.md).
    public static BigDecimal eficienciaEspacial(GrillaSimulacion grilla) {
        int celdasDePlaza = grilla.getPlazas().size() * CELDAS_POR_PLAZA;
        int celdasTotales = grilla.getFilas() * grilla.getColumnas() * grilla.getCantidadPisos();
        return porcentaje(celdasDePlaza, celdasTotales);
    }

    // La calificación de RF-15 no compara contra el 100% de las celdas, que
    // ningún diseño puede alcanzar, sino contra el techo teórico: la cantidad
    // máxima de celdas de plaza que entra en esta grilla con estos pisos.
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

    // Sigue los pasos del PDF, uno por línea. Con 15×15 y un piso da 175.2
    // celdas (unas 87 plazas), el mismo número del ejemplo del documento.
    public static double techoCeldasDePlaza(GrillaSimulacion grilla) {
        int pisos = grilla.getCantidadPisos();

        // Paso 1: total de celdas, de todos los pisos.
        int celdasTotales = grilla.getFilas() * grilla.getColumnas() * pisos;

        // Paso 2: entrada y salida, que nunca pueden ser plaza.
        int sinEntradaYSalida = celdasTotales - CELDAS_ENTRADA_Y_SALIDA;

        // Paso 4: rampas y escalera, si hay más de un piso. La escalera ocupa
        // una celda en cada piso.
        int overheadVertical = 0;
        if (pisos > 1) {
            int conexiones = pisos - 1;
            int celdasRampa = conexiones * CELDAS_RAMPA_POR_CONEXION;
            int celdasEscalera = pisos;
            overheadVertical = celdasEscalera + celdasRampa;
        }
        int utilizablesPrimeraPasada = sinEntradaYSalida - overheadVertical;

        // Pasos 3 y 6: el mínimo de bicis/motos depende de la cantidad de
        // plazas, que es lo que se está calculando. Primera pasada sin
        // descontarlas; con eso se estima la cantidad de plazas (punto medio
        // entre el diseño de borde y el compacto) y se descuentan las zonas
        // que exigiría. No hace falta iterar: el ajuste es de pocas celdas.
        double plazasDisenoBorde = utilizablesPrimeraPasada * FRACCION_PLAZA_BORDE / CELDAS_POR_PLAZA;
        double plazasDisenoCompacto = utilizablesPrimeraPasada * FRACCION_PLAZA_DOBLE_CARGA / CELDAS_POR_PLAZA;
        double plazasEstimadas = (plazasDisenoBorde + plazasDisenoCompacto) / 2;
        int zonasBiciMoto = (int) Math.ceil(plazasEstimadas / PLAZAS_POR_ZONA_BICI_MOTO);
        int utilizables = utilizablesPrimeraPasada - zonasBiciMoto;

        // Paso 5 y 7: el techo es el extremo alcanzable, con doble carga.
        return utilizables * FRACCION_PLAZA_DOBLE_CARGA;
    }

    // --- RF-29: demanda satisfecha ---

    // Porcentaje de vehículos que consiguieron plaza sobre los que intentaron
    // ingresar. Si no llegó ninguno (período más corto que el intervalo entre
    // llegadas), no se rechazó a nadie: 100%.
    public static BigDecimal demandaSatisfecha(ResultadoMotor resultado) {
        int llegados = resultado.vehiculosLlegados();
        if (llegados == 0) {
            return porcentaje(1, 1);
        }
        int atendidos = llegados - resultado.vehiculosRechazados();
        return porcentaje(atendidos, llegados);
    }

    // --- RF-28: período de saturación ---

    // Los tramos de minutos consecutivos en los que la ocupación alcanzó el
    // total de plazas colocadas en la grilla. Lista vacía = no hubo
    // saturación (Flujo Alternativo A1).
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

    // Porcentaje con dos decimales, como DECIMAL(5,2) en contratos/esquema-bd.md.
    private static BigDecimal porcentaje(int parte, int total) {
        return BigDecimal.valueOf(parte)
                .multiply(BigDecimal.valueOf(100))
                .divide(BigDecimal.valueOf(total), 2, RoundingMode.HALF_UP);
    }
}
