using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    public class RecuperarContrasenaScreen : MonoBehaviour
    {
        [SerializeField] private GameObject panelUsuario;
        [SerializeField] private TMP_InputField campoNombreUsuario;
        [SerializeField] private Button botonContinuar;

        [SerializeField] private GameObject panelRespuesta;
        [SerializeField] private TMP_Text textoPreguntaSeguridad;
        [SerializeField] private TMP_InputField campoRespuestaSeguridad;
        [SerializeField] private TMP_InputField campoNuevaContrasena;
        [SerializeField] private TMP_InputField campoConfirmacionNuevaContrasena;
        [SerializeField] private Button botonRecuperar;

        [SerializeField] private AvisoError avisoError;

        [SerializeField] private UnityEvent alRecuperarConExito;

        private string nombreUsuario;

        private void Awake()
        {
            botonContinuar.onClick.AddListener(BuscarPreguntaSeguridad);
            botonRecuperar.onClick.AddListener(RecuperarContrasena);
        }

        private void OnEnable()
        {
            MostrarPanelUsuario();
        }

        private void MostrarPanelUsuario()
        {
            OcultarError();
            panelUsuario.SetActive(true);
            panelRespuesta.SetActive(false);
        }

        private void BuscarPreguntaSeguridad()
        {
            OcultarError();
            botonContinuar.interactable = false;

            ApiClient.ObtenerPreguntaSeguridad(
                campoNombreUsuario.text,
                alTenerExito: respuesta =>
                {
                    botonContinuar.interactable = true;
                    nombreUsuario = campoNombreUsuario.text;
                    textoPreguntaSeguridad.text = respuesta.preguntaSeguridad;
                    panelUsuario.SetActive(false);
                    panelRespuesta.SetActive(true);
                },
                alFallar: (mensaje, codigo) =>
                {
                    botonContinuar.interactable = true;
                    MostrarError(mensaje);
                });
        }

        private void RecuperarContrasena()
        {
            OcultarError();
            botonRecuperar.interactable = false;

            var request = new RecuperarContrasenaRequest
            {
                nombreUsuario = nombreUsuario,
                respuestaSeguridad = campoRespuestaSeguridad.text,
                nuevaContrasena = campoNuevaContrasena.text,
                confirmacionNuevaContrasena = campoConfirmacionNuevaContrasena.text
            };

            ApiClient.Post<RecuperarContrasenaRequest, RecuperarContrasenaResponse>(
                "/auth/recuperar-contrasena",
                request,
                alTenerExito: _ =>
                {
                    botonRecuperar.interactable = true;
                    alRecuperarConExito?.Invoke();
                },
                alFallar: (mensaje, codigo) =>
                {
                    botonRecuperar.interactable = true;
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
