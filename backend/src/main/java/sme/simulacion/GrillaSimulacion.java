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

    public CeldaSimulacion obtenerCelda(int piso, int fila, int columna) {
        boolean dentro = piso >= 0 && piso < cantidadPisos
                && fila >= 0 && fila < filas
                && columna >= 0 && columna < columnas;
        if (!dentro) return null;
        return celdas[piso][fila][columna];
    }

    public CeldaSimulacion obtenerVecina(CeldaSimulacion celda, Direccion direccion) {
        return obtenerCelda(celda.getPiso(), celda.getFila() + deltaFila(direccion),
                celda.getColumna() + deltaColumna(direccion));
    }

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
