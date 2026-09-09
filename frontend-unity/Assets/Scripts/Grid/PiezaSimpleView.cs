using Sme.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sme.Grid
{
    // RF-16: piezas de una sola celda (Calle, Entrada, Salida de vehículos,
    // Zona de bicicletas/motos) — se pueden colocar, mover, rotar y quitar,
    // igual que Plaza (PiezaView), pero sin celda secundaria: ocupan solo su
    // celda ancla. tipo se fija por prefab (un prefab por tipo, con su propio
    // color/sprite en el Inspector) — la lógica es la misma para las 4.
    //
    // RF-19: además de celda ocupada/fuera de la grilla (igual que Plaza),
    // Entrada, Salida y ZonaBicicletasMotos solo son válidas sobre el borde de
    // la grilla. Calle no tiene esa restricción — puede ir en cualquier celda
    // libre.
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    public class PiezaSimpleView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPiezaColocada, IArrastrableDesdeCatalogo
    {
        [SerializeField] private TipoPieza tipo;

        public TipoPieza Tipo => tipo;
        public string Orientacion => orientacion.ToString();
        public bool EsAccesible => false;
        public bool EsCrucePeatonal => false;

        private CaraAcceso orientacion = CaraAcceso.NORTE;
        private CaraAcceso orientacionOriginal;
        private CeldaView celdaAncla;
        private bool estaArrastrando;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas canvasRaiz;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            canvasRaiz = GetComponentInParent<Canvas>().rootCanvas;

            // Mismo motivo que PiezaView.Awake: Canvas propio para dibujarse
            // encima de las celdas sin alterar el orden de hermanos que
            // necesita el GridLayoutGroup.
            Canvas canvasPropio = gameObject.AddComponent<Canvas>();
            canvasPropio.overrideSorting = true;
            canvasPropio.sortingOrder = 1;
            gameObject.AddComponent<GraphicRaycaster>();
        }

        private void Update()
        {
            if (estaArrastrando && Input.GetKeyDown(KeyCode.R))
            {
                orientacion = (CaraAcceso)(((int)orientacion + 1) % 4);
                rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f * (int)orientacion);
            }
        }

        // Reconstruir una pieza ya guardada (abrir proyecto).
        public void ColocarDesdeGuardado(CeldaView ancla, CaraAcceso orientacionGuardada)
        {
            orientacion = orientacionGuardada;
            IntentarColocar(ancla);
        }

        // --- Arrastre desde el catálogo ---

        public void ComenzarArrastreDesdeCatalogo()
        {
            estaArrastrando = true;
            canvasGroup.blocksRaycasts = false;
            rectTransform.sizeDelta = GrillaGenerador.TamanioCelda;
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
                MensajesEditor.Mostrar("La pieza queda fuera de los límites de la grilla.");
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
            orientacionOriginal = orientacion;
            celdaAncla.Liberar();
            estaArrastrando = true;
            rectTransform.SetParent(canvasRaiz.transform, true);
            rectTransform.sizeDelta = GrillaGenerador.TamanioCelda;
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
                MensajesEditor.Mostrar("La pieza queda fuera de los límites de la grilla.");
                Destroy(gameObject);
                return;
            }

            CeldaView anclaOriginal = celdaAncla;
            if (!IntentarColocar(celdaDestino))
            {
                orientacion = orientacionOriginal;
                IntentarColocar(anclaOriginal);
            }
        }

        // --- Colocación (compartida por ambos flujos de arrastre) ---

        public bool IntentarColocar(CeldaView nuevaAncla)
        {
            if (nuevaAncla.Ocupada)
            {
                MensajesEditor.Mostrar("La celda está ocupada.");
                return false;
            }

            if (RequiereBorde(tipo) && !GrillaGenerador.EsCeldaDeBorde(nuevaAncla.Fila, nuevaAncla.Columna))
            {
                MensajesEditor.Mostrar("La pieza solo puede colocarse sobre el borde de la grilla.");
                return false;
            }

            celdaAncla = nuevaAncla;
            celdaAncla.Ocupar();
            PosicionarSobreCelda();
            return true;
        }

        private static bool RequiereBorde(TipoPieza tipo)
        {
            return tipo == TipoPieza.ENTRADA || tipo == TipoPieza.SALIDA || tipo == TipoPieza.ZONA_BICI_MOTO;
        }

        private void PosicionarSobreCelda()
        {
            rectTransform.SetParent(celdaAncla.transform, false);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = GrillaGenerador.TamanioCelda;
            rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f * (int)orientacion);
        }
    }
}
