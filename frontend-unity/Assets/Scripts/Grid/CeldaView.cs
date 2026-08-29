using UnityEngine;

namespace Sme.Grid
{
    // RF-12: una celda de la grilla. Guarda su posición lógica (fila, columna)
    // para que RF-13 (colocar/quitar plaza) sepa sobre qué celda se soltó una pieza.
    public class CeldaView : MonoBehaviour
    {
        public int Fila { get; private set; }
        public int Columna { get; private set; }

        public void Inicializar(int fila, int columna)
        {
            Fila = fila;
            Columna = columna;
        }
    }
}
