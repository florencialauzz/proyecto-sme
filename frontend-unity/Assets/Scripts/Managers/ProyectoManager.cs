namespace Sme.Managers
{
    // Datos del proyecto actualmente abierto, en memoria (mismo criterio que
    // SesionManager: vive mientras dure el proceso del cliente, no persiste
    // entre ejecuciones a propósito).
    public static class ProyectoManager
    {
        public static long ProyectoId { get; private set; }
        public static int FilasGrilla { get; private set; }
        public static int ColumnasGrilla { get; private set; }
        public static string Estado { get; private set; }

        public static void GuardarProyecto(long proyectoId, int filasGrilla, int columnasGrilla, string estado)
        {
            ProyectoId = proyectoId;
            FilasGrilla = filasGrilla;
            ColumnasGrilla = columnasGrilla;
            Estado = estado;
        }

        public static void CerrarProyecto()
        {
            ProyectoId = 0;
            FilasGrilla = 0;
            ColumnasGrilla = 0;
            Estado = null;
        }
    }
}
