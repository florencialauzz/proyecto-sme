using System.Globalization;
using System.Text;
using Sme.Models;

namespace Sme.UI
{
    public static class FormatoIndicadores
    {
        private const int MinutosPorHora = 60;

        public static string ValorEficiencia(EjecutarSimulacionResponse resultado)
        {
            return $"{Porcentaje(resultado.eficienciaEspacial)}%";
        }

        public static string DetalleEficiencia(EjecutarSimulacionResponse resultado)
        {
            return $"Calificación: {resultado.calificacionEficiencia}";
        }

        public static string ValorDemanda(EjecutarSimulacionResponse resultado)
        {
            return $"{Porcentaje(resultado.demandaSatisfecha)}%";
        }

        public static string DetalleDemanda(EjecutarSimulacionResponse resultado)
        {
            return resultado.vehiculosRechazados == 1
                ? "1 vehículo rechazado"
                : $"{resultado.vehiculosRechazados} vehículos rechazados";
        }

        public static string ValorSaturacion(EjecutarSimulacionResponse resultado)
        {
            PeriodoSaturacionDto[] periodos = resultado.periodosSaturacion;
            if (periodos == null || periodos.Length == 0)
            {
                return "Sin saturación";
            }

            return $"{MinutosSaturados(periodos)} min";
        }

        public static string DetalleSaturacion(EjecutarSimulacionResponse resultado, string horaInicioSimulacion)
        {
            PeriodoSaturacionDto[] periodos = resultado.periodosSaturacion;
            if (periodos == null || periodos.Length == 0)
            {
                return "El estacionamiento nunca se llenó.";
            }
            if (periodos.Length == 1)
            {
                string intervalo = IntervalosSaturacion(periodos, horaInicioSimulacion);
                return char.ToUpperInvariant(intervalo[0]) + intervalo.Substring(1);
            }
            return $"En {periodos.Length} intervalos (detalle debajo del gráfico)";
        }

        public static string PeriodosSaturacion(EjecutarSimulacionResponse resultado, string horaInicioSimulacion)
        {
            PeriodoSaturacionDto[] periodos = resultado.periodosSaturacion;
            if (periodos == null || periodos.Length == 0)
            {
                return string.Empty;
            }
            return "Períodos de saturación: " + IntervalosSaturacion(periodos, horaInicioSimulacion) + ".";
        }

        public static string ValorPuntuacion(EjecutarSimulacionResponse resultado)
        {
            return $"{Porcentaje(resultado.puntuacionGeneral)}<size=55%> / 100</size>";
        }

        public static string DetallePuntuacion(EjecutarSimulacionResponse resultado)
        {
            return resultado.calificacionTexto;
        }

        private static string IntervalosSaturacion(PeriodoSaturacionDto[] periodos, string horaInicioSimulacion)
        {
            int minutoInicioDelDia = MinutoDelDia(horaInicioSimulacion);
            var texto = new StringBuilder();
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

        private static int MinutosSaturados(PeriodoSaturacionDto[] periodos)
        {
            int minutosSaturados = 0;
            foreach (PeriodoSaturacionDto periodo in periodos)
            {
                minutosSaturados += periodo.fin - periodo.inicio + 1;
            }
            return minutosSaturados;
        }

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

            int minutosSaturados = MinutosSaturados(periodos);

            string intervalos = periodos.Length == 1 ? "1 intervalo" : $"{periodos.Length} intervalos";
            return $"Saturación: {intervalos}, {minutosSaturados} min";
        }

        private static string Porcentaje(float valor)
        {
            return valor.ToString("0.00", CultureInfo.InvariantCulture);
        }

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
