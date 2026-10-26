using Sme.Grid;
using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    public class ModoReproduccion : MonoBehaviour
    {
        private const string TextoLeyenda =
            "La circulación es ilustrativa; los indicadores no contemplan tiempos de recorrido ni congestión.";

        private const float MinutosSimuladosPorSegundoAx1 = 1f;

        [SerializeField] private GameObject[] objetosDeEdicion;

        [SerializeField] private GameObject panelReproduccion;
        [SerializeField] private Button botonVerResultados;
        [SerializeField] private UnityEvent alIrAResultados;

        [SerializeField] private AutosReproduccion autos;

        [SerializeField] private Button botonPlayPausa;
        [SerializeField] private TMP_Text textoPlayPausa;
        [SerializeField] private Button botonVelocidadX1;
        [SerializeField] private Button botonVelocidadX5;
        [SerializeField] private Button botonVelocidadX20;

        [SerializeField] private TMP_Text textoHora;
        [SerializeField] private TMP_Text textoOcupadas;
        [SerializeField] private TMP_Text textoRechazados;
        [SerializeField] private GameObject avisoSaturado;
        [SerializeField] private TMP_Text textoLeyenda;

        private EjecutarSimulacionResponse indicadores;
        private ReproduccionDto reproduccion;

        private int duracionMinutos;
        private int minutoInicioDelDia;

        private float minutoActual;
        private int ultimoMinutoMostrado;
        private bool reproduciendo;
        private int velocidad;

        private void Awake()
        {
            panelReproduccion.SetActive(false);
            botonVerResultados.onClick.AddListener(VerResultados);
            botonPlayPausa.onClick.AddListener(AlternarPlayPausa);
            botonVelocidadX1.onClick.AddListener(() => CambiarVelocidad(1));
            botonVelocidadX5.onClick.AddListener(() => CambiarVelocidad(5));
            botonVelocidadX20.onClick.AddListener(() => CambiarVelocidad(20));
        }

        public void Entrar(EjecutarSimulacionResponse indicadoresSimulacion, ReproduccionDto reproduccionSimulacion)
        {
            indicadores = indicadoresSimulacion;
            reproduccion = reproduccionSimulacion;
            duracionMinutos = indicadores.curvaOcupacion.Length;
            minutoInicioDelDia = FormatoIndicadores.MinutoDelDia(ProyectoManager.HoraInicioSimulacion);

            foreach (GameObject objeto in objetosDeEdicion)
            {
                objeto.SetActive(false);
            }

            GrillaGenerador.ActivarSoloLectura();
            autos.Preparar(reproduccion);

            panelReproduccion.SetActive(true);
            textoLeyenda.text = TextoLeyenda;

            minutoActual = 0f;
            reproduciendo = false;
            CambiarVelocidad(1);
            ActualizarBotonPlayPausa();
            MostrarMinuto(0);
        }

        private void Update()
        {
            if (!reproduciendo) return;

            minutoActual += Time.deltaTime * MinutosSimuladosPorSegundoAx1 * velocidad;

            if (minutoActual >= duracionMinutos)
            {
                minutoActual = duracionMinutos - 1;
                reproduciendo = false;
                ActualizarBotonPlayPausa();
            }

            int minuto = (int)minutoActual;
            if (minuto != ultimoMinutoMostrado)
            {
                MostrarMinuto(minuto);
            }

            autos.Avanzar(Time.deltaTime);
        }

        private void AlternarPlayPausa()
        {
            reproduciendo = !reproduciendo;
            ActualizarBotonPlayPausa();
        }

        private bool LlegoAlFinal()
        {
            return ultimoMinutoMostrado >= duracionMinutos - 1;
        }

        private void ActualizarBotonPlayPausa()
        {
            if (LlegoAlFinal())
            {
                textoPlayPausa.text = "Fin";
                botonPlayPausa.interactable = false;
                return;
            }

            textoPlayPausa.text = reproduciendo ? "‖  Pausa" : "▶  Reproducir";
            botonPlayPausa.interactable = true;
        }

        private void CambiarVelocidad(int nuevaVelocidad)
        {
            int velocidadAnterior = velocidad;
            velocidad = nuevaVelocidad;

            if (velocidadAnterior > 0 && velocidadAnterior != velocidad)
            {
                autos.CambiarVelocidad(MinutosSimuladosPorSegundoAx1 * velocidadAnterior,
                    MinutosSimuladosPorSegundoAx1 * velocidad);
            }

            MarcarVelocidad(botonVelocidadX1, velocidad == 1);
            MarcarVelocidad(botonVelocidadX5, velocidad == 5);
            MarcarVelocidad(botonVelocidadX20, velocidad == 20);
        }

        private static void MarcarVelocidad(Button boton, bool elegida)
        {
            boton.interactable = !elegida;
            boton.image.color = elegida ? Tema.Primario : Tema.Superficie;
            boton.GetComponentInChildren<TMP_Text>().color = elegida ? Tema.TextoSobreColor : Tema.Texto;
        }

        private void MostrarMinuto(int minuto)
        {
            ultimoMinutoMostrado = minuto;

            textoHora.text = FormatoIndicadores.HoraDelDia(minutoInicioDelDia + minuto);

            int ocupadas = indicadores.curvaOcupacion[minuto].cantidadOcupadas;
            textoOcupadas.text = $"Ocupadas: {ocupadas} / {reproduccion.plazas.Length}";

            textoRechazados.text = $"Rechazados: {RechazadosHasta(minuto)}";
            avisoSaturado.SetActive(EstaSaturado(minuto));

            float minutosSimuladosPorSegundo = MinutosSimuladosPorSegundoAx1 * velocidad;
            int autosConPlaza = autos.MostrarMinuto(minuto, minutosSimuladosPorSegundo);
            if (autosConPlaza != ocupadas)
            {
                Debug.LogWarning($"Reproducción: en el minuto {minuto} hay {autosConPlaza} autos con plaza " +
                                 $"pero curvaOcupacion dice {ocupadas}.");
            }

            if (LlegoAlFinal())
            {
                ActualizarBotonPlayPausa();
            }
        }

        private int RechazadosHasta(int minuto)
        {
            int rechazados = 0;
            foreach (EventoVehiculoDto evento in reproduccion.eventos)
            {
                if (evento.indicePlaza < 0 && evento.minutoLlegada <= minuto)
                {
                    rechazados++;
                }
            }
            return rechazados;
        }

        private bool EstaSaturado(int minuto)
        {
            foreach (PeriodoSaturacionDto periodo in indicadores.periodosSaturacion)
            {
                if (periodo.inicio <= minuto && minuto <= periodo.fin)
                {
                    return true;
                }
            }
            return false;
        }

        private void VerResultados()
        {
            alIrAResultados?.Invoke();
        }
    }
}
