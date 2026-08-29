using Sme.Managers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sme.Grid
{
    // RF-13: pieza Plaza. Se puede colocar, mover (arrastrar a otra celda),
    // rotar y quitar (arrastrar fuera de los límites de la grilla).
    //
    // Una Plaza ocupa 2 celdas pero se persiste como una sola fila en su celda
    // "ancla" (dominio/modelo-clases.md) — la segunda celda se infiere de
    // caraAcceso. Por eso el rectángulo es siempre de 2 celdas de largo (nunca
    // un cuadrado de 1), y cambia de forma (vertical u horizontal) según la
    // orientación.
    //
    // La rotación se hace ANTES de soltar la pieza: mientras se sostiene el
    // arrastre (recién instanciada desde el catálogo, o una ya colocada que se
    // está moviendo), la tecla R cicla la orientación sin validar nada — la
    // validación real (celda ocupada / fuera de la grilla) ocurre una sola vez,
    // al soltar, en IntentarColocar. Así se puede orientar la pieza de una sola
    // vez en espacios ajustados, sin tener que colocarla y corregirla después.
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class PiezaView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform flechaAcceso;

        // RF-21 lee esto para armar el JSON a guardar (PUT /grilla).
        public CaraAcceso CaraAcceso => caraAcceso;

        private CaraAcceso caraAcceso = CaraAcceso.NORTE;
        private CeldaView celdaAncla;
        private CeldaView celdaSecundaria;
        private bool estaArrastrando;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas canvasRaiz;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            canvasRaiz = GetComponentInParent<Canvas>().rootCanvas;
        }

        private void Update()
        {
            if (estaArrastrando && Input.GetKeyDown(KeyCode.R))
            {
                caraAcceso = (CaraAcceso)(((int)caraAcceso + 1) % 4);
                AplicarTamanioLibre();
            }
        }

        // Reconstruir una pieza ya guardada (abrir proyecto) — sin arrastre, se
        // asienta directo con la orientación que ya tenía.
        public void ColocarDesdeGuardado(CeldaView ancla, CaraAcceso orientacionGuardada)
        {
            caraAcceso = orientacionGuardada;
            IntentarColocar(ancla);
        }

        // --- Arrastre desde el catálogo (pieza recién instanciada, todavía sin celda) ---

        public void ComenzarArrastreDesdeCatalogo()
        {
            estaArrastrando = true;
            canvasGroup.blocksRaycasts = false;
            AplicarTamanioLibre();
        }

        public void SeguirPuntero(Vector2 posicionPantalla)
        {
            rectTransform.position = posicionPantalla;
        }

        public void FinalizarArrastreDesdeCatalogo(PointerEventData eventData)
        {
            estaArrastrando = false;
            canvasGroup.blocksRaycasts = true;
            CeldaView celdaDestino = RaycastUtils.BuscarCeldaBajoPuntero(eventData);

            if (celdaDestino == null)
            {
                MensajesEditor.Mostrar("La plaza queda fuera de los límites de la grilla.");
                Destroy(gameObject);
                return;
            }

            if (!IntentarColocar(celdaDestino))
            {
                Destroy(gameObject);
            }
        }

        // --- Arrastre de una pieza ya colocada (mover o quitar) ---

        public void OnBeginDrag(PointerEventData eventData)
        {
            celdaAncla.Liberar();
            celdaSecundaria.Liberar();
            estaArrastrando = true;
            rectTransform.SetParent(canvasRaiz.transform, true);
            canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            rectTransform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            estaArrastrando = false;
            canvasGroup.blocksRaycasts = true;
            CeldaView celdaDestino = RaycastUtils.BuscarCeldaBajoPuntero(eventData);

            if (celdaDestino == null)
            {
                // Soltada fuera de los límites de la grilla: quitar.
                MensajesEditor.Mostrar("La plaza queda fuera de los límites de la grilla.");
                Destroy(gameObject);
                return;
            }

            CeldaView anclaOriginal = celdaAncla;
            if (!IntentarColocar(celdaDestino))
            {
                IntentarColocar(anclaOriginal);
            }
        }

        // --- Colocación (compartida por ambos flujos de arrastre) ---

        // Intenta asentar la pieza con nuevaAncla como ancla y la celda vecina
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

        // Define qué celda es la vecina en cada dirección — es una regla de
        // topología (fila/columna), no una posición en pantalla. No necesita
        // asumir hacia dónde "apunta" cada dirección visualmente: eso lo
        // resuelve PosicionarSobreCeldas midiendo las celdas reales.
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
        // borde externo de la celda secundaria. En vez de asumir a qué dirección
        // de pantalla corresponde cada CaraAcceso (eso dependía de cómo esté
        // configurado el Start Corner/Start Axis del GridLayoutGroup, y se
        // rompía si no coincidía con lo asumido), se mide la posición real de
        // ambas celdas — así funciona sin importar esa configuración.
        private void PosicionarSobreCeldas()
        {
            RectTransform anclaRect = celdaAncla.GetComponent<RectTransform>();
            RectTransform secundariaRect = celdaSecundaria.GetComponent<RectTransform>();
            Vector2 offsetHaciaSecundaria = secundariaRect.anchoredPosition - anclaRect.anchoredPosition;

            rectTransform.SetParent(celdaAncla.transform, false);
            rectTransform.anchoredPosition = offsetHaciaSecundaria / 2f;

            Vector2 tamCelda = GrillaGenerador.TamanioCelda;
            Vector2 espaciado = GrillaGenerador.Espaciado;
            bool esHorizontal = Mathf.Abs(offsetHaciaSecundaria.x) > Mathf.Abs(offsetHaciaSecundaria.y);

            rectTransform.sizeDelta = esHorizontal
                ? new Vector2(tamCelda.x * 2f + espaciado.x, tamCelda.y)
                : new Vector2(tamCelda.x, tamCelda.y * 2f + espaciado.y);

            AplicarFlecha();
        }

        // Tamaño aproximado mientras se arrastra y todavía no hay celdas reales
        // contra las que medir (recién instanciada desde el catálogo, o una ya
        // colocada que se está moviendo) — es solo la vista previa. El tamaño
        // definitivo se recalcula en PosicionarSobreCeldas al soltar.
        private void AplicarTamanioLibre()
        {
            Vector2 tamCelda = GrillaGenerador.TamanioCelda;
            Vector2 espaciado = GrillaGenerador.Espaciado;
            bool esVertical = caraAcceso == CaraAcceso.NORTE || caraAcceso == CaraAcceso.SUR;

            rectTransform.sizeDelta = esVertical
                ? new Vector2(tamCelda.x, tamCelda.y * 2f + espaciado.y)
                : new Vector2(tamCelda.x * 2f + espaciado.x, tamCelda.y);

            AplicarFlecha();
        }

        private void AplicarFlecha()
        {
            if (flechaAcceso != null)
            {
                flechaAcceso.localEulerAngles = new Vector3(0f, 0f, -90f * (int)caraAcceso);
            }
        }
    }
}
