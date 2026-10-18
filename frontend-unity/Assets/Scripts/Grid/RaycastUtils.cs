using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sme.Grid
{
    public static class RaycastUtils
    {
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

                if (resultado.gameObject == contenedorGrilla)
                {
                    return GrillaGenerador.ObtenerCeldaMasCercana(eventData.position, eventData.enterEventCamera);
                }
            }

            return null;
        }
    }
}
