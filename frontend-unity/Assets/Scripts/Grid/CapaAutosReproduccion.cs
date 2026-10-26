using UnityEngine;

namespace Sme.Grid
{
    public class CapaAutosReproduccion : MonoBehaviour
    {
        private const int OrdenCapaAutos = 2;

        private void Awake()
        {
            // Tiene que colgar de otro Canvas al llegar acá: en un Canvas raíz Unity ignora overrideSorting
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = OrdenCapaAutos;
        }
    }
}
