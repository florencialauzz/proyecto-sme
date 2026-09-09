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
        [SerializeField] private GameObject prefabCalle;
        [SerializeField] private GameObject prefabEntrada;
        [SerializeField] private GameObject prefabSalida;
        [SerializeField] private GameObject prefabZonaBiciMoto;

        // Registro estático de celdas por posición: lo necesita PiezaView (RF-13)
        // para encontrar la celda vecina de una pieza de 2 celdas, como Plaza
        // (dominio/modelo-clases.md). Tamaño y espaciado también se exponen acá
        // para que PiezaView no hardcodee valores que se configuran en el
        // GridLayoutGroup del Inspector.
        private static readonly Dictionary<(int fila, int columna), CeldaView> celdas = new();

        public static Vector2 TamanioCelda { get; private set; }
        public static Vector2 Espaciado { get; private set; }
        private static int filasGrilla;
        private static int columnasGrilla;

        private void Start()
        {
            GenerarGrilla(ProyectoManager.FilasGrilla, ProyectoManager.ColumnasGrilla);

            // El GridLayoutGroup recién ubica las celdas en su rebuild diferido
            // (antes de dibujar el frame), no apenas se instancian. Sin este
            // forzado, PiezaView.PosicionarSobreCeldas mide anchoredPosition
            // todavía en (0,0) para todas las celdas al reconstruir piezas
            // guardadas, y calcula mal tamaño y posición.
            LayoutRebuilder.ForceRebuildLayoutImmediate(contenedorGrilla);

            CargarPiezasGuardadas();
        }

        private void GenerarGrilla(int filas, int columnas)
        {
            celdas.Clear();
            filasGrilla = filas;
            columnasGrilla = columnas;

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

        // Reconstruye cada pieza guardada en su celda ancla, con la
        // orientación (y accesibilidad, si aplica) guardadas — celdas fuera de
        // rango se ignoran en vez de romper todo, por si la grilla cambió de
        // tamaño.
        private void CargarPiezasGuardadas()
        {
            PiezaDto[] piezas = ProyectoManager.PiezasACargar;
            if (piezas == null) return;

            foreach (PiezaDto pieza in piezas)
            {
                CeldaView ancla = ObtenerCelda(pieza.fila, pieza.columna);
                if (ancla == null) continue;

                CaraAcceso orientacion = (CaraAcceso)Enum.Parse(typeof(CaraAcceso), pieza.caraAcceso);
                TipoPieza tipo = (TipoPieza)Enum.Parse(typeof(TipoPieza), pieza.tipo);

                if (tipo == TipoPieza.PLAZA)
                {
                    GameObject instancia = Instantiate(prefabPlaza, contenedorGrilla);
                    instancia.GetComponent<PiezaView>().ColocarDesdeGuardado(ancla, orientacion, pieza.esAccesible);
                }
                else
                {
                    GameObject instancia = Instantiate(PrefabPara(tipo), contenedorGrilla);
                    instancia.GetComponent<PiezaSimpleView>().ColocarDesdeGuardado(ancla, orientacion);
                }
            }
        }

        private GameObject PrefabPara(TipoPieza tipo)
        {
            return tipo switch
            {
                TipoPieza.CALLE => prefabCalle,
                TipoPieza.ENTRADA => prefabEntrada,
                TipoPieza.SALIDA => prefabSalida,
                TipoPieza.ZONA_BICI_MOTO => prefabZonaBiciMoto,
                _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de pieza sin prefab asignado")
            };
        }

        public static CeldaView ObtenerCelda(int fila, int columna)
        {
            return celdas.TryGetValue((fila, columna), out CeldaView celda) ? celda : null;
        }

        // RF-19: Entrada, Salida y ZonaBicicletasMotos solo son válidas sobre
        // el borde de la grilla.
        public static bool EsCeldaDeBorde(int fila, int columna)
        {
            return fila == 0 || fila == filasGrilla - 1 || columna == 0 || columna == columnasGrilla - 1;
        }

        // RF-21: recorrer toda la grilla para juntar las piezas colocadas.
        public static IEnumerable<CeldaView> ObtenerTodasLasCeldas()
        {
            return celdas.Values;
        }
    }
}
