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

// editor/grafo-circulacion.md del lado de Java: por dónde puede pasar un
// vehículo, para que el motor busque la plaza libre más cercana a la entrada
// (RF-26). Todo se deriva de la grilla en el momento, no hay nada guardado.
//
// Unity tiene su propia copia de estas reglas para validar el diseño mientras
// se arma (RF-20). Son implementaciones separadas a propósito
// (arquitectura/decisiones.md): no hay que unificarlas.
public class GrafoCirculacion {

    private static final Direccion[] DIRECCIONES = {
            Direccion.NORTE, Direccion.SUR, Direccion.ESTE, Direccion.OESTE
    };

    private final GrillaSimulacion grilla;

    public GrafoCirculacion(GrillaSimulacion grilla) {
        this.grilla = grilla;
    }

    // Calle, Entrada, Salida y Rampa participan del grafo. Plaza y
    // ZonaBicicletasMotos se enganchan por su boca pero el vehículo no las
    // atraviesa; la Escalera es solo para el peatón.
    public static boolean esCeldaDeCirculacion(CeldaSimulacion celda) {
        if (celda == null) return false;
        TipoPieza tipo = celda.getTipo();
        return tipo == TipoPieza.CALLE || tipo == TipoPieza.ENTRADA || tipo == TipoPieza.SALIDA
                || tipo == TipoPieza.RAMPA;
    }

    // Colineal con el eje norte-sur o este-oeste que une a la celda con esa
    // vecina, para cualquiera de los dos lados del eje.
    private static boolean esColinealConEje(Direccion flecha, Direccion direccionAlVecino) {
        boolean ejeEsNorteSur = direccionAlVecino == Direccion.NORTE || direccionAlVecino == Direccion.SUR;
        boolean flechaEsNorteSur = flecha == Direccion.NORTE || flecha == Direccion.SUR;
        return ejeEsNorteSur == flechaEsNorteSur;
    }

    // Dos celdas de circulación contiguas están conectadas si al menos una de
    // las dos flechas guardadas es colineal con el eje que las une.
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

    // 3 o más conexiones → cruce. Entrada, Salida y Rampa nunca son cruce.
    private boolean esCruce(CeldaSimulacion celda) {
        return celda.getTipo() == TipoPieza.CALLE && direccionesConectadas(celda).size() >= 3;
    }

    // Por qué caras puede salir el vehículo hacia una vecina del mismo piso:
    // - Calle segmento: solo la cara de su flecha.
    // - Calle cruce: cualquier cara conectada (perdió la flecha).
    // - Entrada: la cara de su flecha, que apunta hacia adentro.
    // - Salida: ninguna hacia la grilla, su flecha apunta afuera.
    // - Rampa, piso de entrada: ninguna horizontal, solo el salto.
    // - Rampa, piso de salida: la cara de su flecha.
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

    // Si la celda deja entrar al vehículo que llega por esa cara. Quien llama
    // ya verificó que las dos celdas están conectadas.
    // - Calle segmento: por cualquier cara que no sea la de su flecha.
    // - Calle cruce: por cualquiera.
    // - Entrada: por ninguna (es fuente).
    // - Salida: por cualquiera.
    // - Rampa, piso de entrada: solo por la cara opuesta a la flecha.
    // - Rampa, piso de salida: por ninguna, ahí solo se llega por el salto.
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

    // La celda de rampa del otro piso que corresponde a esta, si esta es la
    // del piso de entrada. null si no lo es.
    private CeldaSimulacion parDeRampa(CeldaSimulacion celda) {
        if (celda.getTipo() != TipoPieza.RAMPA || !celda.isEsEntradaDeRampa()) return null;

        int pisoSalida = celda.getSentidoVertical() == SentidoVertical.SUBE ? celda.getPiso() + 1 : celda.getPiso() - 1;
        CeldaSimulacion par = grilla.obtenerCelda(pisoSalida, celda.getFila(), celda.getColumna());
        boolean esSuPar = par != null && par.getTipo() == TipoPieza.RAMPA && !par.isEsEntradaDeRampa()
                && par.getSentidoVertical() == celda.getSentidoVertical();
        return esSuPar ? par : null;
    }

    // Existe la arista A → B si A y B están conectadas, B está sobre una
    // salida de A, y B acepta la entrada desde A. Además, la entrada de una
    // rampa tiene una arista al salto: su par en el otro piso.
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

    // Cantidad de celdas que recorre el vehículo desde el origen hasta cada
    // celda de circulación alcanzable (recorrido en anchura sobre las
    // aristas). Las celdas que no aparecen en el mapa no son alcanzables.
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

    // Las celdas que recorre el vehículo desde el origen (primera) hasta el
    // destino (última), por uno de los caminos más cortos. Lista vacía si el
    // destino no es alcanzable.
    public List<CeldaSimulacion> caminoHasta(CeldaSimulacion origen, CeldaSimulacion destino) {
        return caminoHastaLaMasCercana(origen, List.of(destino));
    }

    // Igual que caminoHasta, pero hasta el más cercano de varios destinos
    // (la Salida más cercana a la boca de una plaza).
    //
    // Es el mismo recorrido en anchura que distanciasDesde, guardando además
    // de qué celda se llegó a cada una: así el largo del camino es la
    // distancia que usa el motor para ordenar las plazas, y el camino que se
    // ve en la reproducción es el que justificó esa distancia
    // (animacion/reglas.md). Los sucesores se recorren siempre en el mismo
    // orden, así que entre dos caminos igual de cortos sale siempre el mismo.
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

        // Se arma de atrás para adelante siguiendo los predecesores, y se da
        // vuelta al final.
        CeldaSimulacion celda = destinoEncontrado;
        while (celda != null) {
            camino.add(celda);
            celda = predecesores.get(celda);
        }
        Collections.reverse(camino);
        return camino;
    }
}
