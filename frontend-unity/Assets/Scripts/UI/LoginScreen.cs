using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    // RF-02: Iniciar sesión. Requiere que los campos de abajo estén asignados
    // en el Inspector, sobre el Canvas de la pantalla de login.
    public class LoginScreen : MonoBehaviour
    {
        [SerializeField] private TMP_InputField campoNombreUsuario;
        [SerializeField] private TMP_InputField campoContrasena;
        [SerializeField] private Button botonIniciarSesion;
        [SerializeField] private TMP_Text textoError;

        [SerializeField] private UnityEvent alIniciarSesionConExito;

        private void Awake()
        {
            botonIniciarSesion.onClick.AddListener(IniciarSesion);
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
                    SesionManager.GuardarSesion(respuesta.usuarioId, respuesta.token);
                    alIniciarSesionConExito?.Invoke();
                },
                alFallar: (mensaje, codigo) =>
                {
                    // A1 (usuario incorrecto) y A2 (contraseña incorrecta) llegan acá
                    // según el "codigo" del error — el mensaje ya viene listo para mostrar.
                    botonIniciarSesion.interactable = true;
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
