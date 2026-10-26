package sme.simulacion;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Map;

public class MotorSimulacion {
    private static final int LIBRE = -1;
    private static final int SIN_PLAZA_LIBRE = -1;

    private final GrillaSimulacion grilla;

    private final List<CeldaSimulacion> plazasPorCercania;

    public MotorSimulacion(GrillaSimulacion grilla) {
        this.grilla = grilla;
        this.plazasPorCercania = ordenarPlazasPorCercania();
    }

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

    List<CeldaSimulacion> getPlazasPorCercania() {
        return plazasPorCercania;
    }

    public List<CaminosDePlaza> calcularCaminosDePlazas() {
        GrafoCirculacion grafo = new GrafoCirculacion(grilla);
        CeldaSimulacion entrada = grilla.getEntradas().get(0);

        List<CaminosDePlaza> caminos = new ArrayList<>();
        for (CeldaSimulacion plaza : plazasPorCercania) {
            CeldaSimulacion boca = grilla.obtenerVecina(plaza, plaza.getCaraAcceso());
            List<CeldaSimulacion> caminoEntrada = grafo.caminoHasta(entrada, boca);
            List<CeldaSimulacion> caminoSalida = grafo.caminoHastaLaMasCercana(boca, grilla.getSalidas());
            caminos.add(new CaminosDePlaza(plaza, caminoEntrada, caminoSalida));
        }
        return caminos;
    }

    public int indicePlazaRecorridoRechazados() {
        if (plazasPorCercania.isEmpty()) {
            return EventoVehiculo.SIN_PLAZA;
        }
        return plazasPorCercania.size() - 1;
    }

    private boolean puedeLlegarAUnaSalida(GrafoCirculacion grafo, CeldaSimulacion desde) {
        Map<CeldaSimulacion, Integer> alcanzablesDesdeAhi = grafo.distanciasDesde(desde);
        for (CeldaSimulacion salida : grilla.getSalidas()) {
            if (alcanzablesDesdeAhi.containsKey(salida)) return true;
        }
        return false;
    }

    public ResultadoMotor ejecutar(List<Vehiculo> vehiculos, int duracionMinutos) {
        int[] minutoLiberacion = new int[plazasPorCercania.size()];
        for (int i = 0; i < minutoLiberacion.length; i++) {
            minutoLiberacion[i] = LIBRE;
        }

        List<PuntoCurva> curva = new ArrayList<>();
        List<EventoVehiculo> eventos = new ArrayList<>();
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
                    eventos.add(new EventoVehiculo(minuto, minuto, EventoVehiculo.SIN_PLAZA));
                } else {
                    minutoLiberacion[plazaLibre] = minuto + vehiculo.tiempoPermanencia();
                    ocupadas++;
                    eventos.add(new EventoVehiculo(minuto, minutoLiberacion[plazaLibre], plazaLibre));
                }
            }

            curva.add(new PuntoCurva(minuto, ocupadas));
        }

        return new ResultadoMotor(curva, vehiculosLlegados, vehiculosRechazados, plazasPorCercania.size(),
                eventos);
    }

    private int primeraPlazaLibre(int[] minutoLiberacion) {
        for (int i = 0; i < minutoLiberacion.length; i++) {
            if (minutoLiberacion[i] == LIBRE) return i;
        }
        return SIN_PLAZA_LIBRE;
    }

    private record PlazaConDistancia(CeldaSimulacion plaza, int distancia) {
    }
}
