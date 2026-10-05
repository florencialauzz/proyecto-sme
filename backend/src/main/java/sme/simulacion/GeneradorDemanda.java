package sme.simulacion;

import java.util.ArrayList;
import java.util.List;
import java.util.Random;

// Arma la lista de vehículos que llegan durante el período, antes de simular.
// La demanda no depende del estacionamiento: se genera completa primero y el
// motor después la atiende como puede. Por eso dos proyectos con la misma
// configuración reciben exactamente los mismos vehículos, que es lo que hace
// justa la comparación de RF-25.
//
// Dos modos, según el checkbox de la configuración:
// - Sin fluctuaciones: llegadas parejas y permanencia fija, los valores
//   ingresados tal cual.
// - Con fluctuaciones: el tiempo entre llegadas y la permanencia de cada
//   vehículo se sortean dentro de ±50% alrededor del valor ingresado. El
//   sorteo es parejo y simétrico, así que en promedio se respeta lo
//   configurado.
public final class GeneradorDemanda {

    private static final int MINUTOS_POR_HORA = 60;

    // Cuánto puede alejarse cada valor sorteado del ingresado, para arriba o
    // para abajo: 0.5 = ±50%.
    private static final double MARGEN_FLUCTUACION = 0.5;

    // Semilla fija: el sorteo sale siempre igual. Ejecutar dos veces el mismo
    // proyecto da la misma curva, dos proyectos con la misma configuración
    // reciben la misma demanda, y los tests son reproducibles. El valor en sí
    // no importa, solo que no cambie.
    private static final long SEMILLA = 2026L;

    private GeneradorDemanda() {
    }

    // Llegadas: la frecuencia viene en vehículos por hora. Cada minuto suma
    // la frecuencia a un acumulador, y cada vez que el acumulador junta 60
    // llega un vehículo. Con 30/h llega uno cada 2 minutos (el primero en el
    // minuto 1); con 90/h, en algunos minutos llegan dos. Se hace en enteros
    // para que no se acumule error de redondeo en períodos largos.
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

    // Llegadas: el intervalo promedio entre dos vehículos es 60 / frecuencia
    // minutos (con 30/h, 2 minutos). A cada vehículo se le sortea un
    // intervalo entre la mitad y una vez y media del promedio (con 30/h,
    // entre 1 y 3 minutos) desde la llegada del anterior. El instante se
    // lleva con decimales y se trunca al minuto recién al registrar la
    // llegada, así que con frecuencias altas pueden llegar varios en el
    // mismo minuto.
    //
    // Permanencia: a cada vehículo se le sortea entre la mitad y una vez y
    // media del valor ingresado (con 120, entre 60 y 180 minutos),
    // redondeado al minuto y nunca menos de 1.
    //
    // Los dos sorteos de cada vehículo se hacen uno detrás del otro, en el
    // orden en que llegan, así que la lista no depende de nada más que de la
    // configuración.
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

    // Un factor entre 1 - MARGEN y 1 + MARGEN (0.5 a 1.5), todos los valores
    // igual de probables.
    private static double factorAlAzar(Random azar) {
        return 1 - MARGEN_FLUCTUACION + 2 * MARGEN_FLUCTUACION * azar.nextDouble();
    }
}
