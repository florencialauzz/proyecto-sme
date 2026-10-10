using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sme.UI
{
    // Avisa cuando se hace clic derecho sobre el objeto. Lo agrega
    // InicioScreen a cada fila de proyecto para abrir el menú con "Cambiar
    // nombre". El clic izquierdo lo sigue manejando el Toggle de la fila.
    public class DetectorClicDerecho : MonoBehaviour, IPointerClickHandler
    {
        public event Action<Vector2> AlHacerClicDerecho;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right) return;
            AlHacerClicDerecho?.Invoke(eventData.position);
        }
    }
}
