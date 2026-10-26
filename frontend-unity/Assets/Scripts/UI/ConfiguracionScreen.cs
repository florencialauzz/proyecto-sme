using System.Text.RegularExpressions;
using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    public class ConfiguracionScreen : MonoBehaviour
    {
        private static readonly Regex FormatoHora = new Regex(@"^([01]\d|2[0-3]):[0-5]\d$");

        [SerializeField] private TMP_InputField campoCantidadPisos;
        [SerializeField] private TMP_InputField campoFrecuenciaIngreso;
        [SerializeField] private TMP_InputField campoTiempoPermanencia;
        [SerializeField] private TMP_InputField campoHoraInicio;
        [SerializeField] private TMP_InputField campoHoraFin;
        [SerializeField] private Toggle toggleFluctuaciones;
        [SerializeField] private Button botonGuardar;
        [SerializeField] private AvisoError avisoError;

        [SerializeField] private Button botonVolver;

        [SerializeField] private UnityEvent alGuardarConfiguracionConExito;
        [SerializeField] private UnityEvent alVolverAlEditor;
        [SerializeField] private UnityEvent alVolverAInicio;

        private void Awake()
        {
            botonGuardar.onClick.AddListener(Guardar);
            botonVolver.onClick.AddListener(Volver);
        }

        private void Volver()
        {
            if (ProyectoManager.ConfiguracionAbiertaDesdeEditor)
            {
                alVolverAlEditor?.Invoke();
            }
            else
            {
                alVolverAInicio?.Invoke();
            }
        }

        private void OnEnable()
        {
            OcultarError();
            PrecargarValores();
        }

        private void PrecargarValores()
        {
            if (ProyectoManager.CantidadPisos > 0)
            {
                campoCantidadPisos.text = ProyectoManager.CantidadPisos.ToString();
                campoFrecuenciaIngreso.text = ProyectoManager.FrecuenciaIngreso.ToString();
                campoTiempoPermanencia.text = ProyectoManager.TiempoPermanencia.ToString();
                campoHoraInicio.text = ProyectoManager.HoraInicioSimulacion;
                campoHoraFin.text = ProyectoManager.HoraFinSimulacion;
                toggleFluctuaciones.isOn = ProyectoManager.ConFluctuaciones;
            }
            else
            {
                toggleFluctuaciones.isOn = true;
            }
        }

        private void Guardar()
        {
            OcultarError();

            if (!int.TryParse(campoCantidadPisos.text, out int cantidadPisos)
                || !int.TryParse(campoFrecuenciaIngreso.text, out int frecuenciaIngreso)
                || !int.TryParse(campoTiempoPermanencia.text, out int tiempoPermanencia))
            {
                MostrarError("Ingresá valores numéricos válidos.");
                return;
            }

            string horaInicio = campoHoraInicio.text;
            string horaFin = campoHoraFin.text;
            if (!FormatoHora.IsMatch(horaInicio) || !FormatoHora.IsMatch(horaFin))
            {
                MostrarError("Ingresá los horarios en formato HH:mm.");
                return;
            }

            bool conFluctuaciones = toggleFluctuaciones.isOn;

            botonGuardar.interactable = false;

            var request = new GuardarConfiguracionRequest
            {
                cantidadPisos = cantidadPisos,
                frecuenciaIngreso = frecuenciaIngreso,
                tiempoPermanencia = tiempoPermanencia,
                horaInicioSimulacion = horaInicio,
                horaFinSimulacion = horaFin,
                conFluctuaciones = conFluctuaciones
            };

            ApiClient.Put<GuardarConfiguracionRequest, GuardarConfiguracionResponse>(
                $"/proyectos/{ProyectoManager.ProyectoId}/configuracion",
                request,
                alTenerExito: _ =>
                {
                    botonGuardar.interactable = true;
                    ProyectoManager.GuardarConfiguracion(cantidadPisos, frecuenciaIngreso, tiempoPermanencia,
                        horaInicio, horaFin, conFluctuaciones);
                    alGuardarConfiguracionConExito?.Invoke();
                },
                alFallar: (mensaje, codigo) =>
                {
                    botonGuardar.interactable = true;
                    MostrarError(mensaje);
                });
        }

        private void MostrarError(string mensaje)
        {
            avisoError.Mostrar(mensaje);
        }

        private void OcultarError()
        {
            avisoError.Ocultar();
        }
    }
}
