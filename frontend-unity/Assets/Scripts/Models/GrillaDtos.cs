using System;

namespace Sme.Models
{
    // Espejo de contratos/api-contract.md — PUT /api/proyectos/{id}/grilla
    // (RF-13, RF-14, RF-16, RF-21). "caraAcceso" es la orientación de
    // cualquier pieza orientable, sea cara de acceso (Plaza, ZonaBiciMoto) o
    // dirección de circulación (Calle, Entrada, Salida) — el backend decide a
    // qué columna la guarda según "tipo" (ver ProyectoService.aPieza).
    // "esCrucePeatonal" solo aplica a piezas tipo CALLE (RF-17, todavía sin
    // interacción para alternarlo).

    [Serializable]
    public class PiezaDto
    {
        public int piso;
        public int fila;
        public int columna;
        public string tipo;
        public string caraAcceso;
        public bool esAccesible;
        public bool esCrucePeatonal;
    }

    [Serializable]
    public class GuardarGrillaRequest
    {
        public PiezaDto[] piezas;
    }

    [Serializable]
    public class GuardarGrillaResponse
    {
        public bool guardado;
    }
}
