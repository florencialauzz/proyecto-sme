using System.Collections.Generic;
using Sme.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Sme.Grid
{
    public class AutosReproduccion : MonoBehaviour
    {
        private const float MargenAncho = 0.15f;
        private const float MargenLargo = 0.08f;

        private const float TramosPorSegundo = 4f;

        private const float MaximoMinutosRecorrido = 30f;

        private const float VelocidadMaximaConRechazados = 5f;

        private static readonly Color ColorReservada = new Color(1f, 0.8f, 0.25f, 0.35f);

        private static readonly Color ColorRechazado = new Color(1f, 0.25f, 0.25f, 1f);

        [SerializeField] private Sprite spriteAuto;

        private ReproduccionDto reproduccion;

        private PiezaView[] vistaPorPlaza;
        private GameObject[] autoEstacionadoPorPlaza;
        private GameObject[] marcaReservadaPorPlaza;

        private RectTransform[] capaPorPiso;

        private readonly List<AutoEnCamino> autosEnCamino = new();

        private int siguienteEvento;

        private int[] llegadasEnSuMinuto;
        private int[] ordenEnSuMinuto;

        private int minutoActual;

        private enum TipoRecorrido
        {
            HaciaLaPlaza,
            HaciaLaSalida,
            Rechazado
        }

        private const int SinPlaza = -1;

        private class AutoEnCamino
        {
            public TipoRecorrido Tipo;
            public int IndiceEvento;
            public int IndicePlaza;
            public RectTransform Vista;

            public bool PrimerTramoMarchaAtras;

            public float SegundosDeEspera;

            public Vector3[] Puntos;
            public int[] Pisos;

            public float Avance;

            public bool Termino => Avance >= Puntos.Length - 1;
        }

        public void Preparar(ReproduccionDto reproduccionAMostrar)
        {
            reproduccion = reproduccionAMostrar;
            siguienteEvento = 0;
            ContarLlegadasPorMinuto();

            int cantidadPlazas = reproduccion.plazas.Length;
            vistaPorPlaza = new PiezaView[cantidadPlazas];
            autoEstacionadoPorPlaza = new GameObject[cantidadPlazas];
            marcaReservadaPorPlaza = new GameObject[cantidadPlazas];

            for (int i = 0; i < cantidadPlazas; i++)
            {
                PlazaReproduccionDto plaza = reproduccion.plazas[i];
                CeldaView ancla = GrillaGenerador.ObtenerCelda(plaza.piso, plaza.fila, plaza.columna);
                vistaPorPlaza[i] = ancla.GetComponentInChildren<PiezaView>(true);

                marcaReservadaPorPlaza[i] = CrearMarcaReservada(vistaPorPlaza[i].transform);
                marcaReservadaPorPlaza[i].SetActive(false);

                autoEstacionadoPorPlaza[i] = CrearAutoEstacionado(vistaPorPlaza[i].transform);
                autoEstacionadoPorPlaza[i].SetActive(false);
            }

            capaPorPiso = new RectTransform[GrillaGenerador.CantidadPisos];
            for (int piso = 0; piso < capaPorPiso.Length; piso++)
            {
                capaPorPiso[piso] = CrearCapaAutos(piso);
            }
        }

        public int MostrarMinuto(int minuto, float minutosSimuladosPorSegundo)
        {
            int minutoAnterior = minutoActual;
            minutoActual = minuto;

            CortarRecorridosVencidos(minuto);
            ArrancarRecorridosDeLosQueSeVan(minutoAnterior, minuto, minutosSimuladosPorSegundo);
            ArrancarRecorridosDeLosQueLlegaron(minuto, minutosSimuladosPorSegundo);
            return MostrarEstacionados(minuto);
        }

        public void CambiarVelocidad(float minutosSimuladosPorSegundoAnterior, float minutosSimuladosPorSegundo)
        {
            for (int i = autosEnCamino.Count - 1; i >= 0; i--)
            {
                AutoEnCamino auto = autosEnCamino[i];

                auto.SegundosDeEspera = auto.SegundosDeEspera * minutosSimuladosPorSegundoAnterior
                                        / minutosSimuladosPorSegundo;

                float tramosQueFaltan = auto.Puntos.Length - 1 - auto.Avance;
                float segundosQueFaltan = Mathf.Max(0f, auto.SegundosDeEspera) + tramosQueFaltan / TramosPorSegundo;
                float minutosQueFaltan = segundosQueFaltan * minutosSimuladosPorSegundo;

                bool todaviaEntra;
                if (auto.Tipo == TipoRecorrido.HaciaLaPlaza)
                {
                    int minutoSalida = reproduccion.eventos[auto.IndiceEvento].minutoSalida;
                    todaviaEntra = minutoActual + minutosQueFaltan < minutoSalida;
                }
                else if (auto.Tipo == TipoRecorrido.Rechazado)
                {
                    todaviaEntra = SeDibujanRechazados(minutosSimuladosPorSegundo);
                }
                else
                {
                    todaviaEntra = minutosQueFaltan <= MaximoMinutosRecorrido;
                }

                if (!todaviaEntra)
                {
                    TerminarRecorrido(auto);
                    autosEnCamino.RemoveAt(i);
                }
            }
        }

        public void Avanzar(float segundosReales)
        {
            for (int i = autosEnCamino.Count - 1; i >= 0; i--)
            {
                AutoEnCamino auto = autosEnCamino[i];

                if (auto.SegundosDeEspera > 0f)
                {
                    auto.SegundosDeEspera -= segundosReales;
                    if (auto.SegundosDeEspera > 0f) continue;

                    auto.Vista.gameObject.SetActive(true);
                }

                auto.Avance += TramosPorSegundo * segundosReales;

                if (auto.Termino)
                {
                    TerminarRecorrido(auto);
                    autosEnCamino.RemoveAt(i);
                }
                else
                {
                    Ubicar(auto);
                }
            }
        }

        private void ArrancarRecorridosDeLosQueLlegaron(int minuto, float minutosSimuladosPorSegundo)
        {
            while (siguienteEvento < reproduccion.eventos.Length
                   && reproduccion.eventos[siguienteEvento].minutoLlegada <= minuto)
            {
                int indiceEvento = siguienteEvento;
                siguienteEvento++;

                float minutosDeEspera = EsperaDentroDelMinuto(indiceEvento, minuto);
                float segundosDeEspera = minutosDeEspera / minutosSimuladosPorSegundo;

                EventoVehiculoDto evento = reproduccion.eventos[indiceEvento];
                if (evento.indicePlaza < 0)
                {
                    ArrancarRecorridoRechazadoSiEntra(indiceEvento, segundosDeEspera, minutosSimuladosPorSegundo);
                    continue;
                }

                PlazaReproduccionDto plaza = reproduccion.plazas[evento.indicePlaza];
                int tramos = plaza.caminoEntrada.Length;
                float minutosDelRecorrido = tramos / TramosPorSegundo * minutosSimuladosPorSegundo;

                if (minuto + minutosDeEspera + minutosDelRecorrido < evento.minutoSalida)
                {
                    ArrancarRecorridoHaciaLaPlaza(indiceEvento, evento.indicePlaza, segundosDeEspera);
                }
            }
        }

        // Los que llegan en el mismo minuto se reparten para que no se vean superpuestos
        private float EsperaDentroDelMinuto(int indiceEvento, int minuto)
        {
            EventoVehiculoDto evento = reproduccion.eventos[indiceEvento];
            float momentoDeArranque = evento.minutoLlegada
                                      + (float)ordenEnSuMinuto[indiceEvento] / llegadasEnSuMinuto[indiceEvento];
            return Mathf.Max(0f, momentoDeArranque - minuto);
        }

        private void ContarLlegadasPorMinuto()
        {
            int cantidadEventos = reproduccion.eventos.Length;
            llegadasEnSuMinuto = new int[cantidadEventos];
            ordenEnSuMinuto = new int[cantidadEventos];

            int inicioDelGrupo = 0;
            while (inicioDelGrupo < cantidadEventos)
            {
                int minutoDelGrupo = reproduccion.eventos[inicioDelGrupo].minutoLlegada;
                int finDelGrupo = inicioDelGrupo;
                while (finDelGrupo < cantidadEventos && reproduccion.eventos[finDelGrupo].minutoLlegada == minutoDelGrupo)
                {
                    finDelGrupo++;
                }

                for (int i = inicioDelGrupo; i < finDelGrupo; i++)
                {
                    llegadasEnSuMinuto[i] = finDelGrupo - inicioDelGrupo;
                    ordenEnSuMinuto[i] = i - inicioDelGrupo;
                }
                inicioDelGrupo = finDelGrupo;
            }
        }

        private void ArrancarRecorridoHaciaLaPlaza(int indiceEvento, int indicePlaza, float segundosDeEspera)
        {
            PlazaReproduccionDto plaza = reproduccion.plazas[indicePlaza];

            var puntos = new List<Vector3>();
            var pisos = new List<int>();
            AgregarCeldas(plaza.caminoEntrada, 0, puntos, pisos);
            puntos.Add(CentroDePlaza(indicePlaza));
            pisos.Add(plaza.piso);

            ArrancarRecorrido(TipoRecorrido.HaciaLaPlaza, indiceEvento, indicePlaza, puntos, pisos,
                primerTramoMarchaAtras: false, segundosDeEspera);
            marcaReservadaPorPlaza[indicePlaza].SetActive(true);
        }

        private void CortarRecorridosVencidos(int minuto)
        {
            for (int i = autosEnCamino.Count - 1; i >= 0; i--)
            {
                AutoEnCamino auto = autosEnCamino[i];
                bool haciaLaPlaza = auto.Tipo == TipoRecorrido.HaciaLaPlaza;
                if (haciaLaPlaza && reproduccion.eventos[auto.IndiceEvento].minutoSalida <= minuto)
                {
                    TerminarRecorrido(auto);
                    autosEnCamino.RemoveAt(i);
                }
            }
        }

        private void ArrancarRecorridosDeLosQueSeVan(int minutoAnterior, int minuto, float minutosSimuladosPorSegundo)
        {
            for (int i = 0; i < reproduccion.eventos.Length; i++)
            {
                EventoVehiculoDto evento = reproduccion.eventos[i];
                if (evento.indicePlaza < 0) continue;

                bool seVaAhora = minutoAnterior < evento.minutoSalida && evento.minutoSalida <= minuto;
                if (!seVaAhora) continue;

                PlazaReproduccionDto plaza = reproduccion.plazas[evento.indicePlaza];
                int tramos = plaza.caminoSalida.Length;
                float minutosDelRecorrido = tramos / TramosPorSegundo * minutosSimuladosPorSegundo;

                if (minutosDelRecorrido <= MaximoMinutosRecorrido)
                {
                    ArrancarRecorridoHaciaLaSalida(i, evento.indicePlaza);
                }
            }
        }

        private void ArrancarRecorridoHaciaLaSalida(int indiceEvento, int indicePlaza)
        {
            PlazaReproduccionDto plaza = reproduccion.plazas[indicePlaza];

            var puntos = new List<Vector3>();
            var pisos = new List<int>();
            puntos.Add(CentroDePlaza(indicePlaza));
            pisos.Add(plaza.piso);
            AgregarCeldas(plaza.caminoSalida, 0, puntos, pisos);

            ArrancarRecorrido(TipoRecorrido.HaciaLaSalida, indiceEvento, indicePlaza, puntos, pisos,
                primerTramoMarchaAtras: true, segundosDeEspera: 0f);
        }

        private void ArrancarRecorridoRechazadoSiEntra(int indiceEvento, float segundosDeEspera,
            float minutosSimuladosPorSegundo)
        {
            if (!SeDibujanRechazados(minutosSimuladosPorSegundo)) return;

            int indicePlazaDelRecorrido = reproduccion.indicePlazaRecorridoRechazados;
            if (indicePlazaDelRecorrido < 0) return;

            PlazaReproduccionDto plaza = reproduccion.plazas[indicePlazaDelRecorrido];

            var puntos = new List<Vector3>();
            var pisos = new List<int>();
            AgregarCeldas(plaza.caminoEntrada, 0, puntos, pisos);
            AgregarCeldas(plaza.caminoSalida, 1, puntos, pisos);

            ArrancarRecorrido(TipoRecorrido.Rechazado, indiceEvento, SinPlaza, puntos, pisos,
                primerTramoMarchaAtras: false, segundosDeEspera);
        }

        private static bool SeDibujanRechazados(float minutosSimuladosPorSegundo)
        {
            return minutosSimuladosPorSegundo <= VelocidadMaximaConRechazados;
        }

        private void ArrancarRecorrido(TipoRecorrido tipo, int indiceEvento, int indicePlaza,
            List<Vector3> puntos, List<int> pisos, bool primerTramoMarchaAtras, float segundosDeEspera)
        {
            var auto = new AutoEnCamino
            {
                Tipo = tipo,
                IndiceEvento = indiceEvento,
                IndicePlaza = indicePlaza,
                Vista = CrearAutoEnCamino(capaPorPiso[pisos[0]], tipo == TipoRecorrido.Rechazado),
                PrimerTramoMarchaAtras = primerTramoMarchaAtras,
                SegundosDeEspera = segundosDeEspera,
                Puntos = puntos.ToArray(),
                Pisos = pisos.ToArray(),
                Avance = 0f
            };
            autosEnCamino.Add(auto);
            Ubicar(auto);
            auto.Vista.gameObject.SetActive(segundosDeEspera <= 0f);
        }

        private static void AgregarCeldas(CeldaCaminoDto[] camino, int desde, List<Vector3> puntos, List<int> pisos)
        {
            for (int i = desde; i < camino.Length; i++)
            {
                CeldaView celda = GrillaGenerador.ObtenerCelda(camino[i].piso, camino[i].fila, camino[i].columna);
                puntos.Add(CentroEnElMundo((RectTransform)celda.transform));
                pisos.Add(camino[i].piso);
            }
        }

        private Vector3 CentroDePlaza(int indicePlaza)
        {
            return CentroEnElMundo((RectTransform)vistaPorPlaza[indicePlaza].transform);
        }

        private void TerminarRecorrido(AutoEnCamino auto)
        {
            Destroy(auto.Vista.gameObject);

            if (auto.Tipo != TipoRecorrido.HaciaLaPlaza) return;

            marcaReservadaPorPlaza[auto.IndicePlaza].SetActive(false);
            EventoVehiculoDto evento = reproduccion.eventos[auto.IndiceEvento];
            bool todaviaLeTocaEstar = minutoActual < evento.minutoSalida;
            autoEstacionadoPorPlaza[auto.IndicePlaza].SetActive(todaviaLeTocaEstar);
        }

        private int MostrarEstacionados(int minuto)
        {
            bool[] plazaOcupada = new bool[autoEstacionadoPorPlaza.Length];
            foreach (EventoVehiculoDto evento in reproduccion.eventos)
            {
                bool tienePlaza = evento.indicePlaza >= 0;
                bool ocupa = evento.minutoLlegada <= minuto && minuto < evento.minutoSalida;
                if (tienePlaza && ocupa)
                {
                    plazaOcupada[evento.indicePlaza] = true;
                }
            }

            bool[] plazaConAutoEnCamino = new bool[autoEstacionadoPorPlaza.Length];
            foreach (AutoEnCamino auto in autosEnCamino)
            {
                if (auto.Tipo == TipoRecorrido.HaciaLaPlaza)
                {
                    plazaConAutoEnCamino[auto.IndicePlaza] = true;
                }
            }

            int autosConPlaza = 0;
            for (int i = 0; i < autoEstacionadoPorPlaza.Length; i++)
            {
                autoEstacionadoPorPlaza[i].SetActive(plazaOcupada[i] && !plazaConAutoEnCamino[i]);
                if (plazaOcupada[i])
                {
                    autosConPlaza++;
                }
            }
            return autosConPlaza;
        }

        private void Ubicar(AutoEnCamino auto)
        {
            int tramo = Mathf.Min((int)auto.Avance, auto.Puntos.Length - 2);
            float recorridoDelTramo = auto.Avance - tramo;
            Vector3 desde = auto.Puntos[tramo];
            Vector3 hasta = auto.Puntos[tramo + 1];

            int piso = recorridoDelTramo < 0.5f ? auto.Pisos[tramo] : auto.Pisos[tramo + 1];
            if (auto.Vista.parent != capaPorPiso[piso])
            {
                auto.Vista.SetParent(capaPorPiso[piso], false);
            }

            auto.Vista.position = Vector3.Lerp(desde, hasta, recorridoDelTramo);

            Vector3 direccion = hasta - desde;
            if (direccion.sqrMagnitude > 0.0001f)
            {
                // El sprite mira hacia +Y
                float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg - 90f;
                bool marchaAtras = auto.PrimerTramoMarchaAtras && tramo == 0;
                if (marchaAtras)
                {
                    angulo += 180f;
                }
                auto.Vista.rotation = Quaternion.Euler(0f, 0f, angulo);
            }
        }

        private static Vector3 CentroEnElMundo(RectTransform rect)
        {
            return rect.TransformPoint(rect.rect.center);
        }

        private static RectTransform CrearCapaAutos(int piso)
        {
            var capa = new GameObject($"CapaAutos_Piso{piso}", typeof(RectTransform), typeof(LayoutElement));
            capa.transform.SetParent(GrillaGenerador.ContenedorDePiso(piso), false);
            capa.GetComponent<LayoutElement>().ignoreLayout = true;

            capa.AddComponent<CapaAutosReproduccion>();

            RectTransform rect = capa.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return rect;
        }

        private GameObject CrearAutoEstacionado(Transform vistaPlaza)
        {
            GameObject auto = CrearImagenAuto("AutoEstacionado");
            auto.transform.SetParent(vistaPlaza, false);

            RectTransform rect = auto.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(MargenAncho, MargenLargo);
            rect.anchorMax = new Vector2(1f - MargenAncho, 1f - MargenLargo);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localEulerAngles = new Vector3(0f, 0f, 180f);

            return auto;
        }

        private RectTransform CrearAutoEnCamino(RectTransform capa, bool esRechazado)
        {
            GameObject auto = CrearImagenAuto(esRechazado ? "AutoRechazado" : "AutoEnCamino");
            auto.transform.SetParent(capa, false);
            if (esRechazado)
            {
                auto.GetComponent<Image>().color = ColorRechazado;
            }
            RectTransform rect = auto.GetComponent<RectTransform>();

            Vector2 celda = GrillaGenerador.TamanioCelda;
            float largoPlaza = celda.y * 2f + GrillaGenerador.Espaciado.y;
            rect.sizeDelta = new Vector2(celda.x * (1f - 2f * MargenAncho), largoPlaza * (1f - 2f * MargenLargo));

            return rect;
        }

        private GameObject CrearImagenAuto(string nombre)
        {
            var auto = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            Image imagen = auto.GetComponent<Image>();
            imagen.sprite = spriteAuto;
            imagen.preserveAspect = true;
            imagen.raycastTarget = false;
            return auto;
        }

        private static GameObject CrearMarcaReservada(Transform vistaPlaza)
        {
            var marca = new GameObject("MarcaReservada", typeof(RectTransform), typeof(Image));
            marca.transform.SetParent(vistaPlaza, false);

            RectTransform rect = marca.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image imagen = marca.GetComponent<Image>();
            imagen.color = ColorReservada;
            imagen.raycastTarget = false;
            return marca;
        }
    }
}
