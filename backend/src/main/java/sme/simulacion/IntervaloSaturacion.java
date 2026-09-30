package sme.simulacion;

// Un tramo del período simulado en el que todas las plazas estuvieron
// ocupadas (RF-28). inicio y fin son minutos desde la hora de inicio, los dos
// incluidos: un tramo de un solo minuto tiene inicio == fin.
public record IntervaloSaturacion(int inicio, int fin) {
}
