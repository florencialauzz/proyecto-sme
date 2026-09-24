namespace Sme.Grid
{
    // Espejo del ENUM tipo de contratos/esquema-bd.md. El orden importa: los
    // prefabs guardan el tipo como número (tipo: 2 = ENTRADA), así que los
    // valores nuevos van siempre al final.
    public enum TipoPieza
    {
        PLAZA,
        CALLE,
        ENTRADA,
        SALIDA,
        ZONA_BICI_MOTO,
        RAMPA,
        ESCALERA
    }
}
