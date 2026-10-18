using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sme.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class GraficoCurva : MaskableGraphic
    {
        private const int MinutosPorHora = 60;

        private const int MaximoPlazasConLineaPorPlaza = 15;
        private const int MaximoLineasHorizontales = 10;

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

        private readonly List<float> lineasVerticales = new List<float>();
        private readonly List<float> lineasHorizontales = new List<float>();

        private readonly List<GameObject> etiquetas = new List<GameObject>();

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

        private void ArmarEjeHorizontal(string horaInicio, string horaFin)
        {
            int ultimoMinuto = valores.Length - 1;
            float ancho = rectTransform.rect.width;

            CrearEtiquetaHora(horaInicio, 0f);
            CrearEtiquetaHora(horaFin, 1f);

            int minutoInicioDelDia = MinutoDelDia(horaInicio);
            int primeraHoraEnPunto = (MinutosPorHora - minutoInicioDelDia % MinutosPorHora) % MinutosPorHora;

            for (int minuto = primeraHoraEnPunto; minuto < ultimoMinuto; minuto += MinutosPorHora)
            {
                if (minuto == 0) continue;

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

        private void ArmarEjeVertical()
        {
            int paso = PasoEjeVertical(valorMaximo);

            for (int plazas = 0; plazas <= valorMaximo; plazas += paso)
            {
                AgregarLineaHorizontal(plazas);
            }

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

        private void CrearEtiquetaHora(string texto, float proporcionX)
        {
            TMP_Text etiqueta = CrearEtiqueta(texto, TextAlignmentOptions.Top);
            RectTransform rect = etiqueta.rectTransform;
            rect.anchorMin = new Vector2(proporcionX, 0f);
            rect.anchorMax = new Vector2(proporcionX, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -SeparacionEtiquetaDelEje);
        }

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
