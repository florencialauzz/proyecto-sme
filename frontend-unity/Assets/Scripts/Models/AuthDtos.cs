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
}
