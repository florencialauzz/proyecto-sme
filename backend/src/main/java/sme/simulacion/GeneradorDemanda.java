package sme.simulacion;

import java.util.ArrayList;
import java.util.List;
import java.util.Random;

public final class GeneradorDemanda {
    private static final int MINUTOS_POR_HORA = 60;

    private static final double MARGEN_FLUCTUACION = 0.5;

    // Semilla fija: misma configuración, misma demanda (RF-25 compara con la misma)
    private static final long SEMILLA = 2026L;

    private GeneradorDemanda() {
    }

    public static List<Vehiculo> sinFluctuaciones(int frecuenciaPorHora, int tiempoPermanencia, int duracionMinutos) {
        List<Vehiculo> vehiculos = new ArrayList<>();
        int acumuladorLlegadas = 0;

        for (int minuto = 0; minuto < duracionMinutos; minuto++) {
            acumuladorLlegadas += frecuenciaPorHora;
            while (acumuladorLlegadas >= MINUTOS_POR_HORA) {
                acumuladorLlegadas -= MINUTOS_POR_HORA;
                vehiculos.add(new Vehiculo(minuto, tiempoPermanencia));
            }
        }

        return vehiculos;
    }

    public static List<Vehiculo> conFluctuaciones(int frecuenciaPorHora, int tiempoPermanencia, int duracionMinutos) {
        Random azar = new Random(SEMILLA);
        double intervaloPromedio = (double) MINUTOS_POR_HORA / frecuenciaPorHora;

        List<Vehiculo> vehiculos = new ArrayList<>();
        double instanteLlegada = intervaloPromedio * factorAlAzar(azar);

        while (instanteLlegada < duracionMinutos) {
            int permanencia = (int) Math.round(tiempoPermanencia * factorAlAzar(azar));
            permanencia = Math.max(1, permanencia);

            int minutoLlegada = (int) instanteLlegada;
            vehiculos.add(new Vehiculo(minutoLlegada, permanencia));

            instanteLlegada += intervaloPromedio * factorAlAzar(azar);
        }

        return vehiculos;
    }

    private static double factorAlAzar(Random azar) {
        return 1 - MARGEN_FLUCTUACION + 2 * MARGEN_FLUCTUACION * azar.nextDouble();
    }
}
