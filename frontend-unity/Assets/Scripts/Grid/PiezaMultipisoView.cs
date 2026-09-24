using System.Collections.Generic;
using Sme.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sme.Grid
{
    // RF-18: piezas intermedias, Rampa y Escalera. Las dos son una pieza lógica
    // presente en varios pisos (editor/catalogo-piezas.md): se colocan una sola
    // vez y el sistema crea sus celdas en todos los pisos que ocupan, en la
    // misma fila y columna. Mover y quitar afectan a la pieza completa.
    //
    // - Rampa: ocupa el piso donde se coloca (el de entrada del vehículo) y el
    //   de arriba si sube, o el de abajo si baja. Rota con R para definir su
    //   flecha, que es la misma en las dos celdas.
    // - Escalera: ocupa la misma posición en todos los pisos, de planta baja
    //   al último. No rota.
    //
    // Hay una instancia de este script en cada celda que la pieza ocupa, todas
    // con los mismos datos, y ninguna guarda referencias a las otras: cuando
    // se arrastra una, busca a las demás por posición en los otros pisos, las
    // destruye, y al soltarla las vuelve a crear en los pisos que corresponden.
    //
    // tipo y sentidoVertical se fijan por prefab: hay un prefab para Rampa que
    // sube, otro para Rampa que baja y otro para Escalera, cada uno con su
    // ítem en el catálogo — así el usuario elige el sentido antes de soltar,
    // sin un diálogo aparte.
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    public class PiezaMultipisoView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerClickHandler, IPiezaColocada, IArrastrableDesdeCatalogo
    {
        [SerializeField] private TipoPieza tipo;
        [SerializeField] private SentidoVertical sentidoVertical;

        public TipoPieza Tipo => tipo;
        // La Escalera no tiene orientación; manda NORTE para que el campo del
        // contrato tenga un valor válido, y el backend lo descarta.
        public string Orientacion => orientacion.ToString();
        public bool EsAccesible => false;
        public bool EsCrucePeatonal => false;
        public string SentidoVertical => tipo == TipoPieza.RAMPA ? sentidoVertical.ToString() : null;

        // No participa del autotiling: Rampa y Escalera conservan siempre su
        // propio dibujo (editor/catalogo-piezas.md).
        public void Refrescar() { }

        private CaraAcceso orientacion = CaraAcceso.NORTE;

        // Rampa: el piso por donde entra el vehículo, que es donde el usuario
        // la colocó. Mover la rampa cambia su fila y columna, no sus pisos.
        // Escalera: siempre 0, porque arranca en planta baja.
        private int pisoEntrada;

        private CeldaView celdaPropia;

        private CaraAcceso orientacionOriginal;
        private int filaOriginal;
        private int columnaOriginal;

        private bool estaArrastrando;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas canvasRaiz;

        // Solo Rampa: etiqueta "SUBE"/"BAJA" que no gira con la pieza (ver
        // CrearEtiquetaSentido).
        // Tonos claros: van como color de letra directo sobre el asfalto
        // oscuro del sprite, sin fondo.
        private static readonly Color ColorEtiquetaSube = new Color(0.4f, 0.9f, 0.5f, 1f);
        private static readonly Color ColorEtiquetaBaja = new Color(1f, 0.62f, 0.25f, 1f);
        private RectTransform etiquetaSentido;

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

        // Reconstruir una pieza ya guardada (abrir proyecto, o rearmar la
        // grilla después de eliminar un piso). Si ya no entra —por ejemplo,
        // el proyecto quedó con un solo piso— se descarta.
        public void ColocarDesdeGuardado(int pisoEntradaGuardado, int fila, int columna, CaraAcceso orientacionGuardada)
        {
            orientacion = orientacionGuardada;
            pisoEntrada = pisoEntradaGuardado;
            if (!IntentarColocar(fila, columna, pisoEntradaGuardado))
            {
                Destroy(gameObject);
            }
        }

        // --- Clic derecho: eliminar ---

        public void OnPointerClick(PointerEventData eventData)
        {
            if (estaArrastrando || eventData.button != PointerEventData.InputButton.Right) return;

            MenuContextual.Mostrar(this, eventData.position, new MenuContextual.Opcion("Eliminar", Eliminar));
        }

        // Llamado por MenuContextual al elegir "Eliminar" — quita la pieza de
        // todos los pisos, igual que soltarla fuera de la grilla.
        private void Eliminar()
        {
            LevantarDeTodosLosPisos();
            Destroy(gameObject);
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
                MensajesEditor.MostrarError("La pieza queda fuera de los límites de la grilla.");
                Destroy(gameObject);
                return;
            }

            // El piso donde se suelta es el de entrada del vehículo.
            pisoEntrada = tipo == TipoPieza.ESCALERA ? 0 : GrillaGenerador.PisoActual;
            if (!IntentarColocar(celdaDestino.Fila, celdaDestino.Columna, GrillaGenerador.PisoActual))
            {
                Destroy(gameObject);
            }
        }

        // --- Arrastre de una pieza ya colocada (mover o quitar) ---

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
                // Soltada fuera de los límites de la grilla: quitar. Las
                // celdas de todos los pisos ya se liberaron en OnBeginDrag.
                MensajesEditor.MostrarError("La pieza queda fuera de los límites de la grilla.");
                Destroy(gameObject);
                return;
            }

            // Esta instancia es la del piso que se está mirando — sigue siendo
            // uno de los pisos de la pieza, porque mover no cambia los pisos.
            if (!IntentarColocar(celdaDestino.Fila, celdaDestino.Columna, GrillaGenerador.PisoActual))
            {
                orientacion = orientacionOriginal;
                IntentarColocar(filaOriginal, columnaOriginal, GrillaGenerador.PisoActual);
            }
        }

        // --- Colocación (compartida por todos los flujos) ---

        // Valida la colocación en todos los pisos que la pieza ocupa y, si
        // es válida, la asienta en cada uno: esta instancia en
        // pisoDeEstaInstancia, y una copia nueva en cada uno de los demás.
        // Si algo no es válido no cambia nada y devuelve false — quien llama
        // decide qué hacer (revertir, destruir).
        private bool IntentarColocar(int fila, int columna, int pisoDeEstaInstancia)
        {
            // RF-19, bloqueante 5: Escalera y Rampa solo si hay más de un piso.
            if (GrillaGenerador.CantidadPisos < 2)
            {
                MensajesEditor.MostrarError($"{NombrePieza()} requiere que el proyecto tenga más de un piso.");
                return false;
            }

            // RF-19, bloqueante 7: rampa con destino inexistente.
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

            // RF-19, bloqueantes 1 y 6: todas las celdas que va a ocupar, en
            // todos los pisos, tienen que estar libres — no solo la del piso
            // que se está mirando.
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

        // Deja esta instancia como la vista de la pieza en una celda concreta
        // y ocupa esa celda del modelo.
        private void Asentar(CeldaView celda, CaraAcceso nuevaOrientacion, int nuevoPisoEntrada)
        {
            orientacion = nuevaOrientacion;
            pisoEntrada = nuevoPisoEntrada;
            celdaPropia = celda;

            // Posicionar antes de ocupar: Ocupar() avisa a las vecinas y a sí
            // misma (CeldaView.Refrescar), y Refrescar necesita encontrar
            // esta pieza como hija de la celda.
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

        // Libera las celdas de la pieza en todos los pisos y destruye las
        // copias de los pisos que no son el de esta instancia.
        private void LevantarDeTodosLosPisos()
        {
            foreach (int piso in PisosQueOcupa())
            {
                CeldaView celda = GrillaGenerador.ObtenerCelda(piso, celdaPropia.Fila, celdaPropia.Columna);
                if (celda == null) continue;

                if (celda != celdaPropia)
                {
                    // includeInactive: la copia está en un piso oculto.
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

        // Nombre completo del enum: dentro de esta clase, SentidoVertical a
        // secas es la propiedad string de IPiezaColocada.
        private int PisoSalidaDeRampa()
        {
            return sentidoVertical == Sme.Grid.SentidoVertical.SUBE ? pisoEntrada + 1 : pisoEntrada - 1;
        }

        private string NombrePieza()
        {
            return tipo == TipoPieza.ESCALERA ? "La escalera" : "La rampa";
        }

        // La Escalera no rota: siempre queda con orientacion NORTE (0°). La
        // etiqueta de sentido gira en contra de la pieza para quedar siempre
        // derecha.
        private void AplicarRotacion()
        {
            rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f * (int)orientacion);
            if (etiquetaSentido != null)
            {
                etiquetaSentido.localEulerAngles = new Vector3(0f, 0f, 90f * (int)orientacion);
            }
        }

        // Los prefabs de rampa que sube y que baja usan el mismo sprite: sus
        // chevrones giran con la pieza y dicen por dónde sale el vehículo en
        // el plano, no si sube o baja (con chevrones distintos, una rampa que
        // sube rotada 180° se vería igual que una que baja). Esta etiqueta
        // ("SUBE"/"BAJA", letras de color sin fondo) es lo que las distingue
        // una vez colocadas. Está centrada en la celda, así que al contrarrotarla
        // (AplicarRotacion) queda derecha y en el mismo lugar con cualquier
        // orientación. No recibe raycasts, para no interferir con el arrastre.
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
