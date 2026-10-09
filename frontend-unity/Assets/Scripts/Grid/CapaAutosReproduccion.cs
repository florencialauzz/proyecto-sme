using UnityEngine;

namespace Sme.Grid
{
    // Capa de los autos en movimiento de un piso (AutosReproduccion). Su
    // Canvas propio, con sorting forzado, la dibuja encima de las piezas
    // (mismo motivo que PiezaView.Awake).
    //
    // El overrideSorting solo se respeta si, al configurarlo, el Canvas está
    // activo y colgado de otro Canvas; si no, Unity lo toma como Canvas raíz
    // y los autos quedan dibujados debajo de las piezas. Por eso el Canvas se
    // crea en Awake: en los pisos que no se están mirando (contenedor
    // inactivo) corre recién cuando el piso se activa, igual que el de las
    // piezas de esos pisos. AutosReproduccion agrega este componente después
    // de colgar la capa del piso.
    public class CapaAutosReproduccion : MonoBehaviour
    {
        // Las piezas usan sortingOrder 1 (PiezaView.Awake).
        private const int OrdenCapaAutos = 2;

        private void Awake()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = OrdenCapaAutos;
        }
    }
}
