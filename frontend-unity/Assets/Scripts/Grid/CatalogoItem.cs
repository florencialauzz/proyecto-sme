using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Sme.Grid
{
    public class CatalogoItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
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
