using Sme.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sme.Grid
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    public class PiezaView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler,
        IPiezaColocada, IArrastrableDesdeCatalogo
    {
        public CaraAcceso CaraAcceso => caraAcceso;
        public bool EsAccesible => esAccesible;

        public TipoPieza Tipo => TipoPieza.PLAZA;
        public string Orientacion => caraAcceso.ToString();
        public bool EsCrucePeatonal => false;
        public string SentidoVertical => null;

        public void Refrescar() { }

        [SerializeField] private Sprite spriteNormal;
        [SerializeField] private Sprite spriteAccesible;

        private CaraAcceso caraAcceso = CaraAcceso.NORTE;
        private CaraAcceso caraAccesoOriginal;
        private bool esAccesible;
        private CeldaView celdaAncla;
        private CeldaView celdaSecundaria;
        private bool estaArrastrando;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas canvasRaiz;
        private Image imagen;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            imagen = GetComponent<Image>();
            canvasRaiz = GetComponentInParent<Canvas>().rootCanvas;

            // Canvas propio para dibujarse encima de las celdas sin tocar el orden del GridLayoutGroup
            Canvas canvasPropio = gameObject.AddComponent<Canvas>();
            canvasPropio.overrideSorting = true;
            canvasPropio.sortingOrder = 1;

            // Sin esto el Canvas raíz deja de detectar la pieza y no se puede arrastrar
            gameObject.AddComponent<GraphicRaycaster>();
        }

        private void Update()
        {
            if (estaArrastrando && Input.GetKeyDown(KeyCode.R))
            {
                caraAcceso = (CaraAcceso)(((int)caraAcceso + 1) % 4);
                AplicarTamanioLibre();
            }
        }

        public void ColocarDesdeGuardado(CeldaView ancla, CaraAcceso orientacionGuardada, bool accesibleGuardado)
        {
            caraAcceso = orientacionGuardada;
            esAccesible = accesibleGuardado;
            ActualizarSprite();
            IntentarColocar(ancla);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (estaArrastrando || eventData.button != PointerEventData.InputButton.Right) return;

            string textoAccesible = esAccesible ? "Quitar accesibilidad" : "Marcar como accesible";
            MenuContextual.Mostrar(this, eventData.position,
                new MenuContextual.Opcion(textoAccesible, AlternarAccesibilidad),
                new MenuContextual.Opcion("Eliminar", Eliminar, esDestructiva: true));
        }

        public void AlternarAccesibilidad()
        {
            esAccesible = !esAccesible;
            ActualizarSprite();
            celdaAncla.MarcarAccesible(esAccesible);
        }

        private void Eliminar()
        {
            celdaAncla.Liberar();
            celdaSecundaria.Liberar();
            Destroy(gameObject);
        }

        private void ActualizarSprite()
        {
            imagen.sprite = esAccesible ? spriteAccesible : spriteNormal;
        }

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
                MensajesEditor.MostrarError("La plaza queda fuera de los límites de la grilla.");
                Destroy(gameObject);
                return;
            }

            if (!IntentarColocar(celdaDestino))
            {
                Destroy(gameObject);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            caraAccesoOriginal = caraAcceso;
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
                MensajesEditor.MostrarError("La plaza queda fuera de los límites de la grilla.");
                Destroy(gameObject);
                return;
            }

            CeldaView anclaOriginal = celdaAncla;
            if (!IntentarColocar(celdaDestino))
            {
                // También se revierte la rotación: si no, puede quedar inválida en la celda original
                caraAcceso = caraAccesoOriginal;
                IntentarColocar(anclaOriginal);
            }
        }

        public bool IntentarColocar(CeldaView nuevaAncla)
        {
            CeldaView nuevaSecundaria = ObtenerCeldaSecundaria(nuevaAncla);

            if (nuevaSecundaria == null)
            {
                MensajesEditor.MostrarError("La plaza queda fuera de los límites de la grilla.");
                return false;
            }

            if (nuevaAncla.Ocupada || nuevaSecundaria.Ocupada)
            {
                MensajesEditor.MostrarError("La celda está ocupada.");
                return false;
            }

            celdaAncla = nuevaAncla;
            celdaSecundaria = nuevaSecundaria;
            PosicionarSobreCeldas();
            celdaAncla.Ocupar(TipoPieza.PLAZA, caraAcceso);
            celdaSecundaria.Ocupar(TipoPieza.PLAZA, null);
            celdaAncla.MarcarAccesible(esAccesible);
            return true;
        }

        private CeldaView ObtenerCeldaSecundaria(CeldaView ancla)
        {
            (int deltaFila, int deltaColumna) = GrillaModelo.Delta(GrillaModelo.Opuesta(caraAcceso));
            return GrillaGenerador.ObtenerCelda(ancla.Piso, ancla.Fila + deltaFila, ancla.Columna + deltaColumna);
        }

        private void PosicionarSobreCeldas()
        {
            RectTransform anclaRect = celdaAncla.GetComponent<RectTransform>();
            RectTransform secundariaRect = celdaSecundaria.GetComponent<RectTransform>();
            Vector2 offsetHaciaSecundaria = secundariaRect.anchoredPosition - anclaRect.anchoredPosition;

            rectTransform.SetParent(celdaAncla.transform, false);
            rectTransform.anchoredPosition = offsetHaciaSecundaria / 2f;

            AplicarTamanioYRotacion();
        }

        private void AplicarTamanioLibre()
        {
            AplicarTamanioYRotacion();
        }

        private void AplicarTamanioYRotacion()
        {
            Vector2 tamCelda = GrillaGenerador.TamanioCelda;
            Vector2 espaciado = GrillaGenerador.Espaciado;

            rectTransform.sizeDelta = new Vector2(tamCelda.x, tamCelda.y * 2f + espaciado.y);
            rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f * (int)caraAcceso);
        }
    }
}
