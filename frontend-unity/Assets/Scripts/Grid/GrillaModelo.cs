using System.Collections.Generic;

namespace Sme.Grid
{
    // Modelo puro de la grilla (sin MonoBehaviour): qué pieza hay en cada
    // celda y hacia dónde apunta. CeldaView lee y escribe acá en vez de que
    // las piezas se consulten entre sí recorriendo GameObjects; el conteo de
    // conexiones (GrafoCirculacion) también consulta este modelo.
    public static class GrillaModelo
    {
        private static readonly Dictionary<(int fila, int columna), CeldaGrilla> celdas = new();

        public static void Generar(int filas, int columnas)
        {
            celdas.Clear();
            for (int fila = 0; fila < filas; fila++)
            {
                for (int columna = 0; columna < columnas; columna++)
                {
                    celdas[(fila, columna)] = new CeldaGrilla(fila, columna);
                }
            }
        }

        public static CeldaGrilla ObtenerCelda(int fila, int columna)
        {
            return celdas.TryGetValue((fila, columna), out CeldaGrilla celda) ? celda : null;
        }

        // RF-19: exactamente una Entrada y una Salida en todo el proyecto —
        // antes de colocar una, hay que saber si ya existe otra. Al mover una
        // pieza ya colocada, su propia celda ya se liberó (CeldaView.Liberar)
        // antes de intentar la nueva, así que no hace falta excluirla acá.
        public static bool ExisteOcupadaDeTipo(TipoPieza tipo)
        {
            foreach (CeldaGrilla celda in celdas.Values)
            {
                if (celda.Tipo == tipo) return true;
            }
            return false;
        }

        // Delta de fila/columna hacia el que apunta cada dirección — única
        // fuente de verdad para no repetir este switch en cada lugar que
        // necesita moverse por la grilla (celda secundaria de la Plaza,
        // conteo de conexiones, aviso a vecinas).
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
