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
                AplicarTamanioYOrientacion();
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
            AplicarTamanioYOrientacion();
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

        private CeldaView ObtenerCeldaSecundaria(CeldaView ancla)
        {
            // Asume que el GridLayoutGroup arranca en la esquina superior
            // izquierda con eje horizontal (default de Unity): fila creciente
            // = hacia abajo, columna creciente = hacia la derecha. Si en algún
            // momento se cambia el Start Corner / Start Axis del contenedor,
            // este mapeo hay que revisarlo.
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
            AplicarTamanioYOrientacion();

            Vector2 pasoEntreCentros = GrillaGenerador.TamanioCelda + GrillaGenerador.Espaciado;

            rectTransform.anchoredPosition = caraAcceso switch
            {
                CaraAcceso.NORTE => new Vector2(0f, pasoEntreCentros.y / 2f),
                CaraAcceso.SUR => new Vector2(0f, -pasoEntreCentros.y / 2f),
                CaraAcceso.ESTE => new Vector2(pasoEntreCentros.x / 2f, 0f),
                CaraAcceso.OESTE => new Vector2(-pasoEntreCentros.x / 2f, 0f),
                _ => Vector2.zero
            };
        }

        // Tamaño real (en unidades de la grilla) y flecha de orientación. Se usa
        // tanto para la pieza ya asentada como para el "fantasma" en pleno
        // arrastre (todavía sin celda) — por eso no depende de celdaAncla.
        private void AplicarTamanioYOrientacion()
        {
            Vector2 tamCelda = GrillaGenerador.TamanioCelda;
            Vector2 espaciado = GrillaGenerador.Espaciado;
            bool esVertical = caraAcceso == CaraAcceso.NORTE || caraAcceso == CaraAcceso.SUR;

            rectTransform.sizeDelta = esVertical
                ? new Vector2(tamCelda.x, tamCelda.y * 2f + espaciado.y)
                : new Vector2(tamCelda.x * 2f + espaciado.x, tamCelda.y);

            if (flechaAcceso != null)
            {
                flechaAcceso.localEulerAngles = new Vector3(0f, 0f, -90f * (int)caraAcceso);
            }
        }
    }
}
