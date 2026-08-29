using UnityEngine;
using UnityEngine.EventSystems;

namespace Sme.Grid
{
    // RF-13: origen de arrastre para colocar una Plaza nueva. El ícono del
    // catálogo es fijo (no se mueve ni se consume) — al empezar el arrastre
    // se instancia una pieza real, que solo queda colocada si se suelta sobre
    // una celda libre.
    public class CatalogoItemPlaza : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private GameObject prefabPlaza;

        private RectTransform piezaEnCurso;
        private Canvas canvasRaiz;

        private void Awake()
        {
            canvasRaiz = GetComponentInParent<Canvas>().rootCanvas;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            GameObject instancia = Instantiate(prefabPlaza, canvasRaiz.transform);
            piezaEnCurso = instancia.GetComponent<RectTransform>();
            instancia.GetComponent<CanvasGroup>().blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            piezaEnCurso.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            piezaEnCurso.GetComponent<CanvasGroup>().blocksRaycasts = true;
            CeldaView celdaDestino = RaycastUtils.BuscarCeldaBajoPuntero(eventData);

            bool colocada = celdaDestino != null
                && piezaEnCurso.GetComponent<PiezaView>().IntentarColocar(celdaDestino);

            if (!colocada)
            {
                Destroy(piezaEnCurso.gameObject);
            }
        }
    }
}
