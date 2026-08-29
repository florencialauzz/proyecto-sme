using Sme.Managers;
using UnityEngine;

namespace Sme.Grid
{
    // RF-12: Generar grilla. Al cargar la escena Editor (después de crear un
    // proyecto), instancia la grilla vacía de filasGrilla x columnasGrilla
    // usando los datos guardados en ProyectoManager. El posicionamiento lo
    // resuelve el GridLayoutGroup del contenedor, asignado en el Inspector —
    // este script solo instancia las celdas en orden de fila y columna.
    public class GrillaGenerador : MonoBehaviour
    {
        [SerializeField] private RectTransform contenedorGrilla;
        [SerializeField] private GameObject prefabCelda;

        private void Start()
        {
            GenerarGrilla(ProyectoManager.FilasGrilla, ProyectoManager.ColumnasGrilla);
        }

        private void GenerarGrilla(int filas, int columnas)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                for (int columna = 0; columna < columnas; columna++)
                {
                    GameObject celda = Instantiate(prefabCelda, contenedorGrilla);
                    celda.name = $"Celda_{fila}_{columna}";

                    celda.GetComponent<CeldaView>().Inicializar(fila, columna);
                }
            }
        }
    }
}
