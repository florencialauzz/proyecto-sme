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

        // --- Tarjetas de la pantalla de Resultados (RF-24) ---
        // Cada indicador se muestra como un número grande (Valor...) con una
        // línea de detalle abajo (Detalle...). La etiqueta del indicador está
        // fija en la escena. RF-15 eficiencia, RF-29 demanda, RF-28
        // saturación, RF-30 puntuación.

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

        // Resumen corto para la tarjeta: con un solo intervalo, sus horas;
        // con varios, cuántos son. La lista completa no entra en la tarjeta
        // (con muchos intervalos la ensanchaba y corría a las otras): va
        // debajo del gráfico (PeriodosSaturacion).
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

        // RF-28: todos los intervalos en horas del día, seguidos y separados
        // por coma. Vacío si no hubo saturación.
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

        // "de 10:00 a 11:30, de 15:00 a 15:20". Un intervalo [inicio, fin]
        // incluye el minuto fin entero, así que la hora de cierre es la del
        // minuto siguiente.
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

            int minutosSaturados = MinutosSaturados(periodos);

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
