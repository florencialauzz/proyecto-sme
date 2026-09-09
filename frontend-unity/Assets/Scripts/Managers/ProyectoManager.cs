using Sme.Models;

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

        // RF-08 a RF-11: 0 (o null en horaInicioSimulacion/horaFinSimulacion)
        // significa que el proyecto todavía no tiene configuración cargada —
        // ConfiguracionScreen lo usa para decidir si precargar el formulario
        // al reabrir un proyecto.
        public static int CantidadPisos { get; private set; }
        public static int FrecuenciaIngreso { get; private set; }
        public static int TiempoPermanencia { get; private set; }
        public static string HoraInicioSimulacion { get; private set; }
        public static string HoraFinSimulacion { get; private set; }

        // Piezas ya guardadas de un proyecto que se está reabriendo (null si es
        // un proyecto recién creado, sin nada todavía) — GrillaGenerador las lee
        // para reconstruir la grilla al entrar a la escena Editor.
        public static PiezaDto[] PiezasACargar { get; private set; }

        public static void GuardarProyecto(long proyectoId, int filasGrilla, int columnasGrilla, string estado,
            PiezaDto[] piezasACargar = null, int cantidadPisos = 0, int frecuenciaIngreso = 0,
            int tiempoPermanencia = 0, string horaInicioSimulacion = null, string horaFinSimulacion = null)
        {
            ProyectoId = proyectoId;
            FilasGrilla = filasGrilla;
            ColumnasGrilla = columnasGrilla;
            Estado = estado;
            PiezasACargar = piezasACargar;
            CantidadPisos = cantidadPisos;
            FrecuenciaIngreso = frecuenciaIngreso;
            TiempoPermanencia = tiempoPermanencia;
            HoraInicioSimulacion = horaInicioSimulacion;
            HoraFinSimulacion = horaFinSimulacion;
        }

        // RF-08 a RF-11: se llama después de guardar la configuración con éxito,
        // para que quede disponible en memoria sin tener que volver a pedirla al backend.
        public static void GuardarConfiguracion(int cantidadPisos, int frecuenciaIngreso, int tiempoPermanencia,
            string horaInicioSimulacion, string horaFinSimulacion)
        {
            CantidadPisos = cantidadPisos;
            FrecuenciaIngreso = frecuenciaIngreso;
            TiempoPermanencia = tiempoPermanencia;
            HoraInicioSimulacion = horaInicioSimulacion;
            HoraFinSimulacion = horaFinSimulacion;
        }

        public static void CerrarProyecto()
        {
            ProyectoId = 0;
            FilasGrilla = 0;
            ColumnasGrilla = 0;
            Estado = null;
            PiezasACargar = null;
            CantidadPisos = 0;
            FrecuenciaIngreso = 0;
            TiempoPermanencia = 0;
            HoraInicioSimulacion = null;
            HoraFinSimulacion = null;
        }
    }
}
