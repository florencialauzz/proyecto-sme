package sme.simulacion;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Map;

// RF-26: recorre minuto a minuto el período configurado. En cada minuto
// libera las plazas cuyo tiempo de permanencia venció, registra las llegadas
// de ese minuto, y a cada vehículo le asigna la plaza libre más cercana a la
// entrada siguiendo el grafo de circulación. Si no hay ninguna libre, el
// vehículo cuenta como rechazado (Flujo Alternativo A1).
//
// Qué vehículos llegan y cuánto se queda cada uno no lo decide el motor: lo
// arma GeneradorDemanda antes, con o sin fluctuaciones. Así la misma grilla y
// la misma configuración dan siempre el mismo resultado, y dos proyectos con
// la misma configuración reciben exactamente la misma demanda — eso es lo que
// hace justa la comparación de RF-25.
public class MotorSimulacion {

    // Valor de minutoLiberacion para una plaza desocupada, y respuesta de
    // primeraPlazaLibre cuando están todas ocupadas.
    private static final int LIBRE = -1;
    private static final int SIN_PLAZA_LIBRE = -1;

    private final GrillaSimulacion grilla;

    // Las plazas donde un vehículo puede estacionar, de la más cercana a la
    // entrada a la más lejana. Como la distancia no depende de qué plazas
    // están ocupadas, el orden se calcula una sola vez.
    private final List<CeldaSimulacion> plazasPorCercania;

    public MotorSimulacion(GrillaSimulacion grilla) {
        this.grilla = grilla;
        this.plazasPorCercania = ordenarPlazasPorCercania();
    }

    // La distancia de una plaza es la de su boca (la celda de circulación
    // vecina en la dirección de caraAcceso) más uno, el paso para entrar.
    // Una plaza entra en la lista solo si su boca es alcanzable desde la
    // entrada y desde la boca se puede llegar a una salida: un vehículo no
    // estaciona donde después no puede salir. Con un diseño que pasó RF-20 en
    // Unity son todas; el filtro evita que un dato inconsistente deje autos
    // atrapados en la simulación.
    //
    // Empates de distancia: se desempata por piso, fila y columna, para que
    // el resultado no dependa del orden en que vinieron las piezas.
    private List<CeldaSimulacion> ordenarPlazasPorCercania() {
        GrafoCirculacion grafo = new GrafoCirculacion(grilla);
        CeldaSimulacion entrada = grilla.getEntradas().get(0);
        Map<CeldaSimulacion, Integer> distanciasDesdeEntrada = grafo.distanciasDesde(entrada);

        List<PlazaConDistancia> alcanzables = new ArrayList<>();
        for (CeldaSimulacion plaza : grilla.getPlazas()) {
            CeldaSimulacion boca = grilla.obtenerVecina(plaza, plaza.getCaraAcceso());
            if (!GrafoCirculacion.esCeldaDeCirculacion(boca)) continue;
            if (!distanciasDesdeEntrada.containsKey(boca)) continue;
            if (!puedeLlegarAUnaSalida(grafo, boca)) continue;

            int distancia = distanciasDesdeEntrada.get(boca) + 1;
            alcanzables.add(new PlazaConDistancia(plaza, distancia));
        }

        alcanzables.sort(Comparator
                .comparingInt(PlazaConDistancia::distancia)
                .thenComparingInt(p -> p.plaza().getPiso())
                .thenComparingInt(p -> p.plaza().getFila())
                .thenComparingInt(p -> p.plaza().getColumna()));

        List<CeldaSimulacion> ordenadas = new ArrayList<>();
        for (PlazaConDistancia plazaConDistancia : alcanzables) {
            ordenadas.add(plazaConDistancia.plaza());
        }
        return ordenadas;
    }

    // Solo para los tests: el orden en que se asignan las plazas.
    List<CeldaSimulacion> getPlazasPorCercania() {
        return plazasPorCercania;
    }

    private boolean puedeLlegarAUnaSalida(GrafoCirculacion grafo, CeldaSimulacion desde) {
        Map<CeldaSimulacion, Integer> alcanzablesDesdeAhi = grafo.distanciasDesde(desde);
        for (CeldaSimulacion salida : grilla.getSalidas()) {
            if (alcanzablesDesdeAhi.containsKey(salida)) return true;
        }
        return false;
    }

    // Recorre el período minuto a minuto atendiendo a los vehículos de la
    // demanda (GeneradorDemanda), que vienen ordenados por minuto de llegada.
    //
    // Permanencia: un vehículo que estaciona en el minuto m libera la plaza
    // al empezar el minuto m + su tiempoPermanencia, así que la ocupa durante
    // exactamente esa cantidad de minutos.
    //
    // La curva registra, para cada minuto, las plazas ocupadas después de
    // procesar las salidas y las llegadas de ese minuto.
    public ResultadoMotor ejecutar(List<Vehiculo> vehiculos, int duracionMinutos) {
        // Minuto en que se libera cada plaza de plazasPorCercania, o LIBRE.
        int[] minutoLiberacion = new int[plazasPorCercania.size()];
        for (int i = 0; i < minutoLiberacion.length; i++) {
            minutoLiberacion[i] = LIBRE;
        }

        List<PuntoCurva> curva = new ArrayList<>();
        int siguienteVehiculo = 0;
        int vehiculosLlegados = 0;
        int vehiculosRechazados = 0;
        int ocupadas = 0;

        for (int minuto = 0; minuto < duracionMinutos; minuto++) {
            for (int i = 0; i < minutoLiberacion.length; i++) {
                if (minutoLiberacion[i] != LIBRE && minutoLiberacion[i] <= minuto) {
                    minutoLiberacion[i] = LIBRE;
                    ocupadas--;
                }
            }

            while (siguienteVehiculo < vehiculos.size()
                    && vehiculos.get(siguienteVehiculo).minutoLlegada() == minuto) {
                Vehiculo vehiculo = vehiculos.get(siguienteVehiculo);
                siguienteVehiculo++;
                vehiculosLlegados++;

                int plazaLibre = primeraPlazaLibre(minutoLiberacion);
                if (plazaLibre == SIN_PLAZA_LIBRE) {
                    vehiculosRechazados++;
                } else {
                    minutoLiberacion[plazaLibre] = minuto + vehiculo.tiempoPermanencia();
                    ocupadas++;
                }
            }

            curva.add(new PuntoCurva(minuto, ocupadas));
        }

        return new ResultadoMotor(curva, vehiculosLlegados, vehiculosRechazados, plazasPorCercania.size());
    }

    // El índice de la plaza libre más cercana a la entrada, o SIN_PLAZA_LIBRE
    // si están todas ocupadas. Como la lista ya está ordenada por cercanía,
    // es la primera libre.
    private int primeraPlazaLibre(int[] minutoLiberacion) {
        for (int i = 0; i < minutoLiberacion.length; i++) {
            if (minutoLiberacion[i] == LIBRE) return i;
        }
        return SIN_PLAZA_LIBRE;
    }

    private record PlazaConDistancia(CeldaSimulacion plaza, int distancia) {
    }
}
