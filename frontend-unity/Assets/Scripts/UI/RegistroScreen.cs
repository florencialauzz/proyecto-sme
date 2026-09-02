using Sme.Managers;
using Sme.Models;
using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    // RF-01: Registrar usuario. Requiere que los campos de abajo estén asignados
    // en el Inspector, sobre el Canvas de la pantalla de registro.
    public class RegistroScreen : MonoBehaviour
    {
        [SerializeField] private TMP_InputField campoNombreUsuario;
        [SerializeField] private TMP_InputField campoContrasena;
        [SerializeField] private TMP_InputField campoConfirmacionContrasena;
        [SerializeField] private TMP_Dropdown campoPreguntaSeguridad;
        [SerializeField] private TMP_InputField campoRespuestaSeguridad;
        [SerializeField] private Button botonRegistrar;
        [SerializeField] private TMP_Text textoError;

        [SerializeField] private UnityEvent alRegistrarseConExito;

        private void Awake()
        {
            CargarOpcionesPreguntaSeguridad();
            botonRegistrar.onClick.AddListener(Registrar);
            OcultarError();
        }

        // El usuario elige de una picklist en vez de escribir su propia pregunta
        // de seguridad — ver Models/PreguntasSeguridad.cs.
        private void CargarOpcionesPreguntaSeguridad()
        {
            campoPreguntaSeguridad.ClearOptions();
            campoPreguntaSeguridad.AddOptions(new List<string>(PreguntasSeguridad.Opciones));
        }

        private void Registrar()
        {
            OcultarError();
            botonRegistrar.interactable = false;

            var request = new RegistroRequest
            {
                nombreUsuario = campoNombreUsuario.text,
                contrasena = campoContrasena.text,
                confirmacionContrasena = campoConfirmacionContrasena.text,
                preguntaSeguridad = campoPreguntaSeguridad.options[campoPreguntaSeguridad.value].text,
                respuestaSeguridad = campoRespuestaSeguridad.text
            };

            ApiClient.Post<RegistroRequest, RegistroResponse>(
                "/auth/registro",
                request,
                alTenerExito: _ =>
                {
                    botonRegistrar.interactable = true;
                    alRegistrarseConExito?.Invoke();
                },
                alFallar: (mensaje, codigo) =>
                {
                    // A1 (usuario duplicado) y A2 (confirmación no coincide) llegan acá
                    // según el "codigo" del error — el mensaje ya viene listo para mostrar.
                    botonRegistrar.interactable = true;
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
