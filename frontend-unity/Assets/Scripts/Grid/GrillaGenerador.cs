using System;
using System.Collections.Generic;
using Sme.Managers;
using Sme.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Sme.Grid
{
    // RF-12: Generar grilla. Al cargar la escena Editor, instancia una grilla
    // vacía de filasGrilla x columnasGrilla por cada piso del proyecto,
    // usando los datos guardados en ProyectoManager. El posicionamiento lo
    // resuelve el GridLayoutGroup del contenedor, asignado en el Inspector —
    // este script solo instancia las celdas en orden de fila y columna.
    //
    // Si el proyecto se está reabriendo (no es uno recién creado), después
    // reconstruye las piezas guardadas (ProyectoManager.PiezasACargar).
    //
    // RF-18: cada piso es una copia de contenedorGrilla (que queda oculto,
    // como plantilla), y se ve uno solo a la vez — SelectorPisos cambia cuál.
    // Las celdas de los pisos ocultos siguen existiendo: Rampa y Escalera
    // ocupan celdas en pisos que no son el que se está mirando.
    public class GrillaGenerador : MonoBehaviour
    {
        [SerializeField] private RectTransform contenedorGrilla;
        [SerializeField] private GameObject prefabCelda;
        [SerializeField] private GameObject prefabPlaza;
        [SerializeField] private GameObject prefabCalle;
        [SerializeField] private GameObject prefabEntrada;
        [SerializeField] private GameObject prefabSalida;
        [SerializeField] private GameObject prefabZonaBiciMoto;
        [SerializeField] private GameObject prefabRampaSube;
        [SerializeField] private GameObject prefabRampaBaja;
        [SerializeField] private GameObject prefabEscalera;

        // Registro estático de celdas por posición: lo necesita PiezaView (RF-13)
        // para encontrar la celda vecina de una pieza de 2 celdas, como Plaza
        // (dominio/modelo-clases.md), y PiezaMultipisoView (RF-18) para
        // encontrar la misma posición en otro piso. Tamaño y espaciado también
        // se exponen acá para que las piezas no hardcodeen valores que se
        // configuran en el GridLayoutGroup del Inspector.
        private static readonly Dictionary<(int piso, int fila, int columna), CeldaView> celdas = new();
        private static readonly List<RectTransform> contenedoresPorPiso = new();

        public static Vector2 TamanioCelda { get; private set; }
        public static Vector2 Espaciado { get; private set; }
        public static int PisoActual { get; private set; }
        public static int CantidadPisos => GrillaModelo.CantidadPisos;

        // SelectorPisos se suscribe para rearmar sus botones cada vez que
        // cambia el piso visible o la cantidad de pisos.
        public static event Action AlCambiarPisos;

        private static int filasGrilla;
        private static int columnasGrilla;
        private static GrillaGenerador instancia;
        private static Transform canvasRaiz;

        // RaycastUtils la usa para reconocer el catcher transparente del
        // contenedor (ver Editor.unity) entre los resultados del raycast —
        // solo el del piso visible, que es el único que recibe raycasts.
        public static GameObject ContenedorGrillaGameObject =>
            ContenedorActual != null ? ContenedorActual.gameObject : null;

        private static RectTransform ContenedorActual =>
            PisoActual < contenedoresPorPiso.Count ? contenedoresPorPiso[PisoActual] : null;

        private void Start()
        {
            // Las listas estáticas sobreviven a la recarga de la escena (por
            // ejemplo, Editor → Configuración → Editor), pero los contenedores
            // y celdas que tenían ya los destruyó Unity al descargar la escena
            // anterior. Se vacían sin tocarlos: no hay nada que destruir.
            contenedoresPorPiso.Clear();
            celdas.Clear();

            instancia = this;
            canvasRaiz = contenedorGrilla.GetComponentInParent<Canvas>().rootCanvas.transform;
            filasGrilla = ProyectoManager.FilasGrilla;
            columnasGrilla = ProyectoManager.ColumnasGrilla;

            // Fija la cantidad de columnas para que el GridLayoutGroup no dependa
            // del ancho del contenedor para decidir dónde wrappear la fila. Se
            // configura en la plantilla, así cada piso la copia ya configurada.
            GridLayoutGroup layout = contenedorGrilla.GetComponent<GridLayoutGroup>();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = columnasGrilla;
            TamanioCelda = layout.cellSize;
            Espaciado = layout.spacing;

            contenedorGrilla.gameObject.SetActive(false);

            // Un proyecto sin configuración todavía (cantidadPisos null en el
            // backend) se edita como de un solo piso.
            int pisos = Mathf.Max(1, ProyectoManager.CantidadPisos);
            Construir(pisos, ProyectoManager.PiezasACargar, 0);
        }

        // Arma todos los pisos desde cero y coloca las piezas recibidas. Lo usa
        // tanto la carga inicial como EliminarPiso, que reconstruye la grilla
        // entera con las piezas que quedan en vez de mover celdas de un piso a
        // otro.
        private void Construir(int pisos, PiezaDto[] piezas, int pisoAMostrar)
        {
            DestruirPisos();
            GrillaModelo.Generar(pisos, filasGrilla, columnasGrilla);

            for (int piso = 0; piso < pisos; piso++)
            {
                RectTransform contenedor = Instantiate(contenedorGrilla, contenedorGrilla.parent);
                contenedor.name = $"ContenedorGrilla_Piso{piso}";
                contenedor.SetSiblingIndex(contenedorGrilla.GetSiblingIndex() + 1 + piso);
                // Todos los pisos quedan activos mientras se arman: el
                // GridLayoutGroup de un contenedor inactivo no ubica sus celdas,
                // y PiezaView.PosicionarSobreCeldas mide esas posiciones.
                contenedor.gameObject.SetActive(true);
                contenedoresPorPiso.Add(contenedor);

                GenerarCeldas(piso, contenedor);

                // El GridLayoutGroup recién ubica las celdas en su rebuild
                // diferido (antes de dibujar el frame), no apenas se instancian.
                // Sin este forzado, PiezaView.PosicionarSobreCeldas mide
                // anchoredPosition todavía en (0,0) para todas las celdas al
                // reconstruir piezas guardadas, y calcula mal tamaño y posición.
                LayoutRebuilder.ForceRebuildLayoutImmediate(contenedor);
            }

            CargarPiezas(piezas);
            MostrarPiso(Mathf.Clamp(pisoAMostrar, 0, pisos - 1));

            // RF-20: también con la grilla vacía (proyecto nuevo), para que se
            // vea desde el principio lo que falta, como la Entrada y la Salida.
            ValidadorDiseno.PedirValidacion();
        }

        // RF-20: las celdas piden validación cada vez que cambian, y acá se
        // corre una sola vez por frame, después de que todos los cambios de
        // ese frame ya pasaron (una rampa ocupa dos celdas, una escalera una
        // por piso, reconstruir un proyecto coloca todas sus piezas de golpe).
        private void LateUpdate()
        {
            if (!ValidadorDiseno.HayValidacionPendiente) return;

            ValidadorDiseno.Resultado resultado = ValidadorDiseno.Validar();
            foreach (CeldaView celda in celdas.Values)
            {
                celda.MostrarAdvertencia(resultado.CeldasMarcadas.Contains(celda.Modelo));
            }
        }

        // Se desactivan antes de destruir porque Destroy recién se ejecuta al
        // final del frame, y mientras tanto se verían encima de los nuevos.
        private static void DestruirPisos()
        {
            foreach (RectTransform contenedor in contenedoresPorPiso)
            {
                contenedor.gameObject.SetActive(false);
                Destroy(contenedor.gameObject);
            }
            contenedoresPorPiso.Clear();
            celdas.Clear();
        }

        private void GenerarCeldas(int piso, RectTransform contenedor)
        {
            for (int fila = 0; fila < filasGrilla; fila++)
            {
                for (int columna = 0; columna < columnasGrilla; columna++)
                {
                    GameObject celda = Instantiate(prefabCelda, contenedor);
                    celda.name = $"Celda_{piso}_{fila}_{columna}";

                    CeldaView celdaView = celda.GetComponent<CeldaView>();
                    celdaView.Inicializar(piso, fila, columna);
                    celdas[(piso, fila, columna)] = celdaView;
                }
            }
        }

        // Reconstruye cada pieza guardada en su celda ancla, con la
        // orientación (y accesibilidad, si aplica) guardadas — celdas o pisos
        // fuera de rango se ignoran en vez de romper todo, por si la grilla
        // cambió de tamaño.
        //
        // Rampa y Escalera no se reconstruyen fila por fila: cada una es una
        // sola pieza lógica que al colocarse crea sus celdas en todos los pisos
        // que ocupa (editor/catalogo-piezas.md). La Escalera se coloca una vez
        // por posición, así que si se agregaron pisos desde la configuración
        // se extiende sola hasta el piso nuevo.
        private void CargarPiezas(PiezaDto[] piezas)
        {
            if (piezas == null) return;

            var posicionesEscalera = new HashSet<(int fila, int columna)>();

            foreach (PiezaDto pieza in piezas)
            {
                TipoPieza tipo = (TipoPieza)Enum.Parse(typeof(TipoPieza), pieza.tipo);

                if (tipo == TipoPieza.RAMPA) continue;

                if (tipo == TipoPieza.ESCALERA)
                {
                    posicionesEscalera.Add((pieza.fila, pieza.columna));
                    continue;
                }

                CeldaView ancla = ObtenerCelda(pieza.piso, pieza.fila, pieza.columna);
                if (ancla == null) continue;

                CaraAcceso orientacion = (CaraAcceso)Enum.Parse(typeof(CaraAcceso), pieza.caraAcceso);

                if (tipo == TipoPieza.PLAZA)
                {
                    GameObject instanciaPlaza = Instantiate(prefabPlaza, contenedoresPorPiso[pieza.piso]);
                    instanciaPlaza.GetComponent<PiezaView>().ColocarDesdeGuardado(ancla, orientacion, pieza.esAccesible);
                }
                else
                {
                    GameObject instanciaSimple = Instantiate(PrefabPara(tipo), contenedoresPorPiso[pieza.piso]);
                    instanciaSimple.GetComponent<PiezaSimpleView>().ColocarDesdeGuardado(ancla, orientacion, pieza.esCrucePeatonal);
                }
            }

            foreach ((int fila, int columna) in posicionesEscalera)
            {
                PiezaMultipisoView escalera = CrearPiezaMultipiso(TipoPieza.ESCALERA, SentidoVertical.SUBE);
                escalera.ColocarDesdeGuardado(0, fila, columna, CaraAcceso.NORTE);
            }

            foreach (PiezaDto entrada in EntradasDeRampas(piezas))
            {
                if (entrada.piso >= CantidadPisos) continue;

                SentidoVertical sentido = (SentidoVertical)Enum.Parse(typeof(SentidoVertical), entrada.sentidoVertical);
                CaraAcceso orientacion = (CaraAcceso)Enum.Parse(typeof(CaraAcceso), entrada.caraAcceso);
                PiezaMultipisoView rampa = CrearPiezaMultipiso(TipoPieza.RAMPA, sentido);
                rampa.ColocarDesdeGuardado(entrada.piso, entrada.fila, entrada.columna, orientacion);
            }
        }

        private GameObject PrefabPara(TipoPieza tipo)
        {
            return tipo switch
            {
                TipoPieza.CALLE => prefabCalle,
                TipoPieza.ENTRADA => prefabEntrada,
                TipoPieza.SALIDA => prefabSalida,
                TipoPieza.ZONA_BICI_MOTO => prefabZonaBiciMoto,
                _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de pieza sin prefab asignado")
            };
        }

        // PiezaMultipisoView la usa para crear su copia en cada piso que ocupa
        // además del que se está mirando. Se instancia bajo el Canvas raíz
        // (siempre activo) y no directo en la celda: si la celda es de un piso
        // oculto, Unity no correría Awake hasta que ese piso se muestre, y la
        // pieza se asienta antes.
        public static PiezaMultipisoView CrearPiezaMultipiso(TipoPieza tipo, SentidoVertical sentidoVertical)
        {
            GameObject prefab;
            if (tipo == TipoPieza.ESCALERA)
            {
                prefab = instancia.prefabEscalera;
            }
            else if (sentidoVertical == SentidoVertical.SUBE)
            {
                prefab = instancia.prefabRampaSube;
            }
            else
            {
                prefab = instancia.prefabRampaBaja;
            }

            return Instantiate(prefab, canvasRaiz).GetComponent<PiezaMultipisoView>();
        }

        // Las dos filas de una rampa guardan la misma posición, sentido y
        // flecha; lo que no se guarda es cuál de las dos es la de entrada
        // (editor/catalogo-piezas.md, "el par de rampa se deriva"). Una que
        // sube entra por el piso de abajo del par, una que baja por el de
        // arriba. Si en la misma posición hay varias rampas del mismo sentido
        // apiladas (0→1 y 2→3), recorrerlas en orden de entrada las empareja
        // bien, porque ninguna celda puede ser de dos rampas a la vez. Una
        // fila sin par (dato roto) se descarta.
        private static List<PiezaDto> EntradasDeRampas(PiezaDto[] piezas)
        {
            var filasPorGrupo = new Dictionary<(int fila, int columna, string sentido), List<PiezaDto>>();
            foreach (PiezaDto pieza in piezas)
            {
                if (pieza.tipo != TipoPieza.RAMPA.ToString()) continue;

                var clave = (pieza.fila, pieza.columna, pieza.sentidoVertical);
                if (!filasPorGrupo.ContainsKey(clave))
                {
                    filasPorGrupo[clave] = new List<PiezaDto>();
                }
                filasPorGrupo[clave].Add(pieza);
            }

            var entradas = new List<PiezaDto>();
            foreach (List<PiezaDto> grupo in filasPorGrupo.Values)
            {
                bool sube = grupo[0].sentidoVertical == SentidoVertical.SUBE.ToString();
                if (sube)
                {
                    grupo.Sort((a, b) => a.piso.CompareTo(b.piso));
                }
                else
                {
                    grupo.Sort((a, b) => b.piso.CompareTo(a.piso));
                }

                int i = 0;
                while (i < grupo.Count)
                {
                    int pisoSalidaEsperado = grupo[i].piso + (sube ? 1 : -1);
                    bool tienePar = i + 1 < grupo.Count && grupo[i + 1].piso == pisoSalidaEsperado;
                    if (tienePar)
                    {
                        entradas.Add(grupo[i]);
                        i += 2;
                    }
                    else
                    {
                        i += 1;
                    }
                }
            }
            return entradas;
        }

        // --- Pisos (RF-18) ---

        public static void MostrarPiso(int piso)
        {
            PisoActual = piso;
            for (int i = 0; i < contenedoresPorPiso.Count; i++)
            {
                contenedoresPorPiso[i].gameObject.SetActive(i == piso);
            }
            AlCambiarPisos?.Invoke();
        }

        // Elimina un piso con todas sus piezas y baja un número los pisos de
        // arriba. Una rampa que llega o sale de ese piso se elimina entera
        // (también su celda en el piso vecino); una escalera solo pierde su
        // celda de ese piso y sigue cubriendo el resto. Planta baja no se
        // puede eliminar: ahí están la Entrada y la Salida (SelectorPisos no
        // ofrece la opción). Queda sin guardar hasta que el usuario toque
        // Guardar, igual que cualquier otro cambio del editor.
        public static void EliminarPiso(int pisoAEliminar)
        {
            if (pisoAEliminar == 0 || pisoAEliminar >= CantidadPisos) return;

            PiezaDto[] actuales = RecolectarPiezas();
            var restantes = new List<PiezaDto>();

            foreach (PiezaDto pieza in actuales)
            {
                if (pieza.tipo == TipoPieza.RAMPA.ToString()) continue;
                if (pieza.piso == pisoAEliminar) continue;

                if (pieza.piso > pisoAEliminar)
                {
                    pieza.piso--;
                }
                restantes.Add(pieza);
            }

            foreach (PiezaDto entrada in EntradasDeRampas(actuales))
            {
                bool sube = entrada.sentidoVertical == SentidoVertical.SUBE.ToString();
                int pisoSalida = entrada.piso + (sube ? 1 : -1);
                if (entrada.piso == pisoAEliminar || pisoSalida == pisoAEliminar) continue;

                int corrimiento = Math.Min(entrada.piso, pisoSalida) > pisoAEliminar ? 1 : 0;
                restantes.Add(CopiarEnPiso(entrada, entrada.piso - corrimiento));
                restantes.Add(CopiarEnPiso(entrada, pisoSalida - corrimiento));
            }

            instancia.Construir(CantidadPisos - 1, restantes.ToArray(), pisoAEliminar - 1);
        }

        private static PiezaDto CopiarEnPiso(PiezaDto original, int piso)
        {
            return new PiezaDto
            {
                piso = piso,
                fila = original.fila,
                columna = original.columna,
                tipo = original.tipo,
                caraAcceso = original.caraAcceso,
                esAccesible = original.esAccesible,
                esCrucePeatonal = original.esCrucePeatonal,
                sentidoVertical = original.sentidoVertical
            };
        }

        // Para mensajes y botones: el piso 0 es la planta baja.
        public static string NombrePiso(int piso)
        {
            return piso == 0 ? "planta baja" : $"el piso {piso}";
        }

        // --- Consultas sobre la grilla ---

        public static CeldaView ObtenerCelda(int piso, int fila, int columna)
        {
            return celdas.TryGetValue((piso, fila, columna), out CeldaView celda) ? celda : null;
        }

        // El hueco entre celdas (m_Spacing del GridLayoutGroup) no tiene
        // Image propia, así que un raycast que cae justo ahí no golpea
        // ningún CeldaView. RaycastUtils llama acá cuando el raycast solo
        // encontró el catcher transparente del contenedor (ver
        // Editor.unity), para redondear ese punto a la celda más cercana en
        // vez de tratar el hueco como si estuviera fuera de la grilla.
        public static CeldaView ObtenerCeldaMasCercana(Vector2 posicionPantalla, Camera camaraEvento)
        {
            RectTransform contenedor = ContenedorActual;
            if (contenedor == null) return null;

            bool convertido = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                contenedor, posicionPantalla, camaraEvento, out Vector2 local);
            if (!convertido) return null;

            Rect rect = contenedor.rect;
            float pasoX = TamanioCelda.x + Espaciado.x;
            float pasoY = TamanioCelda.y + Espaciado.y;

            int columna = Mathf.FloorToInt((local.x - rect.xMin) / pasoX);
            int fila = Mathf.FloorToInt((rect.yMax - local.y) / pasoY);

            if (columna < 0 || columna >= columnasGrilla || fila < 0 || fila >= filasGrilla)
            {
                return null;
            }

            return ObtenerCelda(PisoActual, fila, columna);
        }

        // RF-19: Entrada, Salida y ZonaBicicletasMotos solo son válidas sobre
        // el borde de la grilla.
        public static bool EsCeldaDeBorde(int fila, int columna)
        {
            return fila == 0 || fila == filasGrilla - 1 || columna == 0 || columna == columnasGrilla - 1;
        }

        // RF-19: hacia dónde queda "afuera de la grilla" desde esta celda —
        // vacío si no es de borde, una dirección en un borde simple, dos en
        // una esquina. Es la base para la orientación automática de Entrada,
        // Salida y ZonaBicicletasMotos (PiezaSimpleView).
        public static CaraAcceso[] DireccionesHaciaAfuera(int fila, int columna)
        {
            var direcciones = new List<CaraAcceso>();
            if (fila == 0) direcciones.Add(CaraAcceso.NORTE);
            if (fila == filasGrilla - 1) direcciones.Add(CaraAcceso.SUR);
            if (columna == 0) direcciones.Add(CaraAcceso.OESTE);
            if (columna == columnasGrilla - 1) direcciones.Add(CaraAcceso.ESTE);
            return direcciones.ToArray();
        }

        // RF-21: recorre todas las celdas de todos los pisos y arma una fila
        // por cada pieza colocada, en su celda ancla (dominio/modelo-clases.md)
        // — la segunda celda de una Plaza no se guarda aparte, el backend la
        // vuelve a inferir de caraAcceso. Rampa y Escalera sí tienen una fila
        // por piso, porque tienen una vista hija en cada celda que ocupan.
        // includeInactive: las piezas de los pisos que no se están mirando
        // están en contenedores ocultos, y también hay que guardarlas.
        public static PiezaDto[] RecolectarPiezas()
        {
            var piezas = new List<PiezaDto>();

            foreach (CeldaView celda in celdas.Values)
            {
                IPiezaColocada pieza = celda.GetComponentInChildren<IPiezaColocada>(true);
                if (pieza == null) continue;

                piezas.Add(new PiezaDto
                {
                    piso = celda.Piso,
                    fila = celda.Fila,
                    columna = celda.Columna,
                    tipo = pieza.Tipo.ToString(),
                    caraAcceso = pieza.Orientacion,
                    esAccesible = pieza.EsAccesible,
                    esCrucePeatonal = pieza.EsCrucePeatonal,
                    sentidoVertical = pieza.SentidoVertical
                });
            }

            return piezas.ToArray();
        }
    }
}
