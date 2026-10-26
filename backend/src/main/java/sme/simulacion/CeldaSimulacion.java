package sme.simulacion;

import sme.entity.Direccion;
import sme.entity.SentidoVertical;
import sme.entity.TipoPieza;

public class CeldaSimulacion {
    private final int piso;
    private final int fila;
    private final int columna;
    private final TipoPieza tipo;

    private final Direccion direccion;

    private final Direccion caraAcceso;

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
