using System.Collections.Generic;
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
    //
    // Solo CALLE tiene autotiling (editor/catalogo-piezas.md): el fondo lo
    // rota el autotiling según las conexiones, la flecha se orienta por
    // direccion — dos rotaciones distintas que no pueden compartir
    // transform. Por eso, solo para CALLE, fondo y flecha son hijos aparte
    // (ver fondo/flecha más abajo) en vez de usar el Image del objeto raíz:
    // si el fondo rotara el RectTransform raíz, arrastraría con él a la
    // flecha, que tiene que poder girar sola. Entrada, Salida y ZonaBiciMoto
    // no tienen ese problema (conservan siempre su propio sprite) y siguen
    // usando el Image del objeto raíz, rotando como una sola pieza.
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    public class PiezaSimpleView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerClickHandler, IPiezaColocada, IArrastrableDesdeCatalogo
    {
        [SerializeField] private TipoPieza tipo;

        // Solo se usan (y solo deberían asignarse en el prefab) para CALLE:
        // hijos con el fondo y la flecha, y los sprites de fondo según
        // conexiones (AutotilingCalle) o cruce peatonal (RF-17). El Image
        // del objeto raíz no se usa para Calle (Awake lo deshabilita) —
        // RequireComponent lo sigue exigiendo por las demás piezas, que sí
        // lo usan directo.
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

        // RF-19: celda bajo el puntero mientras se arrastra una pieza de
        // borde — hace falta conocerla para saber qué orientaciones son
        // válidas antes de soltar (ver ActualizarHover).
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
                // Calle dibuja con los hijos fondo/flecha, no con este Image
                // (ver comentario de la clase) — se apaga para que no se
                // vea un cuadrado sin sprite encima de los hijos.
                GetComponent<Image>().enabled = false;
            }

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
            if (!estaArrastrando || !Input.GetKeyDown(KeyCode.R)) return;

            // RF-19: sobre una celda de borde, ciclar solo entre las
            // orientaciones válidas para esa celda (una en un borde simple,
            // dos en una esquina) en vez de las 4 posibles.
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

        // Reconstruir una pieza ya guardada (abrir proyecto). esCrucePeatonal
        // solo aplica a CALLE (editor/catalogo-piezas.md) pero PiezaDto lo
        // guarda parejo para todas las piezas simples — se asigna igual acá,
        // sin efecto visual en las demás (SpriteParaForma solo lo mira para
        // Forma.Recta).
        public void ColocarDesdeGuardado(CeldaView ancla, CaraAcceso orientacionGuardada, bool esCrucePeatonalGuardado)
        {
            orientacion = orientacionGuardada;
            esCrucePeatonal = esCrucePeatonalGuardado;
            IntentarColocar(ancla);
        }

        // --- Clic derecho: cruce peatonal (RF-17, solo Calle) y eliminar (toda pieza) ---

        public void OnPointerClick(PointerEventData eventData)
        {
            if (estaArrastrando || eventData.button != PointerEventData.InputButton.Right) return;

            var opciones = new List<MenuContextual.Opcion>();

            // RF-19, bloqueante 8: no se permite marcar cruce peatonal sobre
            // una celda que funciona como cruce vehicular — la opción
            // directamente no aparece en el menú en vez de ofrecerla y
            // rechazarla.
            if (tipo == TipoPieza.CALLE && !GrafoCirculacion.EsCruce(celdaAncla.Modelo))
            {
                string texto = esCrucePeatonal ? "Quitar cruce peatonal" : "Marcar como cruce peatonal";
                opciones.Add(new MenuContextual.Opcion(texto, AlternarCrucePeatonal));
            }

            opciones.Add(new MenuContextual.Opcion("Eliminar", Eliminar));

            MenuContextual.Mostrar(this, eventData.position, opciones.ToArray());
        }

        // Llamado por MenuContextual al elegir "Marcar/Quitar cruce peatonal".
        public void AlternarCrucePeatonal()
        {
            esCrucePeatonal = !esCrucePeatonal;
            Refrescar();
        }

        // Llamado por MenuContextual al elegir "Eliminar" — mismo resultado
        // que soltar la pieza fuera de la grilla (OnEndDrag/FinalizarArrastre),
        // sin pasar por el arrastre.
        private void Eliminar()
        {
            celdaAncla.Liberar();
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
            ActualizarHover(eventData.position);
        }

        // RF-19: apenas el puntero pasa a estar sobre una celda distinta, si
        // la pieza es de las que solo van en el borde y la orientación
        // actual no es válida para esa celda, se ajusta sola — así al soltar
        // ya queda una orientación válida según el borde donde cayó, y la
        // tecla R (Update) tiene un conjunto de opciones para ciclar.
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

        // --- Colocación (compartida por ambos flujos de arrastre) ---

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

            // RF-19: exactamente una Entrada y una Salida en todo el
            // proyecto, y solo en planta baja. Si esta misma pieza ya
            // estaba colocada (se está moviendo), OnBeginDrag ya liberó su
            // celda antes de llegar acá, así que la búsqueda no la
            // encuentra a ella misma.
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

            // RF-19: orientación automática según el borde donde cayó. Si
            // ActualizarHover ya la había dejado válida (o el usuario la
            // eligió con R entre las opciones de esa celda) no cambia nada;
            // si no —por ejemplo, al reconstruir desde guardado— la fija a
            // la primera válida.
            if (RequiereBorde(tipo))
            {
                orientacion = AsegurarOrientacionValida(tipo, nuevaAncla.Fila, nuevaAncla.Columna, orientacion);
            }

            celdaAncla = nuevaAncla;
            // Posicionar antes de ocupar: Ocupar() avisa a las vecinas y a sí
            // misma (CeldaView.Refrescar), y Refrescar necesita encontrar
            // esta pieza como hija de la celda para poder recalcularse.
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

        // RF-19: la Salida apunta hacia afuera de la grilla; la Entrada y la
        // ZonaBicicletasMotos, hacia adentro (la opuesta a cada borde que la
        // celda toca). Un borde simple da una sola opción, una esquina da
        // dos — igual que GrillaGenerador.DireccionesHaciaAfuera.
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

        // Si actual ya es válida para esa celda, la conserva (para no pisar
        // lo que el usuario eligió con R en una esquina); si no, la primera
        // válida.
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

        // Para CALLE, rota solo la flecha según direccion — el fondo lo rota
        // Refrescar() según las conexiones (AutotilingCalle), por separado.
        // Para el resto, sigue rotando la pieza entera como una sola imagen.
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

        // IPiezaColocada: CeldaView llama esto en esta celda y en hasta 4
        // vecinas cada vez que algo se coloca, mueve o quita. Solo CALLE
        // tiene algo que recalcular: qué sprite de fondo le toca y con qué
        // rotación (AutotilingCalle), y ocultar la flecha cuando la celda
        // pasa a ser cruce (editor/grafo-circulacion.md,
        // editor/catalogo-piezas.md).
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

            // Dejar de ser Recta (Curva, CruceT o CrucePlus) es un cambio de
            // forma deliberado, no algo que se revierta solo — se apaga acá
            // para que no quede guardado un cruce peatonal invisible que
            // reaparezca solo si la celda vuelve a ser recta. Distinto de
            // editor/catalogo-piezas.md, que documentaba conservar el dato
            // al pasar a cruce vehicular — decisión revisada.
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
            // RF-17: la cebra reemplaza al fondo recto — no hay arte de
            // cruce peatonal para Curva/CruceT/CrucePlus, así que en esos
            // casos el dato queda guardado pero no se dibuja
            // (editor/catalogo-piezas.md: "la cebra deja de dibujarse hasta
            // que vuelva a ser segmento").
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
