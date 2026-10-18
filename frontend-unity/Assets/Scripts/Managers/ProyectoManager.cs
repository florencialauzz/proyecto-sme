using Sme.Models;

namespace Sme.Managers
{
    public static class ProyectoManager
    {
        public static long ProyectoId { get; private set; }
        public static int FilasGrilla { get; private set; }
        public static int ColumnasGrilla { get; private set; }
        public static string Estado { get; private set; }

        public static int CantidadPisos { get; private set; }
        public static int FrecuenciaIngreso { get; private set; }
        public static int TiempoPermanencia { get; private set; }
        public static string HoraInicioSimulacion { get; private set; }
        public static string HoraFinSimulacion { get; private set; }

        public static bool ConFluctuaciones { get; private set; }

        public static PiezaDto[] PiezasACargar { get; private set; }

        public static EjecutarSimulacionResponse UltimaSimulacion { get; private set; }

        public static bool SimulacionGuardada { get; private set; }

        public static bool ResultadosAbiertosDesdeInicio { get; private set; }

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

        public static void GuardarGrilla(PiezaDto[] piezas, int cantidadPisos)
        {
            PiezasACargar = piezas;

            if (CantidadPisos > 0)
            {
                CantidadPisos = cantidadPisos;
            }
        }

        public static void GuardarSimulacion(EjecutarSimulacionResponse resultado, bool yaGuardada,
            bool abiertoDesdeInicio)
        {
            UltimaSimulacion = resultado;
            SimulacionGuardada = yaGuardada;
            ResultadosAbiertosDesdeInicio = abiertoDesdeInicio;
        }

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
