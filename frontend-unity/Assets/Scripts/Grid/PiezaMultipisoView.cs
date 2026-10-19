using System.Collections.Generic;
using Sme.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sme.Grid
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    public class PiezaMultipisoView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerClickHandler, IPiezaColocada, IArrastrableDesdeCatalogo
    {
        [SerializeField] private TipoPieza tipo;
        [SerializeField] private SentidoVertical sentidoVertical;

        public TipoPieza Tipo => tipo;
        public string Orientacion => orientacion.ToString();
        public bool EsAccesible => false;
        public bool EsCrucePeatonal => false;
        public string SentidoVertical => tipo == TipoPieza.RAMPA ? sentidoVertical.ToString() : null;

        public void Refrescar() { }

        private CaraAcceso orientacion = CaraAcceso.NORTE;

        private int pisoEntrada;

        private CeldaView celdaPropia;

        private CaraAcceso orientacionOriginal;
        private int filaOriginal;
        private int columnaOriginal;

        private bool estaArrastrando;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas canvasRaiz;

        private static readonly Color ColorEtiquetaSube = new Color(0.4f, 0.9f, 0.5f, 1f);
        private static readonly Color ColorEtiquetaBaja = new Color(1f, 0.62f, 0.25f, 1f);
        private RectTransform etiquetaSentido;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            canvasRaiz = GetComponentInParent<Canvas>().rootCanvas;

            Canvas canvasPropio = gameObject.AddComponent<Canvas>();
            canvasPropio.overrideSorting = true;
            canvasPropio.sortingOrder = 1;
            gameObject.AddComponent<GraphicRaycaster>();

            if (tipo == TipoPieza.RAMPA)
            {
                CrearEtiquetaSentido();
            }
        }

        private void Update()
        {
            if (!estaArrastrando || tipo != TipoPieza.RAMPA || !Input.GetKeyDown(KeyCode.R)) return;

            orientacion = (CaraAcceso)(((int)orientacion + 1) % 4);
            AplicarRotacion();
        }

        public void ColocarDesdeGuardado(int pisoEntradaGuardado, int fila, int columna, CaraAcceso orientacionGuardada)
        {
            orientacion = orientacionGuardada;
            pisoEntrada = pisoEntradaGuardado;
            if (!IntentarColocar(fila, columna, pisoEntradaGuardado))
            {
                Destroy(gameObject);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (estaArrastrando || eventData.button != PointerEventData.InputButton.Right) return;

            MenuContextual.Mostrar(this, eventData.position, new MenuContextual.Opcion("Eliminar", Eliminar, esDestructiva: true));
        }

        private void Eliminar()
        {
            LevantarDeTodosLosPisos();
            Destroy(gameObject);
        }

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
                MensajesEditor.MostrarError("La pieza queda fuera de los límites de la grilla.");
                Destroy(gameObject);
                return;
            }

            pisoEntrada = tipo == TipoPieza.ESCALERA ? 0 : GrillaGenerador.PisoActual;
            if (!IntentarColocar(celdaDestino.Fila, celdaDestino.Columna, GrillaGenerador.PisoActual))
            {
                Destroy(gameObject);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            orientacionOriginal = orientacion;
            filaOriginal = celdaPropia.Fila;
            columnaOriginal = celdaPropia.Columna;

            LevantarDeTodosLosPisos();

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
                MensajesEditor.MostrarError("La pieza queda fuera de los límites de la grilla.");
                Destroy(gameObject);
                return;
            }

            if (!IntentarColocar(celdaDestino.Fila, celdaDestino.Columna, GrillaGenerador.PisoActual))
            {
                orientacion = orientacionOriginal;
                IntentarColocar(filaOriginal, columnaOriginal, GrillaGenerador.PisoActual);
            }
        }

        private bool IntentarColocar(int fila, int columna, int pisoDeEstaInstancia)
        {
            if (GrillaGenerador.CantidadPisos < 2)
            {
                MensajesEditor.MostrarError($"{NombrePieza()} requiere que el proyecto tenga más de un piso.");
                return false;
            }

            if (tipo == TipoPieza.RAMPA)
            {
                int pisoSalida = PisoSalidaDeRampa();
                if (pisoSalida >= GrillaGenerador.CantidadPisos)
                {
                    MensajesEditor.MostrarError("Una rampa que sube no puede colocarse en el último piso.");
                    return false;
                }
                if (pisoSalida < 0)
                {
                    MensajesEditor.MostrarError("Una rampa que baja no puede colocarse en planta baja.");
                    return false;
                }
            }

            List<int> pisos = PisosQueOcupa();
            foreach (int piso in pisos)
            {
                CeldaView celda = GrillaGenerador.ObtenerCelda(piso, fila, columna);
                if (celda == null)
                {
                    MensajesEditor.MostrarError("La pieza queda fuera de los límites de la grilla.");
                    return false;
                }
                if (celda.Ocupada)
                {
                    string mensaje = piso == pisoDeEstaInstancia
                        ? "La celda está ocupada."
                        : $"La celda está ocupada en {GrillaGenerador.NombrePiso(piso)}.";
                    MensajesEditor.MostrarError(mensaje);
                    return false;
                }
            }

            foreach (int piso in pisos)
            {
                CeldaView celda = GrillaGenerador.ObtenerCelda(piso, fila, columna);
                PiezaMultipisoView vista = piso == pisoDeEstaInstancia
                    ? this
                    : GrillaGenerador.CrearPiezaMultipiso(tipo, sentidoVertical);
                vista.Asentar(celda, orientacion, pisoEntrada);
            }
            return true;
        }

        private void Asentar(CeldaView celda, CaraAcceso nuevaOrientacion, int nuevoPisoEntrada)
        {
            orientacion = nuevaOrientacion;
            pisoEntrada = nuevoPisoEntrada;
            celdaPropia = celda;

            rectTransform.SetParent(celda.transform, false);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = GrillaGenerador.TamanioCelda;
            AplicarRotacion();

            if (tipo == TipoPieza.RAMPA)
            {
                celda.OcuparComoRampa(orientacion, sentidoVertical, celda.Piso == pisoEntrada);
            }
            else
            {
                celda.Ocupar(TipoPieza.ESCALERA, null);
            }
        }

        private void LevantarDeTodosLosPisos()
        {
            foreach (int piso in PisosQueOcupa())
            {
                CeldaView celda = GrillaGenerador.ObtenerCelda(piso, celdaPropia.Fila, celdaPropia.Columna);
                if (celda == null) continue;

                if (celda != celdaPropia)
                {
                    PiezaMultipisoView copia = celda.GetComponentInChildren<PiezaMultipisoView>(true);
                    if (copia != null)
                    {
                        Destroy(copia.gameObject);
                    }
                }
                celda.Liberar();
            }
        }

        private List<int> PisosQueOcupa()
        {
            var pisos = new List<int>();
            if (tipo == TipoPieza.ESCALERA)
            {
                for (int piso = 0; piso < GrillaGenerador.CantidadPisos; piso++)
                {
                    pisos.Add(piso);
                }
            }
            else
            {
                pisos.Add(pisoEntrada);
                pisos.Add(PisoSalidaDeRampa());
            }
            return pisos;
        }

        private int PisoSalidaDeRampa()
        {
            return sentidoVertical == Sme.Grid.SentidoVertical.SUBE ? pisoEntrada + 1 : pisoEntrada - 1;
        }

        private string NombrePieza()
        {
            return tipo == TipoPieza.ESCALERA ? "La escalera/salida de emergencia" : "La rampa";
        }

        private void AplicarRotacion()
        {
            rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f * (int)orientacion);
            if (etiquetaSentido != null)
            {
                etiquetaSentido.localEulerAngles = new Vector3(0f, 0f, 90f * (int)orientacion);
            }
        }

        private void CrearEtiquetaSentido()
        {
            bool sube = sentidoVertical == Sme.Grid.SentidoVertical.SUBE;

            GameObject etiquetaGO = new GameObject("EtiquetaSentido", typeof(RectTransform));
            etiquetaGO.transform.SetParent(transform, false);
            etiquetaSentido = etiquetaGO.GetComponent<RectTransform>();
            etiquetaSentido.anchorMin = new Vector2(0.1f, 0.34f);
            etiquetaSentido.anchorMax = new Vector2(0.9f, 0.66f);
            etiquetaSentido.offsetMin = Vector2.zero;
            etiquetaSentido.offsetMax = Vector2.zero;

            TMP_Text texto = etiquetaGO.AddComponent<TextMeshProUGUI>();
            texto.text = sube ? "SUBE" : "BAJA";
            texto.alignment = TextAlignmentOptions.Center;
            texto.fontStyle = FontStyles.Bold;
            texto.color = sube ? ColorEtiquetaSube : ColorEtiquetaBaja;
            texto.enableAutoSizing = true;
            texto.fontSizeMin = 8;
            texto.fontSizeMax = 18;
            texto.raycastTarget = false;
        }
    }
}
