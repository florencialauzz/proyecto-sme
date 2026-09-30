package sme.simulacion;

import sme.entity.Direccion;
import sme.entity.SentidoVertical;
import sme.entity.TipoPieza;

// Una celda ocupada de la grilla, tal como la ve el motor de simulación.
// Sale de una fila de Pieza, más lo que se deriva al armar la grilla (cuál
// de las dos celdas de una rampa es la de entrada).
public class CeldaSimulacion {

    private final int piso;
    private final int fila;
    private final int columna;
    private final TipoPieza tipo;

    // Calle, Entrada, Salida y Rampa: hacia dónde sale el vehículo.
    private final Direccion direccion;

    // Plaza y ZonaBicicletasMotos: de qué lado está la celda de circulación
    // por la que se entra.
    private final Direccion caraAcceso;

    // Solo Rampa.
    private final SentidoVertical sentidoVertical;
    private boolean esEntradaDeRampa;

    public CeldaSimulacion(int piso, int fila, int columna, TipoPieza tipo, Direccion direccion,
                           Direccion caraAcceso, SentidoVertical sentidoVertical) {
        this.piso = piso;
        this.fila = fila;
        this.columna = columna;
        this.tipo = tipo;
        this.direccion = direccion;
        this.caraAcceso = caraAcceso;
        this.sentidoVertical = sentidoVertical;
        this.esEntradaDeRampa = false;
    }

    public int getPiso() { return piso; }
    public int getFila() { return fila; }
    public int getColumna() { return columna; }
    public TipoPieza getTipo() { return tipo; }
    public Direccion getDireccion() { return direccion; }
    public Direccion getCaraAcceso() { return caraAcceso; }
    public SentidoVertical getSentidoVertical() { return sentidoVertical; }

    public boolean isEsEntradaDeRampa() { return esEntradaDeRampa; }
    public void setEsEntradaDeRampa(boolean esEntradaDeRampa) { this.esEntradaDeRampa = esEntradaDeRampa; }
}
