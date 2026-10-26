using Sme.Managers;
using Sme.Models;
using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    public class RegistroScreen : MonoBehaviour
    {
        [SerializeField] private TMP_InputField campoNombreUsuario;
        [SerializeField] private TMP_InputField campoContrasena;
        [SerializeField] private TMP_InputField campoConfirmacionContrasena;
        [SerializeField] private TMP_Dropdown campoPreguntaSeguridad;
        [SerializeField] private TMP_InputField campoRespuestaSeguridad;
        [SerializeField] private Button botonRegistrar;
        [SerializeField] private AvisoError avisoError;

        [SerializeField] private UnityEvent alRegistrarseConExito;

        private void Awake()
        {
            CargarOpcionesPreguntaSeguridad();
            botonRegistrar.onClick.AddListener(Registrar);
        }

        private void OnEnable()
        {
            OcultarError();
        }

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
                    botonRegistrar.interactable = true;
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
