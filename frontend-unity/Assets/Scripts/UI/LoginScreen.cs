using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    public class LoginScreen : MonoBehaviour
    {
        [SerializeField] private TMP_InputField campoNombreUsuario;
        [SerializeField] private TMP_InputField campoContrasena;
        [SerializeField] private Button botonIniciarSesion;
        [SerializeField] private AvisoError avisoError;

        [SerializeField] private UnityEvent alIniciarSesionConExito;

        private void Awake()
        {
            botonIniciarSesion.onClick.AddListener(IniciarSesion);
        }

        private void OnEnable()
        {
            OcultarError();
        }

        private void IniciarSesion()
        {
            OcultarError();
            botonIniciarSesion.interactable = false;

            var request = new LoginRequest
            {
                nombreUsuario = campoNombreUsuario.text,
                contrasena = campoContrasena.text
            };

            ApiClient.Post<LoginRequest, LoginResponse>(
                "/auth/login",
                request,
                alTenerExito: respuesta =>
                {
                    botonIniciarSesion.interactable = true;
                    SesionManager.GuardarSesion(respuesta.usuarioId, respuesta.token, request.nombreUsuario);
                    alIniciarSesionConExito?.Invoke();
                },
                alFallar: (mensaje, codigo) =>
                {
                    botonIniciarSesion.interactable = true;
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
