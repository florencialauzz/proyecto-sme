package sme.simulacion;

import sme.entity.Direccion;
import sme.entity.SentidoVertical;
import sme.entity.TipoPieza;

import java.util.ArrayDeque;
import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Queue;

public class GrafoCirculacion {
    private static final Direccion[] DIRECCIONES = {
            Direccion.NORTE, Direccion.SUR, Direccion.ESTE, Direccion.OESTE
    };

    private final GrillaSimulacion grilla;

    public GrafoCirculacion(GrillaSimulacion grilla) {
        this.grilla = grilla;
    }

    public static boolean esCeldaDeCirculacion(CeldaSimulacion celda) {
        if (celda == null) return false;
        TipoPieza tipo = celda.getTipo();
        return tipo == TipoPieza.CALLE || tipo == TipoPieza.ENTRADA || tipo == TipoPieza.SALIDA
                || tipo == TipoPieza.RAMPA;
    }

    private static boolean esColinealConEje(Direccion flecha, Direccion direccionAlVecino) {
        boolean ejeEsNorteSur = direccionAlVecino == Direccion.NORTE || direccionAlVecino == Direccion.SUR;
        boolean flechaEsNorteSur = flecha == Direccion.NORTE || flecha == Direccion.SUR;
        return ejeEsNorteSur == flechaEsNorteSur;
    }

    private static boolean estanConectadas(CeldaSimulacion celda, CeldaSimulacion vecina, Direccion direccionAlVecino) {
        if (!esCeldaDeCirculacion(celda) || !esCeldaDeCirculacion(vecina)) return false;

        boolean celdaColineal = esColinealConEje(celda.getDireccion(), direccionAlVecino);
        boolean vecinaColineal = esColinealConEje(vecina.getDireccion(), direccionAlVecino);
        return celdaColineal || vecinaColineal;
    }

    private List<Direccion> direccionesConectadas(CeldaSimulacion celda) {
        List<Direccion> conectadas = new ArrayList<>();
        for (Direccion direccion : DIRECCIONES) {
            CeldaSimulacion vecina = grilla.obtenerVecina(celda, direccion);
            if (estanConectadas(celda, vecina, direccion)) {
                conectadas.add(direccion);
            }
        }
        return conectadas;
    }

    private boolean esCruce(CeldaSimulacion celda) {
        return celda.getTipo() == TipoPieza.CALLE && direccionesConectadas(celda).size() >= 3;
    }

    private List<Direccion> carasDeSalida(CeldaSimulacion celda) {
        switch (celda.getTipo()) {
            case CALLE:
                if (esCruce(celda)) {
                    return direccionesConectadas(celda);
                }
                return List.of(celda.getDireccion());
            case ENTRADA:
                return List.of(celda.getDireccion());
            case RAMPA:
                if (celda.isEsEntradaDeRampa()) {
                    return List.of();
                }
                return List.of(celda.getDireccion());
            default:
                return List.of();
        }
    }

    private boolean aceptaEntradaPor(CeldaSimulacion celda, Direccion caraDeLlegada) {
        switch (celda.getTipo()) {
            case CALLE:
                return esCruce(celda) || caraDeLlegada != celda.getDireccion();
            case SALIDA:
                return true;
            case RAMPA:
                return celda.isEsEntradaDeRampa()
                        && caraDeLlegada == GrillaSimulacion.opuesta(celda.getDireccion());
            default:
                return false;
        }
    }

    private CeldaSimulacion parDeRampa(CeldaSimulacion celda) {
        if (celda.getTipo() != TipoPieza.RAMPA || !celda.isEsEntradaDeRampa()) return null;

        int pisoSalida = celda.getSentidoVertical() == SentidoVertical.SUBE ? celda.getPiso() + 1 : celda.getPiso() - 1;
        CeldaSimulacion par = grilla.obtenerCelda(pisoSalida, celda.getFila(), celda.getColumna());
        boolean esSuPar = par != null && par.getTipo() == TipoPieza.RAMPA && !par.isEsEntradaDeRampa()
                && par.getSentidoVertical() == celda.getSentidoVertical();
        return esSuPar ? par : null;
    }

    public List<CeldaSimulacion> sucesores(CeldaSimulacion celda) {
        List<CeldaSimulacion> sucesores = new ArrayList<>();
        if (!esCeldaDeCirculacion(celda)) return sucesores;

        for (Direccion cara : carasDeSalida(celda)) {
            CeldaSimulacion vecina = grilla.obtenerVecina(celda, cara);
            if (vecina == null) continue;

            if (estanConectadas(celda, vecina, cara) && aceptaEntradaPor(vecina, GrillaSimulacion.opuesta(cara))) {
                sucesores.add(vecina);
            }
        }

        CeldaSimulacion par = parDeRampa(celda);
        if (par != null) {
            sucesores.add(par);
        }

        return sucesores;
    }

    public Map<CeldaSimulacion, Integer> distanciasDesde(CeldaSimulacion origen) {
        Map<CeldaSimulacion, Integer> distancias = new HashMap<>();
        Queue<CeldaSimulacion> pendientes = new ArrayDeque<>();

        distancias.put(origen, 0);
        pendientes.add(origen);

        while (!pendientes.isEmpty()) {
            CeldaSimulacion actual = pendientes.poll();
            int distanciaActual = distancias.get(actual);

            for (CeldaSimulacion siguiente : sucesores(actual)) {
                if (distancias.containsKey(siguiente)) continue;

                distancias.put(siguiente, distanciaActual + 1);
                pendientes.add(siguiente);
            }
        }

        return distancias;
    }

    public List<CeldaSimulacion> caminoHasta(CeldaSimulacion origen, CeldaSimulacion destino) {
        return caminoHastaLaMasCercana(origen, List.of(destino));
    }

    public List<CeldaSimulacion> caminoHastaLaMasCercana(CeldaSimulacion origen, List<CeldaSimulacion> destinos) {
        Map<CeldaSimulacion, CeldaSimulacion> predecesores = new HashMap<>();
        Queue<CeldaSimulacion> pendientes = new ArrayDeque<>();

        predecesores.put(origen, null);
        pendientes.add(origen);

        CeldaSimulacion destinoEncontrado = null;
        while (!pendientes.isEmpty()) {
            CeldaSimulacion actual = pendientes.poll();
            if (destinos.contains(actual)) {
                destinoEncontrado = actual;
                break;
            }

            for (CeldaSimulacion siguiente : sucesores(actual)) {
                if (predecesores.containsKey(siguiente)) continue;

                predecesores.put(siguiente, actual);
                pendientes.add(siguiente);
            }
        }

        List<CeldaSimulacion> camino = new ArrayList<>();
        if (destinoEncontrado == null) return camino;

        CeldaSimulacion celda = destinoEncontrado;
        while (celda != null) {
            camino.add(celda);
            celda = predecesores.get(celda);
        }
        Collections.reverse(camino);
        return camino;
    }
}
