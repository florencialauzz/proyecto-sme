using Sme.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sme.Grid
{
    // RF-13: pieza Plaza. Se puede colocar, mover (arrastrar a otra celda),
    // rotar y quitar (arrastrar fuera de los límites de la grilla).
    //
    // Una Plaza ocupa 2 celdas pero se persiste como una sola fila en su celda
    // "ancla" (dominio/modelo-clases.md) — la segunda celda se infiere de
    // caraAcceso. El arte ya viene dibujado en su forma final (1 celda de
    // ancho x 2 de alto, con la marca de acceso en el borde superior) y el
    // rectángulo nunca cambia de tamaño — para las cuatro orientaciones se
    // rota en pasos de 90°, igual que antes rotaba solo la flecha.
    //
    // La rotación se hace ANTES de soltar la pieza: mientras se sostiene el
    // arrastre (recién instanciada desde el catálogo, o una ya colocada que se
    // está moviendo), la tecla R cicla la orientación sin validar nada — la
    // validación real (celda ocupada / fuera de la grilla) ocurre una sola vez,
    // al soltar, en IntentarColocar. Así se puede orientar la pieza de una sola
    // vez en espacios ajustados, sin tener que colocarla y corregirla después.
    //
    // RF-14: clic derecho sobre una Plaza ya colocada abre MenuContextual,
    // que alterna esAccesible o elimina la pieza. Solo tiene sentido sobre
    // una pieza asentada — mientras se arrastra, OnPointerClick no se
    // dispara (el clic derecho no inicia arrastre, así que nunca hay
    // conflicto con IBeginDragHandler).
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    public class PiezaView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler,
        IPiezaColocada, IArrastrableDesdeCatalogo
    {
        // RF-21 lee esto para armar el JSON a guardar (PUT /grilla).
        public CaraAcceso CaraAcceso => caraAcceso;
        public bool EsAccesible => esAccesible;

        // IPiezaColocada: Plaza es la única pieza de 2 celdas en un mismo piso
        // y no tiene cruce peatonal (eso es propiedad de Calle, RF-17) ni
        // sentido vertical (eso es de Rampa, RF-18).
        public TipoPieza Tipo => TipoPieza.PLAZA;
        public string Orientacion => caraAcceso.ToString();
        public bool EsCrucePeatonal => false;
        public string SentidoVertical => null;

        // La Plaza no participa del conteo de conexiones ni del autotiling
        // (editor/grafo-circulacion.md) — no tiene nada que recalcular.
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

            // La pieza es hija de celdaAncla (para que RecolectarPiezas sepa
            // cuál es la celda ancla al guardar), pero eso ata su orden de
            // dibujo al orden de hermanos de las celdas dentro de
            // contenedorGrilla — que el GridLayoutGroup necesita intacto para
            // ubicarlas, no se puede reordenar para resolver esto. Un Canvas
            // propio con sorting forzado la dibuja siempre encima de las
            // celdas sin tocar esa jerarquía.
            Canvas canvasPropio = gameObject.AddComponent<Canvas>();
            canvasPropio.overrideSorting = true;
            canvasPropio.sortingOrder = 1;

            // Un Canvas propio re-registra los gráficos de la pieza bajo sí
            // mismo en vez de bajo canvasRaiz — sin un GraphicRaycaster acá, el
            // raycaster del Canvas raíz deja de verla y el arrastre se rompe.
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

        // Reconstruir una pieza ya guardada (abrir proyecto) — sin arrastre, se
        // asienta directo con la orientación y accesibilidad que ya tenía.
        public void ColocarDesdeGuardado(CeldaView ancla, CaraAcceso orientacionGuardada, bool accesibleGuardado)
        {
            caraAcceso = orientacionGuardada;
            esAccesible = accesibleGuardado;
            ActualizarSprite();
            IntentarColocar(ancla);
        }

        // --- RF-14: accesibilidad (clic derecho) — y eliminar, para toda pieza ---

        public void OnPointerClick(PointerEventData eventData)
        {
            if (estaArrastrando || eventData.button != PointerEventData.InputButton.Right) return;

            string textoAccesible = esAccesible ? "Quitar accesibilidad" : "Marcar como accesible";
            MenuContextual.Mostrar(this, eventData.position,
                new MenuContextual.Opcion(textoAccesible, AlternarAccesibilidad),
                new MenuContextual.Opcion("Eliminar", Eliminar));
        }

        // Llamado por MenuContextual al elegir "Marcar/Quitar accesibilidad".
        public void AlternarAccesibilidad()
        {
            esAccesible = !esAccesible;
            ActualizarSprite();
            celdaAncla.MarcarAccesible(esAccesible);
        }

        // Llamado por MenuContextual al elegir "Eliminar" — mismo resultado
        // que soltar la pieza fuera de la grilla (OnEndDrag), sin pasar por
        // el arrastre.
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
                MensajesEditor.MostrarError("La plaza queda fuera de los límites de la grilla.");
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
                // Soltada fuera de los límites de la grilla: quitar.
                MensajesEditor.MostrarError("La plaza queda fuera de los límites de la grilla.");
                Destroy(gameObject);
                return;
            }

            CeldaView anclaOriginal = celdaAncla;
            if (!IntentarColocar(celdaDestino))
            {
                // Si se rotó durante el arrastre, la orientación nueva puede
                // no ser válida en la celda original (por ejemplo, la segunda
                // celda con esa rotación cae sobre otra pieza) — se revierte
                // también la orientación, no solo la celda, para garantizar
                // que el estado al que se vuelve es el mismo que ya era
                // válido antes de empezar a arrastrar.
                caraAcceso = caraAccesoOriginal;
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
            // Posicionar antes de ocupar: Ocupar() avisa a las vecinas y a sí
            // misma (CeldaView.Refrescar), y Refrescar necesita encontrar esta
            // pieza como hija de la celda para poder recalcularse.
            PosicionarSobreCeldas();
            celdaAncla.Ocupar(TipoPieza.PLAZA, caraAcceso);
            celdaSecundaria.Ocupar(TipoPieza.PLAZA, null);
            // Ocupar deja la celda como no accesible; la accesibilidad es de
            // la pieza y viaja con ella al moverla (RF-20 la cuenta).
            celdaAncla.MarcarAccesible(esAccesible);
            return true;
        }

        // El ancla es siempre la celda por la que entra el vehículo
        // (editor/catalogo-piezas.md): el resto de la pieza (fondo) se
        // extiende en la dirección OPUESTA a caraAcceso. La celda vecina en
        // la dirección de caraAcceso es la que tiene que ser circulación
        // (alcanzabilidad, editor/grafo-circulacion.md) — no es esta pieza,
        // así que no se calcula acá.
        private CeldaView ObtenerCeldaSecundaria(CeldaView ancla)
        {
            (int deltaFila, int deltaColumna) = GrillaModelo.Delta(GrillaModelo.Opuesta(caraAcceso));
            return GrillaGenerador.ObtenerCelda(ancla.Piso, ancla.Fila + deltaFila, ancla.Columna + deltaColumna);
        }

        // El rectángulo se centra en el punto medio entre ambas celdas — eso
        // sigue midiéndose en vez de asumirse, para no depender de cómo esté
        // configurado el Start Corner/Start Axis del GridLayoutGroup. El
        // tamaño, en cambio, es siempre el mismo (ver AplicarRotacion): la
        // forma vertical u horizontal la da la rotación, no un resize.
        private void PosicionarSobreCeldas()
        {
            RectTransform anclaRect = celdaAncla.GetComponent<RectTransform>();
            RectTransform secundariaRect = celdaSecundaria.GetComponent<RectTransform>();
            Vector2 offsetHaciaSecundaria = secundariaRect.anchoredPosition - anclaRect.anchoredPosition;

            rectTransform.SetParent(celdaAncla.transform, false);
            rectTransform.anchoredPosition = offsetHaciaSecundaria / 2f;

            AplicarTamanioYRotacion();
        }

        // Mientras se arrastra (recién instanciada desde el catálogo, o una ya
        // colocada que se está moviendo) no hay celdas reales contra las que
        // medir el punto medio, pero el tamaño y la rotación son los mismos
        // que en PosicionarSobreCeldas — no dependen de la posición.
        private void AplicarTamanioLibre()
        {
            AplicarTamanioYRotacion();
        }

        // El arte ya viene dibujado vertical (1 celda de ancho x 2 de alto,
        // marca de acceso arriba = NORTE sin rotar), así que el tamaño nunca
        // cambia — las cuatro orientaciones son la misma pieza rotada en pasos
        // de 90°, con el mismo signo que antes usaba solo la flecha.
        private void AplicarTamanioYRotacion()
        {
            Vector2 tamCelda = GrillaGenerador.TamanioCelda;
            Vector2 espaciado = GrillaGenerador.Espaciado;

            rectTransform.sizeDelta = new Vector2(tamCelda.x, tamCelda.y * 2f + espaciado.y);
            rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f * (int)caraAcceso);
        }
    }
}
