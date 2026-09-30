using System.Globalization;
using Sme.Grid;
using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    // Controlador de la escena Resultados. En Iteración 3 muestra la
    // eficiencia espacial con su calificación (RF-15) y la curva de ocupación
    // (RF-27). En Iteración 4 esta misma pantalla pasa a ser la de RF-24:
    // se le suman saturación, demanda, puntuación y el botón de guardar.
    //
    // El resultado lo deja EditorScreen en ProyectoManager.UltimaSimulacion
    // antes de cambiar de escena.
    public class ResultadosScreen : MonoBehaviour
    {
        [SerializeField] private TMP_Text textoEficiencia;
        // Las etiquetas de los ejes (horas y plazas) las arma el gráfico.
        [SerializeField] private GraficoCurva grafico;

        [SerializeField] private Button botonVolver;
        [SerializeField] private UnityEvent alVolver;

        private void Awake()
        {
            botonVolver.onClick.AddListener(() => alVolver?.Invoke());
        }

        private void Start()
        {
            EjecutarSimulacionResponse resultado = ProyectoManager.UltimaSimulacion;
            if (resultado == null)
            {
                textoEficiencia.text = "Todavía no se ejecutó ninguna simulación.";
                return;
            }

            MostrarEficiencia(resultado);
            MostrarCurva(resultado);
        }

        // RF-15: porcentaje de celdas de la grilla ocupadas por plazas, y su
        // calificación contra el techo teórico (la calcula el backend).
        private void MostrarEficiencia(EjecutarSimulacionResponse resultado)
        {
            string porcentaje = resultado.eficienciaEspacial.ToString("0.00", CultureInfo.InvariantCulture);
            textoEficiencia.text = $"Eficiencia espacial: {porcentaje}% ({resultado.calificacionEficiencia})";
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
        // piezas guardadas justo antes de simular están en ProyectoManager
        // (EditorScreen guarda y después simula). Cada Plaza es una sola
        // fila, en su ancla, así que se cuentan las filas de tipo PLAZA.
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
