using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sme.Grid
{
    // Compartido entre las piezas: qué celda hay bajo el puntero, tanto al
    // soltar el arrastre como, para las piezas de borde (RF-19), mientras se
    // arrastra, así se sabe qué orientación es válida antes de soltar.
    public static class RaycastUtils
    {
        // Mismo raycast que BuscarCeldaBajoPuntero(PointerEventData), pero
        // sin depender de haber recibido un evento de Unity — sirve para
        // consultar la celda bajo el puntero en cualquier momento del
        // arrastre, no solo en OnDrag/OnEndDrag.
        public static CeldaView BuscarCeldaBajoPuntero(Vector2 posicionPantalla)
        {
            return BuscarCeldaBajoPuntero(new PointerEventData(EventSystem.current) { position = posicionPantalla });
        }

        public static CeldaView BuscarCeldaBajoPuntero(PointerEventData eventData)
        {
            var resultados = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, resultados);

            GameObject contenedorGrilla = GrillaGenerador.ContenedorGrillaGameObject;

            foreach (RaycastResult resultado in resultados)
            {
                CeldaView celda = resultado.gameObject.GetComponent<CeldaView>();
                if (celda != null)
                {
                    return celda;
                }

                // Cayó en el hueco de separación entre celdas: ahí no hay
                // CeldaView, solo el catcher transparente del contenedor
                // (ver Editor.unity) — redondear al hueco a la celda más
                // cercana en vez de tratarlo como fuera de la grilla.
                if (resultado.gameObject == contenedorGrilla)
                {
                    return GrillaGenerador.ObtenerCeldaMasCercana(eventData.position, eventData.enterEventCamera);
                }
            }

            return null;
        }
    }
}
