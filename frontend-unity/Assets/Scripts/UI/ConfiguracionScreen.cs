using System.Text.RegularExpressions;
using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    // RF-08 Ingresar cantidad de pisos, RF-09 Ingresar frecuencia, RF-10 Ingresar
    // tiempo de permanencia, RF-11 Ingresar horario de simulación. Requiere que
    // los campos de abajo estén asignados en el Inspector, sobre el Canvas de la
    // pantalla de configuración (a la que se llega después de crear o abrir un
    // proyecto, antes del Editor).
    public class ConfiguracionScreen : MonoBehaviour
    {
        private static readonly Regex FormatoHora = new Regex(@"^([01]\d|2[0-3]):[0-5]\d$");

        [SerializeField] private TMP_InputField campoCantidadPisos;
        [SerializeField] private TMP_InputField campoFrecuenciaIngreso;
        [SerializeField] private TMP_InputField campoTiempoPermanencia;
        [SerializeField] private TMP_InputField campoHoraInicio;
        [SerializeField] private TMP_InputField campoHoraFin;
        [SerializeField] private Button botonGuardar;
        [SerializeField] private TMP_Text textoError;

        [SerializeField] private UnityEvent alGuardarConfiguracionConExito;

        private void Awake()
        {
            botonGuardar.onClick.AddListener(Guardar);
        }

        // Este panel no recarga la escena al mostrarse de nuevo, así que Awake no
        // alcanza para limpiar un error que quedó de una visita anterior —
        // OnEnable corre cada vez que el panel se reactiva. También precarga los
        // valores ya guardados si el proyecto se está reabriendo (ProyectoManager
        // los trae desde GET /proyectos/{id}).
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
            }
        }

        private void Guardar()
        {
            OcultarError();

            // Estas comprobaciones son solo para poder armar un request con los
            // tipos correctos (JsonUtility no serializa un int o una hora mal
            // formada) — las reglas de negocio en sí (RF-08 a RF-11: mínimos,
            // hora fin posterior a hora inicio) las valida el backend, igual que
            // el resto de las pantallas del sistema.
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

            botonGuardar.interactable = false;

            var request = new GuardarConfiguracionRequest
            {
                cantidadPisos = cantidadPisos,
                frecuenciaIngreso = frecuenciaIngreso,
                tiempoPermanencia = tiempoPermanencia,
                horaInicioSimulacion = horaInicio,
                horaFinSimulacion = horaFin
            };

            ApiClient.Put<GuardarConfiguracionRequest, GuardarConfiguracionResponse>(
                $"/proyectos/{ProyectoManager.ProyectoId}/configuracion",
                request,
                alTenerExito: _ =>
                {
                    botonGuardar.interactable = true;
                    ProyectoManager.GuardarConfiguracion(cantidadPisos, frecuenciaIngreso, tiempoPermanencia, horaInicio, horaFin);
                    alGuardarConfiguracionConExito?.Invoke();
                },
                alFallar: (mensaje, codigo) =>
                {
                    // A1 de cada RF (RF-08 a RF-11) llega acá según el "codigo" del
                    // error — el mensaje ya viene listo para mostrar.
                    botonGuardar.interactable = true;
                    MostrarError(mensaje);
                });
        }

        private void MostrarError(string mensaje)
        {
            textoError.text = mensaje;
            textoError.gameObject.SetActive(true);
        }

        private void OcultarError()
        {
            textoError.text = string.Empty;
            textoError.gameObject.SetActive(false);
        }
    }
}
