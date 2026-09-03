using System;

namespace Sme.Models
{
    // Espejo de contratos/api-contract.md — nombres de campo iguales al JSON,
    // JsonUtility serializa por nombre de campo público.

    [Serializable]
    public class RegistroRequest
    {
        public string nombreUsuario;
        public string contrasena;
        public string confirmacionContrasena;
        public string preguntaSeguridad;
        public string respuestaSeguridad;
    }

    [Serializable]
    public class RegistroResponse
    {
        public long usuarioId;
    }

    [Serializable]
    public class LoginRequest
    {
        public string nombreUsuario;
        public string contrasena;
    }

    [Serializable]
    public class LoginResponse
    {
        public string token;
        public long usuarioId;
    }

    [Serializable]
    public class PreguntaSeguridadResponse
    {
        public string preguntaSeguridad;
    }

    // RF-03
    [Serializable]
    public class RecuperarContrasenaRequest
    {
        public string nombreUsuario;
        public string respuestaSeguridad;
        public string nuevaContrasena;
        public string confirmacionNuevaContrasena;
    }

    [Serializable]
    public class RecuperarContrasenaResponse
    {
        public bool actualizado;
    }

    // RF-04
    [Serializable]
    public class CambiarContrasenaRequest
    {
        public string contrasenaActual;
        public string contrasenaNueva;
        public string confirmacionContrasenaNueva;
    }

    [Serializable]
    public class CambiarContrasenaResponse
    {
        public bool actualizado;
    }

    // RF-05
    [Serializable]
    public class EliminarCuentaRequest
    {
        public string contrasena;
    }

    [Serializable]
    public class EliminarCuentaResponse
    {
        public bool eliminado;
    }
}
