using System.Globalization;
using System.Text;
using Sme.Models;

namespace Sme.UI
{
    // Arma los textos de los indicadores de una simulación. Lo usan la
    // pantalla de resultados (RF-24) y los modales de comparación (RF-25),
    // así los dos muestran los mismos números con el mismo formato.
    public static class FormatoIndicadores
    {
        private const int MinutosPorHora = 60;

        // RF-15
        public static string Eficiencia(EjecutarSimulacionResponse resultado)
        {
            return $"Eficiencia espacial: {Porcentaje(resultado.eficienciaEspacial)}% ({resultado.calificacionEficiencia})";
        }

        // RF-29
        public static string Demanda(EjecutarSimulacionResponse resultado)
        {
            return $"Demanda satisfecha: {Porcentaje(resultado.demandaSatisfecha)}% " +
                   $"({resultado.vehiculosRechazados} vehículos rechazados)";
        }

        // RF-30
        public static string Puntuacion(EjecutarSimulacionResponse resultado)
        {
            return $"Puntuación general: {Porcentaje(resultado.puntuacionGeneral)} / 100 ({resultado.calificacionTexto})";
        }

        // RF-28: los intervalos en horas del día, seguidos y separados por
        // coma (uno por renglón empujaba la gráfica cuando eran varios). Un
        // intervalo [inicio, fin] incluye el minuto fin entero, así que la
        // hora de cierre es la del minuto siguiente. Sin intervalos, Flujo
        // Alternativo A1.
        public static string Saturacion(EjecutarSimulacionResponse resultado, string horaInicioSimulacion)
        {
            PeriodoSaturacionDto[] periodos = resultado.periodosSaturacion;
            if (periodos == null || periodos.Length == 0)
            {
                return "Período de saturación: no hubo saturación.";
            }

            int minutoInicioDelDia = MinutoDelDia(horaInicioSimulacion);
            var texto = new StringBuilder("Período de saturación: ");
            for (int i = 0; i < periodos.Length; i++)
            {
                if (i > 0)
                {
                    texto.Append(", ");
                }
                string desde = HoraDelDia(minutoInicioDelDia + periodos[i].inicio);
                string hasta = HoraDelDia(minutoInicioDelDia + periodos[i].fin + 1);
                texto.Append($"de {desde} a {hasta}");
            }
            return texto.ToString();
        }

        // RF-25: todos los indicadores juntos, para un modal de comparación.
        // Etiquetas cortas a propósito: los modales son angostos (van dos uno
        // al lado del otro). La saturación va resumida en intervalos y
        // minutos totales, sin las horas (GET /simulacion no trae el horario
        // de cada proyecto, y para comparar alcanza con cuánto tiempo saturó).
        public static string Resumen(EjecutarSimulacionResponse resultado)
        {
            return $"Eficiencia: {Porcentaje(resultado.eficienciaEspacial)}% ({resultado.calificacionEficiencia})\n"
                   + $"Demanda: {Porcentaje(resultado.demandaSatisfecha)}% ({resultado.vehiculosRechazados} rechazados)\n"
                   + SaturacionResumida(resultado) + "\n"
                   + $"Puntuación: {Porcentaje(resultado.puntuacionGeneral)}/100 ({resultado.calificacionTexto})";
        }

        private static string SaturacionResumida(EjecutarSimulacionResponse resultado)
        {
            PeriodoSaturacionDto[] periodos = resultado.periodosSaturacion;
            if (periodos == null || periodos.Length == 0)
            {
                return "Saturación: no hubo";
            }

            int minutosSaturados = 0;
            foreach (PeriodoSaturacionDto periodo in periodos)
            {
                minutosSaturados += periodo.fin - periodo.inicio + 1;
            }

            string intervalos = periodos.Length == 1 ? "1 intervalo" : $"{periodos.Length} intervalos";
            return $"Saturación: {intervalos}, {minutosSaturados} min";
        }

        private static string Porcentaje(float valor)
        {
            return valor.ToString("0.00", CultureInfo.InvariantCulture);
        }

        // Las horas llegan como "HH:mm" o "HH:mm:ss" (mismo criterio que GraficoCurva).
        // Públicas porque el reloj de la reproducción (ModoReproduccion) muestra
        // la hora simulada con el mismo formato.
        public static int MinutoDelDia(string hora)
        {
            if (string.IsNullOrEmpty(hora)) return 0;

            string[] partes = hora.Split(':');
            int horas = int.Parse(partes[0]);
            int minutos = int.Parse(partes[1]);
            return horas * MinutosPorHora + minutos;
        }

        public static string HoraDelDia(int minutoDelDia)
        {
            int horas = minutoDelDia / MinutosPorHora % 24;
            int minutos = minutoDelDia % MinutosPorHora;
            return $"{horas:00}:{minutos:00}";
        }
    }
}
