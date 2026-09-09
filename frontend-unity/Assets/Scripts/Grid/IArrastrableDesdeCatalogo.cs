using UnityEngine;
using UnityEngine.EventSystems;

namespace Sme.Grid
{
    // Lo que CatalogoItem necesita de una pieza recién instanciada desde el
    // catálogo, sin conocer su clase concreta (PiezaView, PiezaSimpleView, ...).
    public interface IArrastrableDesdeCatalogo
    {
        void ComenzarArrastreDesdeCatalogo();
        void SeguirPuntero(Vector2 posicionPantalla);
        void FinalizarArrastreDesdeCatalogo(PointerEventData eventData);
    }
}
