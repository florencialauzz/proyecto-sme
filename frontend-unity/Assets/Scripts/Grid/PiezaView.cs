using Sme.Managers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sme.Grid
{
    // RF-13: pieza Plaza ya colocada en la grilla. Se puede mover (arrastrar a
    // otra celda), rotar (clic, cambia la cara de acceso) o quitar (arrastrar
    // fuera de los límites de la grilla).
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class PiezaView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform flechaAcceso;

        private CaraAcceso caraAcceso = CaraAcceso.NORTE;
        private CeldaView celdaActual;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas canvasRaiz;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            canvasRaiz = GetComponentInParent<Canvas>().rootCanvas;
        }

        // Deja la pieza asentada en una celda: la marca ocupada y la centra
        // dentro de sus límites. La usan tanto la colocación inicial (desde
        // CatalogoItemPlaza) como un movimiento válido (OnEndDrag).
        public void Inicializar(CeldaView celda)
        {
            celdaActual = celda;
            celda.Ocupar();
            rectTransform.SetParent(celda.transform, false);
            rectTransform.anchoredPosition = Vector2.zero;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            RotarCaraAcceso();
        }

        private void RotarCaraAcceso()
        {
            caraAcceso = (CaraAcceso)(((int)caraAcceso + 1) % 4);

            if (flechaAcceso != null)
            {
                flechaAcceso.localEulerAngles = new Vector3(0f, 0f, -90f * (int)caraAcceso);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            celdaActual.Liberar();
            rectTransform.SetParent(canvasRaiz.transform, true);
            canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            rectTransform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;
            CeldaView celdaDestino = RaycastUtils.BuscarCeldaBajoPuntero(eventData);

            if (celdaDestino == null)
            {
                // Soltada fuera de los límites de la grilla: quitar.
                Destroy(gameObject);
                return;
            }

            if (celdaDestino.Ocupada && celdaDestino != celdaActual)
            {
                MensajesEditor.Mostrar("La celda está ocupada.");
                Inicializar(celdaActual);
                return;
            }

            Inicializar(celdaDestino);
        }
    }
}
