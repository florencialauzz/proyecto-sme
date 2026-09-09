using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Sme.Grid
{
    // RF-13/RF-16: origen de arrastre para colocar una pieza nueva del
    // catálogo (Plaza, Calle, Entrada, Salida, ZonaBicicletasMotos — un
    // GameObject de catálogo por tipo, todos con este mismo script). El ícono
    // del catálogo es fijo (no se mueve ni se consume) — al empezar el
    // arrastre se instancia la pieza real (prefabPieza), que maneja su propia
    // orientación (tecla R) y colocación a través de IArrastrableDesdeCatalogo.
    // Este script solo reenvía la posición del puntero, porque Unity solo
    // llama IBeginDrag/IDrag/IEndDrag sobre el objeto donde arrancó el gesto
    // (el ícono), no sobre la pieza recién instanciada.
    public class CatalogoItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // FormerlySerializedAs conserva la referencia ya asignada en el botón
        // de catálogo de Plaza (serializada con el nombre de campo viejo,
        // antes de generalizar este script).
        [FormerlySerializedAs("prefabPlaza")]
        [SerializeField] private GameObject prefabPieza;

        private IArrastrableDesdeCatalogo piezaEnCurso;

        public void OnBeginDrag(PointerEventData eventData)
        {
            Canvas canvasRaiz = GetComponentInParent<Canvas>().rootCanvas;
            GameObject instancia = Instantiate(prefabPieza, canvasRaiz.transform);
            piezaEnCurso = instancia.GetComponent<IArrastrableDesdeCatalogo>();
            piezaEnCurso.ComenzarArrastreDesdeCatalogo();
        }

        public void OnDrag(PointerEventData eventData)
        {
            piezaEnCurso.SeguirPuntero(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            piezaEnCurso.FinalizarArrastreDesdeCatalogo(eventData);
        }
    }
}
