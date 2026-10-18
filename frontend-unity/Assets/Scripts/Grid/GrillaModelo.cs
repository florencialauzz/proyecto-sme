using System.Collections.Generic;

namespace Sme.Grid
{
    public static class GrillaModelo
    {
        private static readonly Dictionary<(int piso, int fila, int columna), CeldaGrilla> celdas = new();

        public static int CantidadPisos { get; private set; }

        public static void Generar(int pisos, int filas, int columnas)
        {
            celdas.Clear();
            CantidadPisos = pisos;
            for (int piso = 0; piso < pisos; piso++)
            {
                for (int fila = 0; fila < filas; fila++)
                {
                    for (int columna = 0; columna < columnas; columna++)
                    {
                        celdas[(piso, fila, columna)] = new CeldaGrilla(piso, fila, columna);
                    }
                }
            }
        }

        public static CeldaGrilla ObtenerCelda(int piso, int fila, int columna)
        {
            return celdas.TryGetValue((piso, fila, columna), out CeldaGrilla celda) ? celda : null;
        }

        public static IEnumerable<CeldaGrilla> TodasLasCeldas()
        {
            return celdas.Values;
        }

        public static bool ExisteOcupadaDeTipo(TipoPieza tipo)
        {
            foreach (CeldaGrilla celda in celdas.Values)
            {
                if (celda.Tipo == tipo) return true;
            }
            return false;
        }

        public static (int deltaFila, int deltaColumna) Delta(CaraAcceso direccion)
        {
            return direccion switch
            {
                CaraAcceso.NORTE => (-1, 0),
                CaraAcceso.SUR => (1, 0),
                CaraAcceso.ESTE => (0, 1),
                CaraAcceso.OESTE => (0, -1),
                _ => (0, 0)
            };
        }

        public static CaraAcceso Opuesta(CaraAcceso direccion)
        {
            return (CaraAcceso)(((int)direccion + 2) % 4);
        }
    }
}
