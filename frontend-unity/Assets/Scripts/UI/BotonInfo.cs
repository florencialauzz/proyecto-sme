using UnityEngine;
using UnityEngine.EventSystems;

namespace Sme.UI
{
    // Ícono "i" que muestra un panel con una explicación breve mientras el
    // puntero está encima. El panel se arma en la escena (fondo + texto) y
    // arranca desactivado; acá solo se prende y se apaga.
    //
    // El ícono necesita un Graphic con Raycast Target (la Image o el texto
    // de la "i") para recibir los eventos del puntero.
    public class BotonInfo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject panelInfo;

        private void Awake()
        {
            panelInfo.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            panelInfo.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            panelInfo.SetActive(false);
        }

        // Si la pantalla se oculta con el puntero encima, OnPointerExit no
        // llega y el panel quedaría prendido la próxima vez que se muestre.
        private void OnDisable()
        {
            panelInfo.SetActive(false);
        }
    }
}
