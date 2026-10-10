using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sme.UI
{
    // Sistema visual del SME: paleta, tipografías y formas. Es la única
    // fuente de los colores y fuentes de la interfaz — tanto las escenas
    // (armadas por Editor/AplicarDiseno.cs) como los elementos que se crean
    // por código (MenuContextual, SelectorPisos, avisos del editor) salen de
    // acá, así todo el sistema mantiene la misma estética.
    //
    // Las fuentes y los sprites redondeados están en Resources/ para que el
    // código que arma UI en tiempo de ejecución los pueda cargar sin tener
    // una referencia asignada en el Inspector.
    public static class Tema
    {
        // --- Paleta ---

        // Fondos y superficies
        public static readonly Color Fondo = Hex("#F3F5FB");
        public static readonly Color FondoEditor = Hex("#E9EDF6");
        public static readonly Color Superficie = Hex("#FFFFFF");
        public static readonly Color SuperficieSuave = Hex("#F6F7FC");
        public static readonly Color Borde = Hex("#E2E6F0");

        // Texto
        public static readonly Color Texto = Hex("#141B34");
        public static readonly Color TextoSecundario = Hex("#5D6883");
        public static readonly Color TextoTenue = Hex("#9AA3B8");
        public static readonly Color TextoSobreColor = Hex("#FFFFFF");

        // Marca: violeta eléctrico, con cian y ámbar como acentos.
        public static readonly Color Primario = Hex("#5B4CFF");
        public static readonly Color PrimarioOscuro = Hex("#4334E6");
        public static readonly Color PrimarioSuave = Hex("#EEEBFF");
        public static readonly Color Acento = Hex("#00C2E0");
        public static readonly Color AcentoSuave = Hex("#E0F8FC");
        public static readonly Color Ambar = Hex("#FFB020");
        public static readonly Color AmbarSuave = Hex("#FFF4DE");

        // Estados
        public static readonly Color Exito = Hex("#12B981");
        public static readonly Color ExitoSuave = Hex("#E2F8EF");
        public static readonly Color Peligro = Hex("#F43F5E");
        public static readonly Color PeligroOscuro = Hex("#D92349");
        public static readonly Color PeligroSuave = Hex("#FFE9ED");

        // Menús contextuales, avisos flotantes y tooltips.
        public static readonly Color Oscuro = Hex("#161C36");
        public static readonly Color Velo = new Color(0.06f, 0.08f, 0.2f, 0.55f);

        // --- Tipografía ---
        // Poppins para títulos, Inter para todo lo demás.

        public static TMP_FontAsset FuenteTitulo => Cargar(ref fuenteTitulo, "Fuentes/Poppins-Bold SDF");
        public static TMP_FontAsset FuenteSubtitulo => Cargar(ref fuenteSubtitulo, "Fuentes/Poppins-SemiBold SDF");
        public static TMP_FontAsset FuenteTexto => Cargar(ref fuenteTexto, "Fuentes/Inter-Regular SDF");
        public static TMP_FontAsset FuenteTextoFuerte => Cargar(ref fuenteTextoFuerte, "Fuentes/Inter-SemiBold SDF");

        public const float TamanioTitulo = 40f;
        public const float TamanioTituloSeccion = 24f;
        public const float TamanioSubtitulo = 20f;
        public const float TamanioTexto = 18f;
        public const float TamanioBoton = 17f;
        public const float TamanioEtiqueta = 15f;
        public const float TamanioChico = 14f;

        // --- Formas ---
        // Rectángulos redondeados blancos (se tiñen con el color de la Image),
        // con y sin borde. El número es el radio en unidades de UI.

        public static Sprite Redondeado(int radio) => Resources.Load<Sprite>($"UI/Redondeado{radio}");
        public static Sprite RedondeadoConBorde(int radio) => Resources.Load<Sprite>($"UI/Borde{radio}");
        public static Sprite Circulo => Resources.Load<Sprite>("UI/Circulo");

        // --- Botones ---

        public enum EstiloBoton
        {
            Primario,     // la acción principal de la pantalla
            Secundario,   // blanco con borde
            Fantasma,     // sin fondo, texto en color primario
            Peligro,      // acciones destructivas confirmadas
            PeligroSuave, // acciones destructivas que piden confirmación después
            Oscuro        // opciones de menús oscuros
        }

        // Aplica fondo, colores de estado y fuente a un botón. La usan tanto
        // las escenas como los botones creados por código.
        public static void AplicarEstiloBoton(Button boton, EstiloBoton estilo, int radio = 12)
        {
            Image fondo = boton.GetComponent<Image>();
            Color colorFondo;
            Color colorTexto;
            Sprite sprite = Redondeado(radio);

            switch (estilo)
            {
                case EstiloBoton.Primario:
                    colorFondo = Primario;
                    colorTexto = TextoSobreColor;
                    break;
                case EstiloBoton.Peligro:
                    colorFondo = Peligro;
                    colorTexto = TextoSobreColor;
                    break;
                case EstiloBoton.PeligroSuave:
                    colorFondo = PeligroSuave;
                    colorTexto = PeligroOscuro;
                    break;
                case EstiloBoton.Fantasma:
                    colorFondo = new Color(1f, 1f, 1f, 0f);
                    colorTexto = Primario;
                    break;
                case EstiloBoton.Oscuro:
                    colorFondo = Oscuro;
                    colorTexto = TextoSobreColor;
                    break;
                default:
                    colorFondo = Superficie;
                    colorTexto = Texto;
                    sprite = RedondeadoConBorde(radio);
                    break;
            }

            if (fondo != null)
            {
                fondo.sprite = sprite;
                fondo.type = Image.Type.Sliced;
                fondo.color = colorFondo;
                fondo.pixelsPerUnitMultiplier = 1f;
            }

            // El tinte multiplica el color del fondo: blanco = sin cambios.
            ColorBlock colores = boton.colors;
            colores.normalColor = Color.white;
            colores.highlightedColor = estilo == EstiloBoton.Fantasma ? new Color(0.93f, 0.92f, 1f, 1f) : new Color(0.9f, 0.9f, 0.95f, 1f);
            colores.pressedColor = new Color(0.8f, 0.8f, 0.88f, 1f);
            colores.selectedColor = Color.white;
            colores.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            colores.colorMultiplier = 1f;
            colores.fadeDuration = 0.08f;
            boton.colors = colores;
            boton.transition = Selectable.Transition.ColorTint;

            // Un fondo transparente (Fantasma) no puede mostrar el hover con
            // el tinte, porque multiplica un alfa 0: se le da un alfa mínimo.
            if (estilo == EstiloBoton.Fantasma && fondo != null)
            {
                fondo.color = new Color(1f, 1f, 1f, 0.01f);
            }

            TMP_Text texto = boton.GetComponentInChildren<TMP_Text>(true);
            if (texto != null)
            {
                texto.font = FuenteTextoFuerte;
                texto.fontSize = TamanioBoton;
                texto.fontStyle = FontStyles.Normal;
                texto.color = colorTexto;
                texto.alignment = TextAlignmentOptions.Center;
                texto.enableAutoSizing = false;
                texto.textWrappingMode = TextWrappingModes.NoWrap;
                texto.overflowMode = TextOverflowModes.Ellipsis;
                texto.raycastTarget = false;
            }
        }

        // --- Texto creado por código ---

        public static TextMeshProUGUI CrearTexto(Transform padre, string nombre, string contenido,
            TMP_FontAsset fuente, float tamanio, Color color, TextAlignmentOptions alineacion)
        {
            GameObject textoGO = new GameObject(nombre, typeof(RectTransform));
            textoGO.transform.SetParent(padre, false);
            TextMeshProUGUI texto = textoGO.AddComponent<TextMeshProUGUI>();
            texto.text = contenido;
            texto.font = fuente;
            texto.fontSize = tamanio;
            texto.color = color;
            texto.alignment = alineacion;
            texto.raycastTarget = false;
            return texto;
        }

        // Estira un RectTransform para que cubra a su padre, con márgenes.
        public static void Estirar(RectTransform rect, float margenHorizontal = 0f, float margenVertical = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margenHorizontal, margenVertical);
            rect.offsetMax = new Vector2(-margenHorizontal, -margenVertical);
        }

        // Color en formato "#RRGGBB" (o "#RRGGBBAA").
        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }

        // "#RRGGBB" para usar dentro de etiquetas <color> de TextMeshPro.
        public static string HexDe(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }

        private static TMP_FontAsset fuenteTitulo;
        private static TMP_FontAsset fuenteSubtitulo;
        private static TMP_FontAsset fuenteTexto;
        private static TMP_FontAsset fuenteTextoFuerte;

        private static TMP_FontAsset Cargar(ref TMP_FontAsset cache, string ruta)
        {
            if (cache == null)
            {
                cache = Resources.Load<TMP_FontAsset>(ruta);
            }
            return cache;
        }
    }
}
