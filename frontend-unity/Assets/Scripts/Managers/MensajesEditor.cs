using Sme.UI;

namespace Sme.Managers
{
    public static class MensajesEditor
    {
        private static EditorScreen pantalla;

        public static void Registrar(EditorScreen editorScreen)
        {
            pantalla = editorScreen;
        }

        public static void MostrarError(string mensaje)
        {
            pantalla?.MostrarError(mensaje);
        }
    }
}
