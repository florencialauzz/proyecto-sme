namespace Sme.Managers
{
    public static class SesionManager
    {
        public static string Token { get; private set; }
        public static long UsuarioId { get; private set; }
        public static bool HaySesionActiva => !string.IsNullOrEmpty(Token);

        public static string NombreUsuario { get; private set; }

        public static void GuardarSesion(long usuarioId, string token, string nombreUsuario)
        {
            UsuarioId = usuarioId;
            Token = token;
            NombreUsuario = nombreUsuario;
        }

        public static void CerrarSesion()
        {
            UsuarioId = 0;
            Token = null;
            NombreUsuario = null;
        }
    }
}
