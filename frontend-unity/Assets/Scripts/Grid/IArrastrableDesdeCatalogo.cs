using UnityEngine;
using UnityEngine.EventSystems;

namespace Sme.Grid
{
    public interface IArrastrableDesdeCatalogo
    {
        void ComenzarArrastreDesdeCatalogo();
        void SeguirPuntero(Vector2 posicionPantalla);
        void FinalizarArrastreDesdeCatalogo(PointerEventData eventData);
    }
}
