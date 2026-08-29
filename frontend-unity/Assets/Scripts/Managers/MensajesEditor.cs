using TMPro;

namespace Sme.Managers
{
    // RF-13 (Flujo alternativo A1): notifica "celda ocupada o fuera de los
    // límites" cuando una colocación o movimiento no es válido. CatalogoItemPlaza
    // y PiezaView se instancian dinámicamente (no son objetos fijos de la escena),
    // así que no pueden tener el TMP_Text asignado a mano en el Inspector — por
    // eso el bridge estático, igual que SesionManager/ProyectoManager.
    public static class MensajesEditor
    {
        private static TMP_Text texto;

        public static void Registrar(TMP_Text textoMensaje)
        {
            texto = textoMensaje;
        }

        public static void Mostrar(string mensaje)
        {
            if (texto == null) return;

            texto.text = mensaje;
            texto.gameObject.SetActive(true);
        }
    }
}
