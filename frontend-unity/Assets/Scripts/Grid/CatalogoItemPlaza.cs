using UnityEngine;
using UnityEngine.EventSystems;

namespace Sme.Grid
{
    // RF-13: origen de arrastre para colocar una Plaza nueva. El ícono del
    // catálogo es fijo (no se mueve ni se consume) — al empezar el arrastre se
    // instancia una pieza real (PiezaView), que maneja su propia orientación
    // (tecla R) y colocación. Este script solo reenvía la posición del puntero,
    // porque Unity solo llama IBeginDrag/IDrag/IEndDrag sobre el objeto donde
    // arrancó el gesto (el ícono), no sobre la pieza recién instanciada.
    public class CatalogoItemPlaza : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private GameObject prefabPlaza;

        private PiezaView piezaEnCurso;

        public void OnBeginDrag(PointerEventData eventData)
        {
            Canvas canvasRaiz = GetComponentInParent<Canvas>().rootCanvas;
            GameObject instancia = Instantiate(prefabPlaza, canvasRaiz.transform);
            piezaEnCurso = instancia.GetComponent<PiezaView>();
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
