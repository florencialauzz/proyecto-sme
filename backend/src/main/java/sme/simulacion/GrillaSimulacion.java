package sme.simulacion;

import sme.entity.Direccion;
import sme.entity.Pieza;
import sme.entity.SentidoVertical;
import sme.entity.TipoPieza;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

// La grilla del proyecto armada en memoria a partir de las piezas guardadas,
// indexada por (piso, fila, columna). Es la base sobre la que GrafoCirculacion
// calcula las aristas.
//
// Una Plaza ocupa dos celdas pero se guarda como una sola fila, en su ancla
// (arquitectura/decisiones.md). Acá solo se registra el ancla: la celda de
// fondo no es de circulación, así que al grafo no le cambia nada, y para
// contar celdas de plaza alcanza con multiplicar por 2.
public class GrillaSimulacion {

    private final int cantidadPisos;
    private final int filas;
    private final int columnas;
    private final CeldaSimulacion[][][] celdas;

    private final List<CeldaSimulacion> plazas = new ArrayList<>();
    private final List<CeldaSimulacion> entradas = new ArrayList<>();
    private final List<CeldaSimulacion> salidas = new ArrayList<>();

    public GrillaSimulacion(int cantidadPisos, int filas, int columnas, List<Pieza> piezas) {
        this.cantidadPisos = cantidadPisos;
        this.filas = filas;
        this.columnas = columnas;
        this.celdas = new CeldaSimulacion[cantidadPisos][filas][columnas];

        for (Pieza pieza : piezas) {
            CeldaSimulacion celda = new CeldaSimulacion(
                    pieza.getPiso(),
                    pieza.getFila(),
                    pieza.getColumna(),
                    pieza.getTipo(),
                    pieza.getDireccion(),
                    pieza.getCaraAcceso(),
                    pieza.getSentidoVertical());
            celdas[celda.getPiso()][celda.getFila()][celda.getColumna()] = celda;

            if (celda.getTipo() == TipoPieza.PLAZA) {
                plazas.add(celda);
            } else if (celda.getTipo() == TipoPieza.ENTRADA) {
                entradas.add(celda);
            } else if (celda.getTipo() == TipoPieza.SALIDA) {
                salidas.add(celda);
            }
        }

        marcarEntradasDeRampas();
    }

    // Las dos filas de una rampa guardan la misma posición, sentido y flecha;
    // lo que no se guarda es cuál de las dos es la de entrada
    // (editor/catalogo-piezas.md, "el par de rampa se deriva"). Una que sube
    // entra por el piso de abajo del par, una que baja por el de arriba. Si en
    // la misma posición hay varias rampas del mismo sentido apiladas (0→1 y
    // 2→3), recorrerlas en orden de entrada las empareja bien, porque ninguna
    // celda puede ser de dos rampas a la vez. Una fila sin par queda como
    // salida de rampa sin entrada: no aporta aristas.
    private void marcarEntradasDeRampas() {
        Map<String, List<CeldaSimulacion>> rampasPorPosicionYSentido = new HashMap<>();
        for (int piso = 0; piso < cantidadPisos; piso++) {
            for (int fila = 0; fila < filas; fila++) {
                for (int columna = 0; columna < columnas; columna++) {
                    CeldaSimulacion celda = celdas[piso][fila][columna];
                    if (celda == null || celda.getTipo() != TipoPieza.RAMPA) continue;

                    String clave = fila + "," + columna + "," + celda.getSentidoVertical();
                    rampasPorPosicionYSentido.computeIfAbsent(clave, k -> new ArrayList<>()).add(celda);
                }
            }
        }

        for (List<CeldaSimulacion> grupo : rampasPorPosicionYSentido.values()) {
            boolean sube = grupo.get(0).getSentidoVertical() == SentidoVertical.SUBE;
            if (sube) {
                grupo.sort(Comparator.comparingInt(CeldaSimulacion::getPiso));
            } else {
                grupo.sort(Comparator.comparingInt(CeldaSimulacion::getPiso).reversed());
            }

            int i = 0;
            while (i < grupo.size()) {
                int pisoSalidaEsperado = grupo.get(i).getPiso() + (sube ? 1 : -1);
                boolean tienePar = i + 1 < grupo.size() && grupo.get(i + 1).getPiso() == pisoSalidaEsperado;
                if (tienePar) {
                    grupo.get(i).setEsEntradaDeRampa(true);
                    i += 2;
                } else {
                    i += 1;
                }
            }
        }
    }

    // null si la posición está vacía o fuera de la grilla.
    public CeldaSimulacion obtenerCelda(int piso, int fila, int columna) {
        boolean dentro = piso >= 0 && piso < cantidadPisos
                && fila >= 0 && fila < filas
                && columna >= 0 && columna < columnas;
        if (!dentro) return null;
        return celdas[piso][fila][columna];
    }

    // La vecina de la celda en esa dirección, dentro del mismo piso.
    public CeldaSimulacion obtenerVecina(CeldaSimulacion celda, Direccion direccion) {
        return obtenerCelda(celda.getPiso(), celda.getFila() + deltaFila(direccion),
                celda.getColumna() + deltaColumna(direccion));
    }

    // Misma convención que Unity (GrillaModelo.Delta): la fila 0 es la de
    // arriba, así que NORTE resta una fila.
    public static int deltaFila(Direccion direccion) {
        switch (direccion) {
            case NORTE: return -1;
            case SUR: return 1;
            default: return 0;
        }
    }

    public static int deltaColumna(Direccion direccion) {
        switch (direccion) {
            case ESTE: return 1;
            case OESTE: return -1;
            default: return 0;
        }
    }

    public static Direccion opuesta(Direccion direccion) {
        switch (direccion) {
            case NORTE: return Direccion.SUR;
            case SUR: return Direccion.NORTE;
            case ESTE: return Direccion.OESTE;
            default: return Direccion.ESTE;
        }
    }

    public int getCantidadPisos() { return cantidadPisos; }
    public int getFilas() { return filas; }
    public int getColumnas() { return columnas; }
    public List<CeldaSimulacion> getPlazas() { return plazas; }
    public List<CeldaSimulacion> getEntradas() { return entradas; }
    public List<CeldaSimulacion> getSalidas() { return salidas; }
}
