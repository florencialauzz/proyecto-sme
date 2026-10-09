using Sme.Grid;
using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    // Animación (animacion/reglas.md): después de simular, la escena Editor
    // pasa a mostrar la reproducción de la simulación sobre la misma grilla
    // que se acaba de guardar y simular, en vez de ir directo a Resultados.
    // Es un modo dentro del Editor y no una escena aparte porque la grilla ya
    // está cargada y dibujada por GrillaGenerador (animacion/plan.md,
    // decisiones de la etapa 0).
    //
    // Lleva el reloj de la reproducción (play, pausa y velocidad) y los
    // contadores. Todo lo que se muestra sale de los datos del backend
    // (eventos, curvaOcupacion, periodosSaturacion), nunca de contar autos en
    // pantalla.
    //
    // No hay vuelta al modo edición: "Ver resultados" lleva a Resultados, y
    // desde ahí Volver carga de nuevo la escena Editor, que arranca editable.
    public class ModoReproduccion : MonoBehaviour
    {
        // animacion/reglas.md, "Qué se ve en Unity".
        private const string TextoLeyenda =
            "La circulación es ilustrativa; los indicadores no contemplan tiempos de recorrido ni congestión.";

        // A x1, un minuto simulado dura un segundo real.
        private const float MinutosSimuladosPorSegundoAx1 = 1f;

        // Lo que se oculta mientras se reproduce: catálogo, botones del
        // editor y advertencias. Se asignan en el Inspector. "Eliminar piso"
        // no va acá: lo maneja SelectorPisos con GrillaGenerador.SoloLectura.
        [SerializeField] private GameObject[] objetosDeEdicion;

        // Los controles y contadores de la reproducción. Arranca oculto.
        [SerializeField] private GameObject panelReproduccion;
        [SerializeField] private Button botonVerResultados;
        [SerializeField] private UnityEvent alIrAResultados;

        [SerializeField] private AutosReproduccion autos;

        // Reproductor
        [SerializeField] private Button botonPlayPausa;
        [SerializeField] private TMP_Text textoPlayPausa;
        [SerializeField] private Button botonVelocidadX1;
        [SerializeField] private Button botonVelocidadX5;
        [SerializeField] private Button botonVelocidadX20;

        // Contadores, aviso y leyenda
        [SerializeField] private TMP_Text textoHora;
        [SerializeField] private TMP_Text textoOcupadas;
        [SerializeField] private TMP_Text textoRechazados;
        [SerializeField] private GameObject avisoSaturado;
        [SerializeField] private TMP_Text textoLeyenda;

        private EjecutarSimulacionResponse indicadores;
        private ReproduccionDto reproduccion;

        // Un minuto por punto de curvaOcupacion (de 0 a duración − 1).
        private int duracionMinutos;
        private int minutoInicioDelDia;

        // El reloj avanza con decimales; lo que se muestra es el minuto
        // entero. ultimoMinutoMostrado evita rearmar todo en cada frame.
        private float minutoActual;
        private int ultimoMinutoMostrado;
        private bool reproduciendo;
        private int velocidad;

        private void Awake()
        {
            panelReproduccion.SetActive(false);
            botonVerResultados.onClick.AddListener(VerResultados);
            botonPlayPausa.onClick.AddListener(AlternarPlayPausa);
            botonVelocidadX1.onClick.AddListener(() => CambiarVelocidad(1));
            botonVelocidadX5.onClick.AddListener(() => CambiarVelocidad(5));
            botonVelocidadX20.onClick.AddListener(() => CambiarVelocidad(20));
        }

        // La llama EditorScreen cuando /ejecutar respondió bien. Arranca en
        // pausa en el minuto 0, con la grilla vacía.
        public void Entrar(EjecutarSimulacionResponse indicadoresSimulacion, ReproduccionDto reproduccionSimulacion)
        {
            indicadores = indicadoresSimulacion;
            reproduccion = reproduccionSimulacion;
            duracionMinutos = indicadores.curvaOcupacion.Length;
            minutoInicioDelDia = FormatoIndicadores.MinutoDelDia(ProyectoManager.HoraInicioSimulacion);

            foreach (GameObject objeto in objetosDeEdicion)
            {
                objeto.SetActive(false);
            }

            GrillaGenerador.ActivarSoloLectura();
            autos.Preparar(reproduccion);

            panelReproduccion.SetActive(true);
            textoLeyenda.text = TextoLeyenda;

            minutoActual = 0f;
            reproduciendo = false;
            CambiarVelocidad(1);
            ActualizarBotonPlayPausa();
            MostrarMinuto(0);
        }

        private void Update()
        {
            if (!reproduciendo) return;

            minutoActual += Time.deltaTime * MinutosSimuladosPorSegundoAx1 * velocidad;

            // Al llegar al final se queda mostrando el último minuto. No se
            // vuelve a reproducir: saltar a cualquier hora está fuera de
            // alcance en esta etapa (animacion/reglas.md).
            if (minutoActual >= duracionMinutos)
            {
                minutoActual = duracionMinutos - 1;
                reproduciendo = false;
                ActualizarBotonPlayPausa();
            }

            int minuto = (int)minutoActual;
            if (minuto != ultimoMinutoMostrado)
            {
                MostrarMinuto(minuto);
            }

            // Los autos en camino se mueven en tiempo real, a su propia
            // velocidad (AutosReproduccion); en pausa no se llama y se frenan.
            autos.Avanzar(Time.deltaTime);
        }

        // --- Reproductor ---

        private void AlternarPlayPausa()
        {
            reproduciendo = !reproduciendo;
            ActualizarBotonPlayPausa();
        }

        private bool LlegoAlFinal()
        {
            return ultimoMinutoMostrado >= duracionMinutos - 1;
        }

        private void ActualizarBotonPlayPausa()
        {
            if (LlegoAlFinal())
            {
                textoPlayPausa.text = "Fin";
                botonPlayPausa.interactable = false;
                return;
            }

            textoPlayPausa.text = reproduciendo ? "Pausa" : "Reproducir";
            botonPlayPausa.interactable = true;
        }

        // El botón de la velocidad elegida queda deshabilitado, igual que el
        // del piso que se está mirando en SelectorPisos: marca cuál es.
        //
        // Los autos en camino se vuelven a evaluar con la nueva velocidad
        // (AutosReproduccion.CambiarVelocidad). Al entrar al modo no hay
        // velocidad anterior ni autos en camino.
        private void CambiarVelocidad(int nuevaVelocidad)
        {
            int velocidadAnterior = velocidad;
            velocidad = nuevaVelocidad;

            if (velocidadAnterior > 0 && velocidadAnterior != velocidad)
            {
                autos.CambiarVelocidad(MinutosSimuladosPorSegundoAx1 * velocidadAnterior,
                    MinutosSimuladosPorSegundoAx1 * velocidad);
            }

            botonVelocidadX1.interactable = velocidad != 1;
            botonVelocidadX5.interactable = velocidad != 5;
            botonVelocidadX20.interactable = velocidad != 20;
        }

        // --- Lo que se muestra en cada minuto ---

        private void MostrarMinuto(int minuto)
        {
            ultimoMinutoMostrado = minuto;

            textoHora.text = FormatoIndicadores.HoraDelDia(minutoInicioDelDia + minuto);

            // curvaOcupacion trae un punto por minuto, en orden desde el 0.
            int ocupadas = indicadores.curvaOcupacion[minuto].cantidadOcupadas;
            textoOcupadas.text = $"Ocupadas: {ocupadas} / {reproduccion.plazas.Length}";

            textoRechazados.text = $"Rechazados: {RechazadosHasta(minuto)}";
            avisoSaturado.SetActive(EstaSaturado(minuto));

            // Verificación de animacion/plan.md (etapa 5): los autos con plaza
            // (estacionados o yendo hacia ella, que ya la tienen ocupada)
            // tienen que coincidir con la curva del backend. Si no coinciden
            // hay un error en los eventos o en cómo se leen.
            float minutosSimuladosPorSegundo = MinutosSimuladosPorSegundoAx1 * velocidad;
            int autosConPlaza = autos.MostrarMinuto(minuto, minutosSimuladosPorSegundo);
            if (autosConPlaza != ocupadas)
            {
                Debug.LogWarning($"Reproducción: en el minuto {minuto} hay {autosConPlaza} autos con plaza " +
                                 $"pero curvaOcupacion dice {ocupadas}.");
            }

            if (LlegoAlFinal())
            {
                ActualizarBotonPlayPausa();
            }
        }

        // El contador sube cuando el rechazado entra, no cuando sale
        // (animacion/reglas.md).
        private int RechazadosHasta(int minuto)
        {
            int rechazados = 0;
            foreach (EventoVehiculoDto evento in reproduccion.eventos)
            {
                if (evento.indicePlaza < 0 && evento.minutoLlegada <= minuto)
                {
                    rechazados++;
                }
            }
            return rechazados;
        }

        // Los tramos de periodosSaturacion incluyen inicio y fin.
        private bool EstaSaturado(int minuto)
        {
            foreach (PeriodoSaturacionDto periodo in indicadores.periodosSaturacion)
            {
                if (periodo.inicio <= minuto && minuto <= periodo.fin)
                {
                    return true;
                }
            }
            return false;
        }

        // La pantalla de Resultados no cambia (animacion/reglas.md).
        private void VerResultados()
        {
            alIrAResultados?.Invoke();
        }
    }
}
