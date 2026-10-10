namespace Sme.Managers
{
    // Sesión JWT en memoria (decisiones.md: stateless, sin cookies). Vive mientras
    // dure el proceso del cliente — no persiste entre ejecuciones a propósito.
    public static class SesionManager
    {
        public static string Token { get; private set; }
        public static long UsuarioId { get; private set; }
        public static bool HaySesionActiva => !string.IsNullOrEmpty(Token);

        // Solo para mostrarlo en el perfil de Inicio: la respuesta del login
        // no lo trae, así que se guarda el que el usuario escribió.
        public static string NombreUsuario { get; private set; }

        public static void GuardarSesion(long usuarioId, string token, string nombreUsuario)
        {
            UsuarioId = usuarioId;
            Token = token;
            NombreUsuario = nombreUsuario;
        }

        // RF-06: JWT stateless, no hay nada que avisarle al backend. Alcanza con
        // borrar el token en el cliente.
        public static void CerrarSesion()
        {
            UsuarioId = 0;
            Token = null;
            NombreUsuario = null;
        }
    }
}
