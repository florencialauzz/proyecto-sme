using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    // RF-03: Olvidó su contraseña. Se accede desde la pantalla de login. Dos
    // pasos sobre el mismo Canvas: primero se pide el nombre de usuario (A1 si
    // no existe) y se muestra su pregunta de seguridad; después se piden la
    // respuesta y la nueva contraseña juntas, igual que Registro junta todos
    // sus campos en un solo submit (A2 respuesta incorrecta, A3 confirmación
    // no coincide se resuelven ahí, del lado del servidor).
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

        [SerializeField] private TMP_Text textoError;

        [SerializeField] private UnityEvent alRecuperarConExito;

        private string nombreUsuario;

        private void Awake()
        {
            botonContinuar.onClick.AddListener(BuscarPreguntaSeguridad);
            botonRecuperar.onClick.AddListener(RecuperarContrasena);
            MostrarPanelUsuario();
        }

        private void MostrarPanelUsuario()
        {
            OcultarError();
            panelUsuario.SetActive(true);
            panelRespuesta.SetActive(false);
        }

        // Flujo principal, puntos 2-5.
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
                    // A1: nombre de usuario inexistente, vuelve al punto 2.
                    botonContinuar.interactable = true;
                    MostrarError(mensaje);
                });
        }

        // Flujo principal, puntos 6-11.
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
                    // A2 (respuesta incorrecta) y A3 (confirmación no coincide) llegan
                    // acá según el "codigo" del error — ambas vuelven a este mismo panel.
                    botonRecuperar.interactable = true;
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
