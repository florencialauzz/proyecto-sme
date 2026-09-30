using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sme.UI
{
    // RF-27: dibuja la curva de ocupación dentro de su RectTransform — el
    // minuto 0 en el borde izquierdo, el último en el derecho, 0 plazas abajo
    // y el total de plazas arriba — con la grilla de fondo y las etiquetas
    // de los ejes, para que se pueda leer a qué hora hubo cuántas plazas
    // ocupadas.
    //
    // Unity UI no trae gráficos, así que las líneas se arman a mano: un
    // rectángulo fino por cada par de puntos consecutivos, generado en
    // OnPopulateMesh. Es el mismo mecanismo con el que se dibuja una Image,
    // así que funciona en WebGL sin librerías externas. El color de la curva
    // es el Color del componente (Inspector); la grilla y los ejes tienen el
    // suyo.
    //
    // Las etiquetas (horas abajo, cantidad de plazas a la izquierda) son
    // textos hijos de este objeto, creados al mostrar la curva. Quedan por
    // fuera del rectángulo, así que el gráfico necesita margen alrededor.
    [RequireComponent(typeof(CanvasRenderer))]
    public class GraficoCurva : MaskableGraphic
    {
        private const int MinutosPorHora = 60;

        // Con hasta esta cantidad de plazas hay una línea por plaza; con más,
        // las líneas se espacian para no pasar de MaximoLineasHorizontales.
        private const int MaximoPlazasConLineaPorPlaza = 15;
        private const int MaximoLineasHorizontales = 10;

        // Una etiqueta de hora más cerca que esto de la del inicio o la del
        // fin se pisaría con ella: se dibuja la línea pero no el texto.
        private const float DistanciaMinimaEntreEtiquetas = 40f;

        private const float SeparacionEtiquetaDelEje = 6f;

        [SerializeField] private float grosorLinea = 3f;
        [SerializeField] private float grosorEjes = 2f;
        [SerializeField] private float grosorGrilla = 1f;
        [SerializeField] private Color colorEjes = new Color(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color colorGrilla = new Color(0.5f, 0.5f, 0.5f, 0.35f);
        [SerializeField] private Color colorEtiquetas = new Color(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private float tamanoEtiquetas = 14f;

        private int[] valores;
        private int valorMaximo;

        // Posiciones de la grilla, como proporción del ancho y del alto
        // (0 = borde izquierdo / de abajo, 1 = derecho / de arriba).
        private readonly List<float> lineasVerticales = new List<float>();
        private readonly List<float> lineasHorizontales = new List<float>();

        private readonly List<GameObject> etiquetas = new List<GameObject>();

        // valorMaximo es lo que queda en el borde de arriba (el total de
        // plazas). Si algún valor lo supera, se dibuja pegado al borde.
        // horaInicio y horaFin vienen en "HH:mm", como en la configuración.
        public void MostrarCurva(int[] valoresPorMinuto, int valorMaximo, string horaInicio, string horaFin)
        {
            valores = valoresPorMinuto;
            this.valorMaximo = Mathf.Max(1, valorMaximo);

            BorrarEtiquetas();
            lineasVerticales.Clear();
            lineasHorizontales.Clear();

            if (valores != null && valores.Length >= 2)
            {
                ArmarEjeHorizontal(horaInicio, horaFin);
                ArmarEjeVertical();
            }

            SetVerticesDirty();
        }

        // --- Eje horizontal: una línea por cada hora en punto ---

        private void ArmarEjeHorizontal(string horaInicio, string horaFin)
        {
            int ultimoMinuto = valores.Length - 1;
            float ancho = rectTransform.rect.width;

            CrearEtiquetaHora(horaInicio, 0f);
            // El último punto es el minuto anterior a la hora de fin (con
            // 08:00 a 20:00, el 19:59), pero en el borde derecho se lee mejor
            // la hora de fin tal como se configuró.
            CrearEtiquetaHora(horaFin, 1f);

            int minutoInicioDelDia = MinutoDelDia(horaInicio);
            int primeraHoraEnPunto = (MinutosPorHora - minutoInicioDelDia % MinutosPorHora) % MinutosPorHora;

            for (int minuto = primeraHoraEnPunto; minuto < ultimoMinuto; minuto += MinutosPorHora)
            {
                if (minuto == 0) continue; // ya es la etiqueta del inicio

                float proporcion = (float)minuto / ultimoMinuto;
                lineasVerticales.Add(proporcion);

                bool lejosDelInicio = proporcion * ancho >= DistanciaMinimaEntreEtiquetas;
                bool lejosDelFin = (1f - proporcion) * ancho >= DistanciaMinimaEntreEtiquetas;
                if (lejosDelInicio && lejosDelFin)
                {
                    int horaDelDia = (minutoInicioDelDia + minuto) / MinutosPorHora % 24;
                    CrearEtiquetaHora($"{horaDelDia:00}:00", proporcion);
                }
            }
        }

        private static int MinutoDelDia(string hora)
        {
            string[] partes = hora.Split(':');
            int horas = int.Parse(partes[0]);
            int minutos = int.Parse(partes[1]);
            return horas * MinutosPorHora + minutos;
        }

        // --- Eje vertical: una línea por plaza, o cada tantas si son muchas ---

        private void ArmarEjeVertical()
        {
            int paso = PasoEjeVertical(valorMaximo);

            for (int plazas = 0; plazas <= valorMaximo; plazas += paso)
            {
                AgregarLineaHorizontal(plazas);
            }

            // El total siempre se marca, aunque no caiga en el paso: es el
            // techo del gráfico y se quiere ver cuándo se llegó a él.
            if (valorMaximo % paso != 0)
            {
                AgregarLineaHorizontal(valorMaximo);
            }
        }

        private void AgregarLineaHorizontal(int plazas)
        {
            float proporcion = (float)plazas / valorMaximo;
            lineasHorizontales.Add(proporcion);
            CrearEtiquetaPlazas(plazas.ToString(), proporcion);
        }

        // 1 hasta MaximoPlazasConLineaPorPlaza plazas; después el primer paso
        // "redondo" que deja como mucho MaximoLineasHorizontales líneas.
        private static int PasoEjeVertical(int totalPlazas)
        {
            if (totalPlazas <= MaximoPlazasConLineaPorPlaza) return 1;

            int[] pasosRedondos = { 2, 5, 10, 20, 25, 50, 100, 200, 500 };
            foreach (int paso in pasosRedondos)
            {
                if (totalPlazas / paso <= MaximoLineasHorizontales) return paso;
            }
            return 1000;
        }

        // --- Etiquetas ---

        // Debajo del eje, centrada sobre la línea de esa hora.
        private void CrearEtiquetaHora(string texto, float proporcionX)
        {
            TMP_Text etiqueta = CrearEtiqueta(texto, TextAlignmentOptions.Top);
            RectTransform rect = etiqueta.rectTransform;
            rect.anchorMin = new Vector2(proporcionX, 0f);
            rect.anchorMax = new Vector2(proporcionX, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -SeparacionEtiquetaDelEje);
        }

        // A la izquierda del eje, a la altura de la línea de esa cantidad.
        private void CrearEtiquetaPlazas(string texto, float proporcionY)
        {
            TMP_Text etiqueta = CrearEtiqueta(texto, TextAlignmentOptions.Right);
            RectTransform rect = etiqueta.rectTransform;
            rect.anchorMin = new Vector2(0f, proporcionY);
            rect.anchorMax = new Vector2(0f, proporcionY);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-SeparacionEtiquetaDelEje, 0f);
        }

        private TMP_Text CrearEtiqueta(string texto, TextAlignmentOptions alineacion)
        {
            GameObject etiquetaGO = new GameObject("Etiqueta", typeof(RectTransform));
            etiquetaGO.transform.SetParent(transform, false);
            etiquetaGO.GetComponent<RectTransform>().sizeDelta = new Vector2(60f, 24f);

            TMP_Text etiqueta = etiquetaGO.AddComponent<TextMeshProUGUI>();
            etiqueta.text = texto;
            etiqueta.alignment = alineacion;
            etiqueta.color = colorEtiquetas;
            etiqueta.fontSize = tamanoEtiquetas;
            etiqueta.raycastTarget = false;

            etiquetas.Add(etiquetaGO);
            return etiqueta;
        }

        private void BorrarEtiquetas()
        {
            foreach (GameObject etiqueta in etiquetas)
            {
                Destroy(etiqueta);
            }
            etiquetas.Clear();
        }

        // --- Dibujo ---

        // Orden de dibujo: grilla, ejes y por último la curva, para que quede
        // por encima de todo.
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (valores == null || valores.Length < 2) return;

            Rect area = GetPixelAdjustedRect();

            foreach (float proporcion in lineasVerticales)
            {
                float x = area.xMin + area.width * proporcion;
                AgregarSegmento(vh, new Vector2(x, area.yMin), new Vector2(x, area.yMax), grosorGrilla, colorGrilla);
            }
            foreach (float proporcion in lineasHorizontales)
            {
                float y = area.yMin + area.height * proporcion;
                AgregarSegmento(vh, new Vector2(area.xMin, y), new Vector2(area.xMax, y), grosorGrilla, colorGrilla);
            }

            Vector2 origen = new Vector2(area.xMin, area.yMin);
            AgregarSegmento(vh, origen, new Vector2(area.xMax, area.yMin), grosorEjes, colorEjes);
            AgregarSegmento(vh, origen, new Vector2(area.xMin, area.yMax), grosorEjes, colorEjes);

            Vector2 anterior = PosicionDelPunto(0, area);
            for (int i = 1; i < valores.Length; i++)
            {
                Vector2 actual = PosicionDelPunto(i, area);
                AgregarSegmento(vh, anterior, actual, grosorLinea, color);
                anterior = actual;
            }
        }

        private Vector2 PosicionDelPunto(int indice, Rect area)
        {
            float x = area.xMin + area.width * indice / (valores.Length - 1);
            float proporcion = Mathf.Clamp01((float)valores[indice] / valorMaximo);
            float y = area.yMin + area.height * proporcion;
            return new Vector2(x, y);
        }

        // Un rectángulo del grosor pedido centrado sobre el segmento a→b: los
        // cuatro vértices se corren medio grosor hacia cada lado, en la
        // dirección perpendicular al segmento.
        private void AgregarSegmento(VertexHelper vh, Vector2 a, Vector2 b, float grosor, Color colorSegmento)
        {
            Vector2 direccion = (b - a).normalized;
            Vector2 perpendicular = new Vector2(-direccion.y, direccion.x) * (grosor / 2f);

            int primerVertice = vh.currentVertCount;
            vh.AddVert(CrearVertice(a - perpendicular, colorSegmento));
            vh.AddVert(CrearVertice(a + perpendicular, colorSegmento));
            vh.AddVert(CrearVertice(b + perpendicular, colorSegmento));
            vh.AddVert(CrearVertice(b - perpendicular, colorSegmento));

            vh.AddTriangle(primerVertice, primerVertice + 1, primerVertice + 2);
            vh.AddTriangle(primerVertice + 2, primerVertice + 3, primerVertice);
        }

        private static UIVertex CrearVertice(Vector2 posicion, Color colorVertice)
        {
            UIVertex vertice = UIVertex.simpleVert;
            vertice.position = posicion;
            vertice.color = colorVertice;
            return vertice;
        }
    }
}
