package sme.simulacion;

public record EventoVehiculo(int minutoLlegada, int minutoSalida, int indicePlaza) {
    public static final int SIN_PLAZA = -1;
}
