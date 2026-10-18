using System.Collections.Generic;
using Sme.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sme.Grid
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    public class PiezaSimpleView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerClickHandler, IPiezaColocada, IArrastrableDesdeCatalogo
    {
        [SerializeField] private TipoPieza tipo;

        [SerializeField] private RectTransform fondo;
        [SerializeField] private RectTransform flecha;
        [SerializeField] private Sprite spriteRecta;
        [SerializeField] private Sprite spriteCurva;
        [SerializeField] private Sprite spriteCruceT;
        [SerializeField] private Sprite spriteCrucePlus;
        [SerializeField] private Sprite spriteCrucePeatonal;

        public TipoPieza Tipo => tipo;
        public string Orientacion => orientacion.ToString();
        public bool EsAccesible => false;
        public bool EsCrucePeatonal => esCrucePeatonal;
        public string SentidoVertical => null;

        private CaraAcceso orientacion = CaraAcceso.NORTE;
        private bool esCrucePeatonal;
        private CaraAcceso orientacionOriginal;
        private CeldaView celdaAncla;

        private CeldaView celdaHover;

        private bool estaArrastrando;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas canvasRaiz;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            canvasRaiz = GetComponentInParent<Canvas>().rootCanvas;

            if (tipo == TipoPieza.CALLE)
            {
                GetComponent<Image>().enabled = false;
            }

            Canvas canvasPropio = gameObject.AddComponent<Canvas>();
            canvasPropio.overrideSorting = true;
            canvasPropio.sortingOrder = 1;
            gameObject.AddComponent<GraphicRaycaster>();
        }

        private void Update()
        {
            if (!estaArrastrando || !Input.GetKeyDown(KeyCode.R)) return;

            if (RequiereBorde(tipo) && celdaHover != null)
            {
                CaraAcceso[] validas = OrientacionesValidas(tipo, celdaHover.Fila, celdaHover.Columna);
                if (validas.Length > 0)
                {
                    int indiceActual = System.Array.IndexOf(validas, orientacion);
                    orientacion = validas[(indiceActual + 1) % validas.Length];
                    AplicarRotacion();
                    return;
                }
            }

            orientacion = (CaraAcceso)(((int)orientacion + 1) % 4);
            AplicarRotacion();
        }

        public void ColocarDesdeGuardado(CeldaView ancla, CaraAcceso orientacionGuardada, bool esCrucePeatonalGuardado)
        {
            orientacion = orientacionGuardada;
            esCrucePeatonal = esCrucePeatonalGuardado;
            IntentarColocar(ancla);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (estaArrastrando || eventData.button != PointerEventData.InputButton.Right) return;

            var opciones = new List<MenuContextual.Opcion>();

            if (tipo == TipoPieza.CALLE && !GrafoCirculacion.EsCruce(celdaAncla.Modelo))
            {
                string texto = esCrucePeatonal ? "Quitar cruce peatonal" : "Marcar como cruce peatonal";
                opciones.Add(new MenuContextual.Opcion(texto, AlternarCrucePeatonal));
            }

            opciones.Add(new MenuContextual.Opcion("Eliminar", Eliminar, esDestructiva: true));

            MenuContextual.Mostrar(this, eventData.position, opciones.ToArray());
        }

        public void AlternarCrucePeatonal()
        {
            esCrucePeatonal = !esCrucePeatonal;
            Refrescar();
        }

        private void Eliminar()
        {
            celdaAncla.Liberar();
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
            ActualizarHover(posicionPantalla);
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

            if (!IntentarColocar(celdaDestino))
            {
                Destroy(gameObject);
            }
        }

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
            ActualizarHover(eventData.position);
        }

        private void ActualizarHover(Vector2 posicionPantalla)
        {
            if (!RequiereBorde(tipo)) return;

            CeldaView nuevoHover = RaycastUtils.BuscarCeldaBajoPuntero(posicionPantalla);
            if (nuevoHover == celdaHover) return;

            celdaHover = nuevoHover;
            if (celdaHover == null) return;

            CaraAcceso orientacionValida = AsegurarOrientacionValida(tipo, celdaHover.Fila, celdaHover.Columna, orientacion);
            if (orientacionValida != orientacion)
            {
                orientacion = orientacionValida;
                AplicarRotacion();
            }
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

            CeldaView anclaOriginal = celdaAncla;
            if (!IntentarColocar(celdaDestino))
            {
                orientacion = orientacionOriginal;
                IntentarColocar(anclaOriginal);
            }
        }

        public bool IntentarColocar(CeldaView nuevaAncla)
        {
            if (nuevaAncla.Ocupada)
            {
                MensajesEditor.MostrarError("La celda está ocupada.");
                return false;
            }

            if (RequiereBorde(tipo) && !GrillaGenerador.EsCeldaDeBorde(nuevaAncla.Fila, nuevaAncla.Columna))
            {
                MensajesEditor.MostrarError("La pieza solo puede colocarse sobre el borde de la grilla.");
                return false;
            }

            if (RequiereUnicidad(tipo) && nuevaAncla.Piso != 0)
            {
                string nombre = tipo == TipoPieza.ENTRADA ? "La Entrada" : "La Salida";
                MensajesEditor.MostrarError($"{nombre} solo puede colocarse en planta baja.");
                return false;
            }

            if (RequiereUnicidad(tipo) && GrillaModelo.ExisteOcupadaDeTipo(tipo))
            {
                string nombre = tipo == TipoPieza.ENTRADA ? "una Entrada" : "una Salida";
                MensajesEditor.MostrarError($"Ya existe {nombre} en el proyecto.");
                return false;
            }

            if (RequiereBorde(tipo))
            {
                orientacion = AsegurarOrientacionValida(tipo, nuevaAncla.Fila, nuevaAncla.Columna, orientacion);
            }

            celdaAncla = nuevaAncla;
            PosicionarSobreCelda();
            celdaAncla.Ocupar(tipo, orientacion);
            return true;
        }

        private static bool RequiereBorde(TipoPieza tipo)
        {
            return tipo == TipoPieza.ENTRADA || tipo == TipoPieza.SALIDA || tipo == TipoPieza.ZONA_BICI_MOTO;
        }

        private static bool RequiereUnicidad(TipoPieza tipo)
        {
            return tipo == TipoPieza.ENTRADA || tipo == TipoPieza.SALIDA;
        }

        private static CaraAcceso[] OrientacionesValidas(TipoPieza tipo, int fila, int columna)
        {
            CaraAcceso[] haciaAfuera = GrillaGenerador.DireccionesHaciaAfuera(fila, columna);
            if (tipo == TipoPieza.SALIDA) return haciaAfuera;

            var haciaAdentro = new CaraAcceso[haciaAfuera.Length];
            for (int i = 0; i < haciaAfuera.Length; i++)
            {
                haciaAdentro[i] = GrillaModelo.Opuesta(haciaAfuera[i]);
            }
            return haciaAdentro;
        }

        private static CaraAcceso AsegurarOrientacionValida(TipoPieza tipo, int fila, int columna, CaraAcceso actual)
        {
            CaraAcceso[] validas = OrientacionesValidas(tipo, fila, columna);
            if (validas.Length == 0) return actual;
            return System.Array.IndexOf(validas, actual) >= 0 ? actual : validas[0];
        }

        private void PosicionarSobreCelda()
        {
            rectTransform.SetParent(celdaAncla.transform, false);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = GrillaGenerador.TamanioCelda;
            AplicarRotacion();
        }

        private void AplicarRotacion()
        {
            if (tipo == TipoPieza.CALLE)
            {
                if (flecha != null)
                {
                    flecha.localEulerAngles = new Vector3(0f, 0f, -90f * (int)orientacion);
                }
            }
            else
            {
                rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f * (int)orientacion);
            }
        }

        public void Refrescar()
        {
            if (tipo != TipoPieza.CALLE || celdaAncla == null) return;

            bool esCruce = GrafoCirculacion.EsCruce(celdaAncla.Modelo);
            if (flecha != null)
            {
                flecha.gameObject.SetActive(!esCruce);
            }

            if (fondo == null) return;

            (AutotilingCalle.Forma forma, int pasos) = AutotilingCalle.Elegir(celdaAncla.Modelo);

            if (forma != AutotilingCalle.Forma.Recta)
            {
                esCrucePeatonal = false;
            }

            Image imagenFondo = fondo.GetComponent<Image>();
            if (imagenFondo != null)
            {
                imagenFondo.sprite = SpriteParaForma(forma);
            }

            fondo.localEulerAngles = new Vector3(0f, 0f, -90f * pasos);
        }

        private Sprite SpriteParaForma(AutotilingCalle.Forma forma)
        {
            if (forma == AutotilingCalle.Forma.Recta && esCrucePeatonal)
            {
                return spriteCrucePeatonal;
            }

            return forma switch
            {
                AutotilingCalle.Forma.Recta => spriteRecta,
                AutotilingCalle.Forma.Curva => spriteCurva,
                AutotilingCalle.Forma.CruceT => spriteCruceT,
                AutotilingCalle.Forma.CrucePlus => spriteCrucePlus,
                _ => spriteRecta
            };
        }
    }
}
