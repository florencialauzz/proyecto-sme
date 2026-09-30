package sme.simulacion;

// Un punto de la curva de ocupación (RF-27): cuántas plazas había ocupadas en
// ese minuto del período simulado, contando desde la hora de inicio. Par
// explícito en vez de índice de array, igual que PuntoOcupacion
// (arquitectura/decisiones.md).
public record PuntoCurva(int minuto, int cantidadOcupadas) {
}
