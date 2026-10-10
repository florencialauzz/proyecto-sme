using Sme.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sme.Grid
{
    // Menú contextual genérico de clic derecho: una lista vertical de
    // botones con texto, cada uno con su propia acción. Antes había un
    // MenuContextualPlaza y un MenuContextualCalle casi idénticos — la
    // diferencia entre piezas está en QUÉ opciones arma cada una
    // (PiezaView/PiezaSimpleView deciden eso), no en cómo se arma el menú
    // en sí, así que ese armado de GameObjects se unificó acá.
    public static class MenuContextual
    {
        private const float Ancho = 260f;
        private const float AltoOpcion = 42f;
        private const float Margen = 6f;

        public readonly struct Opcion
        {
            public readonly string Texto;
            public readonly System.Action Accion;

            // Las opciones que borran algo (Eliminar) van en rojo.
            public readonly bool EsDestructiva;

            public Opcion(string texto, System.Action accion, bool esDestructiva = false)
            {
                Texto = texto;
                Accion = accion;
                EsDestructiva = esDestructiva;
            }
        }

        public static void Mostrar(Component pieza, Vector2 posicionPantalla, params Opcion[] opciones)
        {
            Canvas canvasRaiz = pieza.GetComponentInParent<Canvas>().rootCanvas;

            GameObject fondo = new GameObject(
                "FondoMenuContextual", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Canvas), typeof(GraphicRaycaster));
            fondo.transform.SetParent(canvasRaiz.transform, false);
            RectTransform fondoRect = fondo.GetComponent<RectTransform>();
            Tema.Estirar(fondoRect);
            fondo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            fondo.GetComponent<Button>().onClick.AddListener(() => Object.Destroy(fondo));

            // Cada pieza dibuja su Canvas propio con sortingOrder 1
            // (PiezaView.Awake / PiezaSimpleView.Awake) para quedar por
            // encima de las celdas, y la marca roja de RF-20 usa 2
            // (CeldaView.MostrarAdvertencia) — este menú necesita un
            // sortingOrder más alto todavía para no quedar tapado por nada.
            Canvas canvasMenu = fondo.GetComponent<Canvas>();
            canvasMenu.overrideSorting = true;
            canvasMenu.sortingOrder = 10;

            // fondoRect cubre el mismo rectángulo que canvasRaiz, así que un
            // punto de pantalla convertido a coordenadas locales de
            // canvasRaiz sirve directo como anchoredPosition dentro de fondo.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRaiz.transform as RectTransform, posicionPantalla, canvasRaiz.worldCamera, out Vector2 posicionLocal);

            // La tarjeta oscura que contiene las opciones, apiladas hacia
            // abajo desde el punto de clic en el orden en que se pasaron.
            GameObject tarjeta = new GameObject("TarjetaMenu", typeof(RectTransform), typeof(Image), typeof(Shadow));
            tarjeta.transform.SetParent(fondo.transform, false);
            RectTransform tarjetaRect = tarjeta.GetComponent<RectTransform>();
            tarjetaRect.anchorMin = tarjetaRect.anchorMax = new Vector2(0.5f, 0.5f);
            tarjetaRect.pivot = new Vector2(0f, 1f);
            tarjetaRect.sizeDelta = new Vector2(Ancho, AltoOpcion * opciones.Length + Margen * 2);
            tarjetaRect.anchoredPosition = posicionLocal;
            Image imagenTarjeta = tarjeta.GetComponent<Image>();
            imagenTarjeta.sprite = Tema.Redondeado(12);
            imagenTarjeta.type = Image.Type.Sliced;
            imagenTarjeta.color = Tema.Oscuro;
            Shadow sombra = tarjeta.GetComponent<Shadow>();
            sombra.effectColor = new Color(0.05f, 0.07f, 0.2f, 0.25f);
            sombra.effectDistance = new Vector2(0f, -6f);

            for (int i = 0; i < opciones.Length; i++)
            {
                Opcion opcion = opciones[i];

                GameObject boton = new GameObject($"OpcionMenuContextual_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                boton.transform.SetParent(tarjeta.transform, false);
                RectTransform botonRect = boton.GetComponent<RectTransform>();
                botonRect.anchorMin = new Vector2(0f, 1f);
                botonRect.anchorMax = new Vector2(1f, 1f);
                botonRect.pivot = new Vector2(0.5f, 1f);
                botonRect.sizeDelta = new Vector2(-Margen * 2, AltoOpcion);
                botonRect.anchoredPosition = new Vector2(0f, -Margen - AltoOpcion * i);

                TMP_Text texto = Tema.CrearTexto(boton.transform, "Texto", opcion.Texto,
                    Tema.FuenteTextoFuerte, Tema.TamanioEtiqueta, Color.white, TextAlignmentOptions.Left);
                Tema.Estirar(texto.rectTransform, 14f, 0f);

                texto.color = opcion.EsDestructiva ? Tema.Hex("#FF7A90") : Color.white;

                // Fondo blanco transparente: al pasar el puntero se tiñe a un
                // blanco tenue sobre la tarjeta oscura.
                Image imagenBoton = boton.GetComponent<Image>();
                imagenBoton.sprite = Tema.Redondeado(8);
                imagenBoton.type = Image.Type.Sliced;
                imagenBoton.color = Color.white;
                Button componenteBoton = boton.GetComponent<Button>();
                ColorBlock colores = componenteBoton.colors;
                colores.normalColor = new Color(1f, 1f, 1f, 0f);
                colores.highlightedColor = new Color(1f, 1f, 1f, 0.1f);
                colores.pressedColor = new Color(1f, 1f, 1f, 0.18f);
                colores.selectedColor = new Color(1f, 1f, 1f, 0f);
                colores.fadeDuration = 0.08f;
                componenteBoton.colors = colores;

                System.Action accion = opcion.Accion;
                componenteBoton.onClick.AddListener(() =>
                {
                    accion();
                    Object.Destroy(fondo);
                });
            }
        }
    }
}
