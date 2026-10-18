using Sme.Grid;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sme.UI
{
    public class SelectorPisos : MonoBehaviour
    {
        private const float AnchoBoton = 104f;
        private const float AltoBoton = 36f;

        [SerializeField] private RectTransform contenedorBotones;
        [SerializeField] private Button botonEliminarPiso;

        private void Awake()
        {
            GrillaGenerador.AlCambiarPisos += Rearmar;
            ValidadorDiseno.AlValidar += AlValidar;
            botonEliminarPiso.onClick.AddListener(ConfirmarEliminarPiso);
        }

        private void OnDestroy()
        {
            GrillaGenerador.AlCambiarPisos -= Rearmar;
            ValidadorDiseno.AlValidar -= AlValidar;
        }

        private void AlValidar(ValidadorDiseno.Resultado resultado)
        {
            Rearmar();
        }

        private void Rearmar()
        {
            foreach (Transform hijo in contenedorBotones)
            {
                hijo.gameObject.SetActive(false);
                Destroy(hijo.gameObject);
            }

            for (int piso = 0; piso < GrillaGenerador.CantidadPisos; piso++)
            {
                CrearBotonPiso(piso);
            }

            contenedorBotones.gameObject.SetActive(GrillaGenerador.CantidadPisos > 1);

            bool puedeEliminar = !GrillaGenerador.SoloLectura && GrillaGenerador.PisoActual != 0;
            botonEliminarPiso.gameObject.SetActive(puedeEliminar);
        }

        private void CrearBotonPiso(int piso)
        {
            GameObject boton = new GameObject($"BotonPiso_{piso}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            boton.transform.SetParent(contenedorBotones, false);

            LayoutElement layout = boton.GetComponent<LayoutElement>();
            layout.preferredWidth = AnchoBoton;
            layout.preferredHeight = AltoBoton;

            GameObject textoGO = new GameObject("Texto", typeof(RectTransform));
            textoGO.transform.SetParent(boton.transform, false);
            textoGO.AddComponent<TextMeshProUGUI>();
            Tema.Estirar(textoGO.GetComponent<RectTransform>());

            bool esPisoActual = piso == GrillaGenerador.PisoActual;
            Button componenteBoton = boton.GetComponent<Button>();
            Tema.AplicarEstiloBoton(componenteBoton, esPisoActual ? Tema.EstiloBoton.Primario : Tema.EstiloBoton.Fantasma, 8);
            componenteBoton.interactable = !esPisoActual;
            ColorBlock colores = componenteBoton.colors;
            colores.disabledColor = Color.white;
            componenteBoton.colors = colores;

            TMP_Text texto = textoGO.GetComponent<TMP_Text>();
            string nombre = piso == 0 ? "PB" : $"Piso {piso}";
            texto.color = esPisoActual ? Tema.TextoSobreColor : Tema.TextoSecundario;

            ValidadorDiseno.Resultado validacion = ValidadorDiseno.UltimoResultado;
            bool tieneAdvertencias = validacion != null && validacion.PisosConAdvertencias.Contains(piso);
            string colorPunto = Tema.HexDe(esPisoActual ? Tema.Hex("#FFC2CD") : Tema.Peligro);
            texto.text = tieneAdvertencias ? $"<color={colorPunto}>●</color> {nombre}" : nombre;

            componenteBoton.onClick.AddListener(() => GrillaGenerador.MostrarPiso(piso));
        }

        private void ConfirmarEliminarPiso()
        {
            int piso = GrillaGenerador.PisoActual;
            Vector2 posicionPantalla = RectTransformUtility.WorldToScreenPoint(null, botonEliminarPiso.transform.position);

            MenuContextual.Mostrar(botonEliminarPiso, posicionPantalla,
                new MenuContextual.Opcion($"Eliminar {GrillaGenerador.NombrePiso(piso)} y sus piezas", () => GrillaGenerador.EliminarPiso(piso), esDestructiva: true),
                new MenuContextual.Opcion("Cancelar", () => { }));
        }
    }
}
