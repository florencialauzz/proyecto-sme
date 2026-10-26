using Sme.Grid;
using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    public class ResultadosScreen : MonoBehaviour
    {
        [SerializeField] private TMP_Text textoEficiencia;
        [SerializeField] private TMP_Text textoEficienciaDetalle;
        [SerializeField] private TMP_Text textoSaturacion;
        [SerializeField] private TMP_Text textoSaturacionDetalle;
        [SerializeField] private TMP_Text textoDemanda;
        [SerializeField] private TMP_Text textoDemandaDetalle;
        [SerializeField] private TMP_Text textoPuntuacion;
        [SerializeField] private TMP_Text textoPuntuacionDetalle;

        [SerializeField] private TMP_Text textoPeriodosSaturacion;
        [SerializeField] private GraficoCurva grafico;

        [SerializeField] private Button botonGuardarResultados;
        [SerializeField] private TMP_Text textoEstadoGuardado;

        [SerializeField] private Button botonVolver;
        [SerializeField] private UnityEvent alVolver;
        [SerializeField] private UnityEvent alVolverAInicio;

        private void Awake()
        {
            botonVolver.onClick.AddListener(Volver);
            botonGuardarResultados.onClick.AddListener(GuardarResultados);
        }

        private void Volver()
        {
            if (ProyectoManager.ResultadosAbiertosDesdeInicio)
            {
                alVolverAInicio?.Invoke();
            }
            else
            {
                alVolver?.Invoke();
            }
        }

        private void Start()
        {
            textoEstadoGuardado.text = string.Empty;
            textoPeriodosSaturacion.gameObject.SetActive(false);

            EjecutarSimulacionResponse resultado = ProyectoManager.UltimaSimulacion;
            if (resultado == null)
            {
                textoEstadoGuardado.text = "Todavía no se ejecutó ninguna simulación.";
                textoEficiencia.text = "—";
                textoEficienciaDetalle.text = string.Empty;
                textoSaturacion.text = "—";
                textoSaturacionDetalle.text = string.Empty;
                textoDemanda.text = "—";
                textoDemandaDetalle.text = string.Empty;
                textoPuntuacion.text = "—";
                textoPuntuacionDetalle.text = string.Empty;
                botonGuardarResultados.interactable = false;
                return;
            }

            textoEficiencia.text = FormatoIndicadores.ValorEficiencia(resultado);
            textoEficienciaDetalle.text = FormatoIndicadores.DetalleEficiencia(resultado);
            textoSaturacion.text = FormatoIndicadores.ValorSaturacion(resultado);
            textoSaturacionDetalle.text = FormatoIndicadores.DetalleSaturacion(resultado, ProyectoManager.HoraInicioSimulacion);
            textoPeriodosSaturacion.text = FormatoIndicadores.PeriodosSaturacion(resultado, ProyectoManager.HoraInicioSimulacion);
            textoPeriodosSaturacion.gameObject.SetActive(textoPeriodosSaturacion.text.Length > 0);
            textoDemanda.text = FormatoIndicadores.ValorDemanda(resultado);
            textoDemandaDetalle.text = FormatoIndicadores.DetalleDemanda(resultado);
            textoPuntuacion.text = FormatoIndicadores.ValorPuntuacion(resultado);
            textoPuntuacionDetalle.text = FormatoIndicadores.DetallePuntuacion(resultado);
            MostrarCurva(resultado);

            if (ProyectoManager.SimulacionGuardada)
            {
                botonGuardarResultados.interactable = false;
                textoEstadoGuardado.text = "Estos resultados ya están guardados.";
            }
            else
            {
                botonGuardarResultados.interactable = true;
            }
        }

        private void GuardarResultados()
        {
            botonGuardarResultados.interactable = false;
            textoEstadoGuardado.text = "Guardando...";

            ApiClient.Post<EjecutarSimulacionResponse, GuardarSimulacionResponse>(
                $"/proyectos/{ProyectoManager.ProyectoId}/simulacion/guardar",
                ProyectoManager.UltimaSimulacion,
                alTenerExito: respuesta =>
                {
                    ProyectoManager.MarcarSimulacionGuardada();
                    textoEstadoGuardado.text = "Resultados guardados.";
                },
                alFallar: (mensaje, codigo) =>
                {
                    botonGuardarResultados.interactable = true;
                    textoEstadoGuardado.text = mensaje;
                });
        }

        private void MostrarCurva(EjecutarSimulacionResponse resultado)
        {
            int[] ocupadasPorMinuto = new int[resultado.curvaOcupacion.Length];
            for (int i = 0; i < resultado.curvaOcupacion.Length; i++)
            {
                ocupadasPorMinuto[i] = resultado.curvaOcupacion[i].cantidadOcupadas;
            }

            int totalPlazas = ContarPlazasGuardadas();
            grafico.MostrarCurva(ocupadasPorMinuto, totalPlazas,
                ProyectoManager.HoraInicioSimulacion, ProyectoManager.HoraFinSimulacion);
        }

        private static int ContarPlazasGuardadas()
        {
            PiezaDto[] piezas = ProyectoManager.PiezasACargar;
            if (piezas == null) return 0;

            int total = 0;
            foreach (PiezaDto pieza in piezas)
            {
                if (pieza.tipo == TipoPieza.PLAZA.ToString())
                {
                    total++;
                }
            }
            return total;
        }
    }
}
