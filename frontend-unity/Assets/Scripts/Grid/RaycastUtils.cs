using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace Sme.Grid
{
    // Compartido entre CatalogoItemPlaza y PiezaView: ambos necesitan saber
    // sobre qué celda quedó el puntero al soltar el arrastre.
    public static class RaycastUtils
    {
        public static CeldaView BuscarCeldaBajoPuntero(PointerEventData eventData)
        {
            var resultados = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, resultados);

            foreach (RaycastResult resultado in resultados)
            {
                CeldaView celda = resultado.gameObject.GetComponent<CeldaView>();
                if (celda != null)
                {
                    return celda;
                }
            }

            return null;
        }
    }
}
