using Sme.Managers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sme.Grid
{
    // RF-13: pieza Plaza ya colocada en la grilla. Se puede mover (arrastrar a
    // otra celda), rotar (clic, cambia la cara de acceso) o quitar (arrastrar
    // fuera de los límites de la grilla).
    //
    // Una Plaza ocupa 2 celdas pero se persiste como una sola fila en su celda
    // "ancla" (dominio/modelo-clases.md) — la segunda celda se infiere de
    // caraAcceso. Por eso el rectángulo cambia de forma (vertical u horizontal)
    // según la orientación, en vez de ser un cuadrado fijo: así se ve realmente
    // qué 2 celdas ocupa. Rotar cambia cuál es la segunda celda, así que
    // revalida la colocación igual que un movimiento.
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class PiezaView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform flechaAcceso;

        private CaraAcceso caraAcceso = CaraAcceso.NORTE;
        private CeldaView celdaAncla;
        private CeldaView celdaSecundaria;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas canvasRaiz;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            canvasRaiz = GetComponentInParent<Canvas>().rootCanvas;
        }

        // Intenta asentar la pieza con celdaAncla como ancla y la celda vecina
        // (según caraAcceso) como segunda celda. Si alguna de las dos está
        // ocupada o la segunda queda fuera de la grilla, no cambia nada y
        // devuelve false — quien llama decide qué hacer (revertir, destruir).
        public bool IntentarColocar(CeldaView nuevaAncla)
        {
            CeldaView nuevaSecundaria = ObtenerCeldaSecundaria(nuevaAncla);

            if (nuevaSecundaria == null)
            {
                MensajesEditor.Mostrar("La plaza queda fuera de los límites de la grilla.");
                return false;
            }

            if (nuevaAncla.Ocupada || nuevaSecundaria.Ocupada)
            {
                MensajesEditor.Mostrar("La celda está ocupada.");
                return false;
            }

            celdaAncla = nuevaAncla;
            celdaSecundaria = nuevaSecundaria;
            celdaAncla.Ocupar();
            celdaSecundaria.Ocupar();
            PosicionarSobreCeldas();
            return true;
        }

        private CeldaView ObtenerCeldaSecundaria(CeldaView ancla)
        {
            (int deltaFila, int deltaColumna) = caraAcceso switch
            {
                CaraAcceso.NORTE => (-1, 0),
                CaraAcceso.SUR => (1, 0),
                CaraAcceso.ESTE => (0, 1),
                CaraAcceso.OESTE => (0, -1),
                _ => (0, 0)
            };

            return GrillaGenerador.ObtenerCelda(ancla.Fila + deltaFila, ancla.Columna + deltaColumna);
        }

        // El rectángulo cubre desde el borde externo de la celda ancla hasta el
        // borde externo de la celda secundaria — no un cuadrado centrado, sino
        // un rectángulo de 2 celdas de largo en el eje de caraAcceso.
        private void PosicionarSobreCeldas()
        {
            rectTransform.SetParent(celdaAncla.transform, false);

            Vector2 tamCelda = GrillaGenerador.TamanioCelda;
            Vector2 espaciado = GrillaGenerador.Espaciado;
            Vector2 pasoEntreCentros = tamCelda + espaciado;

            switch (caraAcceso)
            {
                case CaraAcceso.NORTE:
                    rectTransform.sizeDelta = new Vector2(tamCelda.x, tamCelda.y * 2f + espaciado.y);
                    rectTransform.anchoredPosition = new Vector2(0f, pasoEntreCentros.y / 2f);
                    break;
                case CaraAcceso.SUR:
                    rectTransform.sizeDelta = new Vector2(tamCelda.x, tamCelda.y * 2f + espaciado.y);
                    rectTransform.anchoredPosition = new Vector2(0f, -pasoEntreCentros.y / 2f);
                    break;
                case CaraAcceso.ESTE:
                    rectTransform.sizeDelta = new Vector2(tamCelda.x * 2f + espaciado.x, tamCelda.y);
                    rectTransform.anchoredPosition = new Vector2(pasoEntreCentros.x / 2f, 0f);
                    break;
                case CaraAcceso.OESTE:
                    rectTransform.sizeDelta = new Vector2(tamCelda.x * 2f + espaciado.x, tamCelda.y);
                    rectTransform.anchoredPosition = new Vector2(-pasoEntreCentros.x / 2f, 0f);
                    break;
            }

            if (flechaAcceso != null)
            {
                flechaAcceso.localEulerAngles = new Vector3(0f, 0f, -90f * (int)caraAcceso);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            RotarCaraAcceso();
        }

        private void RotarCaraAcceso()
        {
            CaraAcceso anterior = caraAcceso;
            CeldaView secundariaAnterior = celdaSecundaria;

            caraAcceso = (CaraAcceso)(((int)caraAcceso + 1) % 4);
            CeldaView nuevaSecundaria = ObtenerCeldaSecundaria(celdaAncla);

            bool invalida = nuevaSecundaria == null
                || (nuevaSecundaria != secundariaAnterior && nuevaSecundaria.Ocupada);

            if (invalida)
            {
                caraAcceso = anterior;
                MensajesEditor.Mostrar(nuevaSecundaria == null
                    ? "La plaza queda fuera de los límites de la grilla."
                    : "La celda está ocupada.");
                return;
            }

            secundariaAnterior.Liberar();
            celdaSecundaria = nuevaSecundaria;
            celdaSecundaria.Ocupar();
            PosicionarSobreCeldas();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            celdaAncla.Liberar();
            celdaSecundaria.Liberar();
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

            CeldaView anclaOriginal = celdaAncla;
            if (!IntentarColocar(celdaDestino))
            {
                IntentarColocar(anclaOriginal);
            }
        }
    }
}
