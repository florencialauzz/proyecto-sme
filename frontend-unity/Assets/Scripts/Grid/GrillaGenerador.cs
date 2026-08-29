using System;
using System.Collections.Generic;
using Sme.Managers;
using Sme.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Sme.Grid
{
    // RF-12: Generar grilla. Al cargar la escena Editor (después de crear un
    // proyecto), instancia la grilla vacía de filasGrilla x columnasGrilla
    // usando los datos guardados en ProyectoManager. El posicionamiento lo
    // resuelve el GridLayoutGroup del contenedor, asignado en el Inspector —
    // este script solo instancia las celdas en orden de fila y columna.
    //
    // Si el proyecto se está reabriendo (no es uno recién creado), después
    // reconstruye las piezas guardadas (ProyectoManager.PiezasACargar).
    public class GrillaGenerador : MonoBehaviour
    {
        [SerializeField] private RectTransform contenedorGrilla;
        [SerializeField] private GameObject prefabCelda;
        [SerializeField] private GameObject prefabPlaza;

        // Registro estático de celdas por posición: lo necesita PiezaView (RF-13)
        // para encontrar la celda vecina de una pieza de 2 celdas, como Plaza
        // (dominio/modelo-clases.md). Tamaño y espaciado también se exponen acá
        // para que PiezaView no hardcodee valores que se configuran en el
        // GridLayoutGroup del Inspector.
        private static readonly Dictionary<(int fila, int columna), CeldaView> celdas = new();

        public static Vector2 TamanioCelda { get; private set; }
        public static Vector2 Espaciado { get; private set; }

        private void Start()
        {
            GenerarGrilla(ProyectoManager.FilasGrilla, ProyectoManager.ColumnasGrilla);
            CargarPiezasGuardadas();
        }

        private void GenerarGrilla(int filas, int columnas)
        {
            celdas.Clear();

            // Fija la cantidad de columnas para que el GridLayoutGroup no dependa
            // del ancho del contenedor para decidir dónde wrappear la fila.
            GridLayoutGroup layout = contenedorGrilla.GetComponent<GridLayoutGroup>();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = columnas;
            TamanioCelda = layout.cellSize;
            Espaciado = layout.spacing;

            for (int fila = 0; fila < filas; fila++)
            {
                for (int columna = 0; columna < columnas; columna++)
                {
                    GameObject celda = Instantiate(prefabCelda, contenedorGrilla);
                    celda.name = $"Celda_{fila}_{columna}";

                    CeldaView celdaView = celda.GetComponent<CeldaView>();
                    celdaView.Inicializar(fila, columna);
                    celdas[(fila, columna)] = celdaView;
                }
            }
        }

        // Solo pone piezas Plaza (único tipo que existe en Iteración 1) en su
        // celda ancla, con la orientación guardada — celdas fuera de rango se
        // ignoran en vez de romper todo, por si la grilla cambió de tamaño.
        private void CargarPiezasGuardadas()
        {
            PiezaDto[] piezas = ProyectoManager.PiezasACargar;
            if (piezas == null) return;

            foreach (PiezaDto pieza in piezas)
            {
                CeldaView ancla = ObtenerCelda(pieza.fila, pieza.columna);
                if (ancla == null) continue;

                CaraAcceso orientacion = (CaraAcceso)Enum.Parse(typeof(CaraAcceso), pieza.caraAcceso);
                GameObject instancia = Instantiate(prefabPlaza, contenedorGrilla);
                instancia.GetComponent<PiezaView>().ColocarDesdeGuardado(ancla, orientacion);
            }
        }

        public static CeldaView ObtenerCelda(int fila, int columna)
        {
            return celdas.TryGetValue((fila, columna), out CeldaView celda) ? celda : null;
        }

        // RF-21: recorrer toda la grilla para juntar las piezas colocadas.
        public static IEnumerable<CeldaView> ObtenerTodasLasCeldas()
        {
            return celdas.Values;
        }
    }
}
