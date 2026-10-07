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

        // Checkbox de la configuración: la simulación sortea llegadas y
        // permanencias dentro de ±50% del valor ingresado (lo hace el backend).
        public static bool ConFluctuaciones { get; private set; }

        // Piezas ya guardadas de un proyecto que se está reabriendo (null si es
        // un proyecto recién creado, sin nada todavía) — GrillaGenerador las lee
        // para reconstruir la grilla al entrar a la escena Editor.
        public static PiezaDto[] PiezasACargar { get; private set; }

        // RF-26: el resultado que muestra la escena Resultados al abrir (mismo
        // criterio que PiezasACargar para la escena Editor): el de la última
        // simulación ejecutada, o el guardado si se llegó desde "Ver
        // resultados" en Inicio (RF-24). null si no hay ninguno.
        public static EjecutarSimulacionResponse UltimaSimulacion { get; private set; }

        // RF-23: si UltimaSimulacion ya está guardada en el backend. La
        // pantalla de resultados lo usa para habilitar o no el botón Guardar.
        public static bool SimulacionGuardada { get; private set; }

        // Si se llegó a Resultados desde "Ver resultados" en Inicio (true) o
        // simulando desde el Editor (false): el botón Volver regresa a esa
        // pantalla.
        public static bool ResultadosAbiertosDesdeInicio { get; private set; }

        // Si se llegó a Configuración desde el Editor (true) o desde Inicio, al
        // crear o abrir un proyecto (false): el botón Volver regresa a esa
        // pantalla.
        public static bool ConfiguracionAbiertaDesdeEditor { get; private set; }

        public static void GuardarProyecto(long proyectoId, int filasGrilla, int columnasGrilla, string estado,
            PiezaDto[] piezasACargar = null, int cantidadPisos = 0, int frecuenciaIngreso = 0,
            int tiempoPermanencia = 0, string horaInicioSimulacion = null, string horaFinSimulacion = null,
            bool conFluctuaciones = false)
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
            ConFluctuaciones = conFluctuaciones;
            UltimaSimulacion = null;
            SimulacionGuardada = false;
            ResultadosAbiertosDesdeInicio = false;
            ConfiguracionAbiertaDesdeEditor = false;
        }

        public static void MarcarConfiguracionAbiertaDesdeEditor()
        {
            ConfiguracionAbiertaDesdeEditor = true;
        }

        // RF-08 a RF-11: se llama después de guardar la configuración con éxito,
        // para que quede disponible en memoria sin tener que volver a pedirla al backend.
        public static void GuardarConfiguracion(int cantidadPisos, int frecuenciaIngreso, int tiempoPermanencia,
            string horaInicioSimulacion, string horaFinSimulacion, bool conFluctuaciones)
        {
            CantidadPisos = cantidadPisos;
            FrecuenciaIngreso = frecuenciaIngreso;
            TiempoPermanencia = tiempoPermanencia;
            HoraInicioSimulacion = horaInicioSimulacion;
            HoraFinSimulacion = horaFinSimulacion;
            ConFluctuaciones = conFluctuaciones;
        }

        // RF-21: se llama después de guardar la grilla con éxito. Sin esto,
        // ir a Configuración y volver al Editor recarga la escena con las
        // piezas que había al abrir el proyecto, no con las recién guardadas.
        // cantidadPisos cambia acá cuando se eliminó un piso desde el editor
        // (RF-18).
        public static void GuardarGrilla(PiezaDto[] piezas, int cantidadPisos)
        {
            PiezasACargar = piezas;

            // 0 significa "sin configuración todavía" (ver arriba): el editor
            // igual guarda 1 piso, pero no hay que hacer creer a
            // ConfiguracionScreen que ya hay una configuración para precargar.
            if (CantidadPisos > 0)
            {
                CantidadPisos = cantidadPisos;
            }
        }

        // yaGuardada: true cuando el resultado viene de GET /simulacion (RF-24),
        // false cuando recién se ejecutó y todavía no se guardó.
        public static void GuardarSimulacion(EjecutarSimulacionResponse resultado, bool yaGuardada,
            bool abiertoDesdeInicio)
        {
            UltimaSimulacion = resultado;
            SimulacionGuardada = yaGuardada;
            ResultadosAbiertosDesdeInicio = abiertoDesdeInicio;
        }

        // RF-23: se llama después de que POST /simulacion/guardar salió bien.
        public static void MarcarSimulacionGuardada()
        {
            SimulacionGuardada = true;
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
            ConFluctuaciones = false;
            UltimaSimulacion = null;
            SimulacionGuardada = false;
            ResultadosAbiertosDesdeInicio = false;
            ConfiguracionAbiertaDesdeEditor = false;
        }
    }
}
