using Sme.Grid;
using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    // Controlador de la escena Resultados (RF-24): en una sola pantalla, la
    // curva de ocupación (RF-27), la eficiencia espacial (RF-15), el período
    // de saturación (RF-28), la demanda satisfecha (RF-29) y la puntuación
    // general (RF-30), más el botón para guardar los resultados (RF-23).
    //
    // El resultado lo deja en ProyectoManager.UltimaSimulacion EditorScreen
    // (recién ejecutado) o InicioScreen (el guardado, desde "Ver resultados").
    public class ResultadosScreen : MonoBehaviour
    {
        // Una tarjeta por indicador: el valor grande y una línea de detalle.
        [SerializeField] private TMP_Text textoEficiencia;
        [SerializeField] private TMP_Text textoEficienciaDetalle;
        [SerializeField] private TMP_Text textoSaturacion;
        [SerializeField] private TMP_Text textoSaturacionDetalle;
        [SerializeField] private TMP_Text textoDemanda;
        [SerializeField] private TMP_Text textoDemandaDetalle;
        [SerializeField] private TMP_Text textoPuntuacion;
        [SerializeField] private TMP_Text textoPuntuacionDetalle;

        // RF-28: la lista completa de intervalos de saturación, debajo del
        // gráfico (en la tarjeta solo va un resumen). Se oculta si no hubo.
        [SerializeField] private TMP_Text textoPeriodosSaturacion;
        // Las etiquetas de los ejes (horas y plazas) las arma el gráfico.
        [SerializeField] private GraficoCurva grafico;

        // RF-23
        [SerializeField] private Button botonGuardarResultados;
        [SerializeField] private TMP_Text textoEstadoGuardado;

        // Volver regresa a la pantalla desde la que se llegó: al Editor si se
        // acaba de simular, a Inicio si se entró por "Ver resultados".
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
                // RF-23, A1: sin simulación no hay nada que guardar.
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

        // RF-23: reenvía al backend el mismo resultado que devolvió /ejecutar.
        // Si el proyecto ya tenía resultados, el backend los reemplaza (A2).
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

        // RF-27: tiempo en el eje horizontal, plazas ocupadas en el vertical.
        // El techo del eje es el total de plazas colocadas, así se ve a
        // simple vista cuándo se llenó.
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

        // La respuesta de la simulación no trae el total de plazas, pero las
        // piezas guardadas del proyecto están en ProyectoManager (EditorScreen
        // guarda antes de simular, InicioScreen las trae al abrir). Cada Plaza
        // es una sola fila, en su ancla, así que se cuentan las filas de tipo
        // PLAZA.
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
