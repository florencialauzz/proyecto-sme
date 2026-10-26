using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sme.UI
{
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
