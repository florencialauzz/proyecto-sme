using Sme.UI;

namespace Sme.Managers
{
    // RF-13 (Flujo alternativo A1): notifica "celda ocupada o fuera de los
    // límites" cuando una colocación o movimiento no es válido. CatalogoItemPlaza
    // y PiezaView se instancian dinámicamente (no son objetos fijos de la escena),
    // así que no pueden tener el EditorScreen asignado a mano en el Inspector —
    // por eso el bridge estático, igual que SesionManager/ProyectoManager. El
    // ocultamiento automático del texto lo maneja EditorScreen (necesita una
    // corutina, y esta clase no es un MonoBehaviour).
    public static class MensajesEditor
    {
        private static EditorScreen pantalla;

        public static void Registrar(EditorScreen editorScreen)
        {
            pantalla = editorScreen;
        }

        public static void Mostrar(string mensaje)
        {
            pantalla?.MostrarMensaje(mensaje);
        }
    }
}
