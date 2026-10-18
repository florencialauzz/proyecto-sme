using System;
using System.Collections.Generic;
using Sme.Managers;
using Sme.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Sme.Grid
{
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

        private static readonly Dictionary<(int piso, int fila, int columna), CeldaView> celdas = new();
        private static readonly List<RectTransform> contenedoresPorPiso = new();

        public static Vector2 TamanioCelda { get; private set; }
        public static Vector2 Espaciado { get; private set; }
        public static int PisoActual { get; private set; }
        public static int CantidadPisos => GrillaModelo.CantidadPisos;

        public static bool SoloLectura { get; private set; }

        public static event Action AlCambiarPisos;

        private static int filasGrilla;
        private static int columnasGrilla;
        private static GrillaGenerador instancia;
        private static Transform canvasRaiz;

        public static GameObject ContenedorGrillaGameObject =>
            ContenedorActual != null ? ContenedorActual.gameObject : null;

        private static RectTransform ContenedorActual =>
            PisoActual < contenedoresPorPiso.Count ? contenedoresPorPiso[PisoActual] : null;

        private void Start()
        {
            contenedoresPorPiso.Clear();
            celdas.Clear();
            SoloLectura = false;

            instancia = this;
            canvasRaiz = contenedorGrilla.GetComponentInParent<Canvas>().rootCanvas.transform;
            filasGrilla = ProyectoManager.FilasGrilla;
            columnasGrilla = ProyectoManager.ColumnasGrilla;

            GridLayoutGroup layout = contenedorGrilla.GetComponent<GridLayoutGroup>();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = columnasGrilla;
            TamanioCelda = layout.cellSize;
            Espaciado = layout.spacing;

            contenedorGrilla.gameObject.SetActive(false);

            int pisos = Mathf.Max(1, ProyectoManager.CantidadPisos);
            Construir(pisos, ProyectoManager.PiezasACargar, 0);

            MarcarDisenoGuardado();
        }

        private void Construir(int pisos, PiezaDto[] piezas, int pisoAMostrar)
        {
            DestruirPisos();
            GrillaModelo.Generar(pisos, filasGrilla, columnasGrilla);

            for (int piso = 0; piso < pisos; piso++)
            {
                RectTransform contenedor = Instantiate(contenedorGrilla, contenedorGrilla.parent);
                contenedor.name = $"ContenedorGrilla_Piso{piso}";
                contenedor.SetSiblingIndex(contenedorGrilla.GetSiblingIndex() + 1 + piso);
                contenedor.gameObject.SetActive(true);
                contenedoresPorPiso.Add(contenedor);

                GenerarCeldas(piso, contenedor);

                // Si no, las celdas siguen en (0,0) cuando se reconstruyen las piezas
                LayoutRebuilder.ForceRebuildLayoutImmediate(contenedor);
            }

            CargarPiezas(piezas);
            MostrarPiso(Mathf.Clamp(pisoAMostrar, 0, pisos - 1));

            ValidadorDiseno.PedirValidacion();
        }

        private void LateUpdate()
        {
            if (!ValidadorDiseno.HayValidacionPendiente) return;

            ValidadorDiseno.Resultado resultado = ValidadorDiseno.Validar();
            foreach (CeldaView celda in celdas.Values)
            {
                celda.MostrarAdvertencia(resultado.CeldasMarcadas.Contains(celda.Modelo));
            }
        }

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

        public static void MostrarPiso(int piso)
        {
            PisoActual = piso;
            for (int i = 0; i < contenedoresPorPiso.Count; i++)
            {
                contenedoresPorPiso[i].gameObject.SetActive(i == piso);
            }
            AlCambiarPisos?.Invoke();
        }

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

        public static void ActivarSoloLectura()
        {
            SoloLectura = true;

            foreach (CeldaView celda in celdas.Values)
            {
                foreach (IPiezaColocada pieza in celda.GetComponentsInChildren<IPiezaColocada>(true))
                {
                    CanvasGroup canvasGroupPieza = ((Component)pieza).GetComponent<CanvasGroup>();
                    // Por pieza: un CanvasGroup en el contenedor no llega a los Canvas con overrideSorting
                    canvasGroupPieza.blocksRaycasts = false;
                }
            }

            AlCambiarPisos?.Invoke();
        }

        public static RectTransform ContenedorDePiso(int piso)
        {
            return contenedoresPorPiso[piso];
        }

        public static string NombrePiso(int piso)
        {
            return piso == 0 ? "planta baja" : $"el piso {piso}";
        }

        public static CeldaView ObtenerCelda(int piso, int fila, int columna)
        {
            return celdas.TryGetValue((piso, fila, columna), out CeldaView celda) ? celda : null;
        }

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

        public static bool EsCeldaDeBorde(int fila, int columna)
        {
            return fila == 0 || fila == filasGrilla - 1 || columna == 0 || columna == columnasGrilla - 1;
        }

        public static CaraAcceso[] DireccionesHaciaAfuera(int fila, int columna)
        {
            var direcciones = new List<CaraAcceso>();
            if (fila == 0) direcciones.Add(CaraAcceso.NORTE);
            if (fila == filasGrilla - 1) direcciones.Add(CaraAcceso.SUR);
            if (columna == 0) direcciones.Add(CaraAcceso.OESTE);
            if (columna == columnasGrilla - 1) direcciones.Add(CaraAcceso.ESTE);
            return direcciones.ToArray();
        }

        private static string firmaGuardada;

        public static void MarcarDisenoGuardado()
        {
            firmaGuardada = FirmaDelDiseno();
        }

        public static bool HayCambiosSinGuardar()
        {
            return FirmaDelDiseno() != firmaGuardada;
        }

        private static string FirmaDelDiseno()
        {
            var lineas = new List<string>();
            foreach (PiezaDto pieza in RecolectarPiezas())
            {
                lineas.Add($"{pieza.piso},{pieza.fila},{pieza.columna},{pieza.tipo},{pieza.caraAcceso}," +
                           $"{pieza.esAccesible},{pieza.esCrucePeatonal},{pieza.sentidoVertical}");
            }
            lineas.Sort(string.CompareOrdinal);
            return CantidadPisos + "|" + string.Join(";", lineas);
        }

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
