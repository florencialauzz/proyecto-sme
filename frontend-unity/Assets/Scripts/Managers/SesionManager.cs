namespace Sme.Managers
{
    // Sesión JWT en memoria (decisiones.md: stateless, sin cookies). Vive mientras
    // dure el proceso del cliente — no persiste entre ejecuciones a propósito.
    public static class SesionManager
    {
        public static string Token { get; private set; }
        public static long UsuarioId { get; private set; }
        public static bool HaySesionActiva => !string.IsNullOrEmpty(Token);

        public static void GuardarSesion(long usuarioId, string token)
        {
            UsuarioId = usuarioId;
            Token = token;
        }
    }
}
