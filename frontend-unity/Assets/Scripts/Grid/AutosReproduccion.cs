using System.Collections.Generic;
using Sme.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Sme.Grid
{
    // Animación (animacion/reglas.md): dibuja los autos sobre la grilla en
    // solo lectura. Qué plaza ocupa cada uno, hasta cuándo y por dónde
    // circula sale de los datos del backend (eventos y caminos); acá no se
    // decide nada.
    //
    // Dos clases de auto:
    // - Estacionado: uno por plaza, creado una sola vez y prendido o apagado
    //   según el minuto. Es hijo de la vista de su plaza (PiezaView), así
    //   hereda su tamaño y su rotación y se oculta solo cuando se mira otro
    //   piso. "Plaza ocupada = plaza con auto encima": no hay un color
    //   aparte para ocupada.
    // - En camino: uno por recorrido, creado al empezar y destruido al
    //   terminar. Se mueve sobre una capa propia de cada piso, encima de las
    //   piezas, y pasa de una capa a otra cuando el camino cambia de piso.
    //   Hay tres recorridos: hacia la plaza, hacia la salida y el del
    //   rechazado, que va en rojo y no ocupa nada.
    //
    // Mientras un auto va hacia su plaza, la plaza se marca como "reservada"
    // y el auto estacionado todavía no se muestra. Al irse, el auto sale de
    // la plaza marcha atrás hasta la boca y de ahí va a la Salida; la plaza
    // ya cuenta como libre (así lo registra curvaOcupacion).
    public class AutosReproduccion : MonoBehaviour
    {
        // Cuánto se separa el auto del borde de la plaza, como fracción de
        // su ancho y de su largo, para que se sigan viendo las líneas.
        private const float MargenAncho = 0.15f;
        private const float MargenLargo = 0.08f;

        // Velocidad de los autos en pantalla, fija en tramos (de una celda a
        // la siguiente) por segundo real, sin importar la velocidad de
        // reproducción (animacion/plan.md, decisión de la etapa 0). Se ajusta
        // probando.
        private const float TramosPorSegundo = 4f;

        // Un recorrido de salida o de rechazado se muestra solo si dura como
        // mucho esto en minutos simulados; si no, no se dibuja. Así a
        // velocidad alta no se juntan decenas de autos circulando: se
        // saltea en vez de acumular atraso (animacion/reglas.md).
        //
        // A x1 tiene que entrar cualquier recorrido: 30 minutos = 30
        // segundos = 120 tramos. El del rechazado es el más largo del diseño
        // (hasta la plaza más lejana y de ahí a la Salida), y con 15 minutos
        // (60 tramos) en una grilla llena no se llegaba a dibujar nunca. A
        // x5 entran 24 tramos; a x20, 6.
        private const float MaximoMinutosRecorrido = 30f;

        // Tono suave sobre la plaza mientras su auto viene en camino.
        private static readonly Color ColorReservada = new Color(1f, 0.8f, 0.25f, 0.35f);

        private static readonly Color ColorRechazado = new Color(1f, 0.25f, 0.25f, 1f);

        [SerializeField] private Sprite spriteAuto;

        private ReproduccionDto reproduccion;

        // Mismo índice que ReproduccionDto.plazas (el indicePlaza de cada
        // evento).
        private PiezaView[] vistaPorPlaza;
        private GameObject[] autoEstacionadoPorPlaza;
        private GameObject[] marcaReservadaPorPlaza;

        // Una por piso: GrillaGenerador.ContenedorDePiso.
        private RectTransform[] capaPorPiso;

        private readonly List<AutoEnCamino> autosEnCamino = new();

        // Los eventos vienen en orden de llegada: este es el primero que
        // todavía no llegó. La reproducción solo avanza, así que no hace
        // falta volver a mirar los anteriores.
        private int siguienteEvento;

        // Por evento: cuántos vehículos llegan en su mismo minuto y en qué
        // lugar llega él entre ellos (0, 1, ...). Ver EsperaDentroDelMinuto.
        private int[] llegadasEnSuMinuto;
        private int[] ordenEnSuMinuto;

        // El último minuto entero que mostró ModoReproduccion. Un recorrido
        // puede terminar entre dos cambios de minuto (Avanzar), y ahí hace
        // falta saber en qué minuto se está.
        private int minutoActual;

        private enum TipoRecorrido
        {
            HaciaLaPlaza,
            HaciaLaSalida,
            Rechazado
        }

        // IndicePlaza de un recorrido de rechazado: no tiene plaza.
        private const int SinPlaza = -1;

        // Un recorrido en curso.
        private class AutoEnCamino
        {
            public TipoRecorrido Tipo;
            public int IndiceEvento;
            public int IndicePlaza;
            public RectTransform Vista;

            // Salir de la plaza: el primer tramo (de la plaza a la boca) se
            // hace marcha atrás, mirando hacia adentro de la plaza como
            // estaba estacionado.
            public bool PrimerTramoMarchaAtras;

            // Cuánto falta para que arranque, en segundos reales. Mientras
            // espera no se dibuja (ver EsperaDentroDelMinuto).
            public float SegundosDeEspera;

            // Las posiciones (en coordenadas del mundo) por las que pasa, y
            // el piso de cada una.
            public Vector3[] Puntos;
            public int[] Pisos;

            // Cuántos tramos lleva recorridos, con decimales. Termina en
            // Puntos.Length - 1.
            public float Avance;

            public bool Termino => Avance >= Puntos.Length - 1;
        }

        public void Preparar(ReproduccionDto reproduccionAMostrar)
        {
            reproduccion = reproduccionAMostrar;
            siguienteEvento = 0;
            ContarLlegadasPorMinuto();

            int cantidadPlazas = reproduccion.plazas.Length;
            vistaPorPlaza = new PiezaView[cantidadPlazas];
            autoEstacionadoPorPlaza = new GameObject[cantidadPlazas];
            marcaReservadaPorPlaza = new GameObject[cantidadPlazas];

            for (int i = 0; i < cantidadPlazas; i++)
            {
                PlazaReproduccionDto plaza = reproduccion.plazas[i];
                CeldaView ancla = GrillaGenerador.ObtenerCelda(plaza.piso, plaza.fila, plaza.columna);
                vistaPorPlaza[i] = ancla.GetComponentInChildren<PiezaView>(true);

                // La marca se crea antes que el auto para quedar debajo.
                marcaReservadaPorPlaza[i] = CrearMarcaReservada(vistaPorPlaza[i].transform);
                marcaReservadaPorPlaza[i].SetActive(false);

                autoEstacionadoPorPlaza[i] = CrearAutoEstacionado(vistaPorPlaza[i].transform);
                autoEstacionadoPorPlaza[i].SetActive(false);
            }

            capaPorPiso = new RectTransform[GrillaGenerador.CantidadPisos];
            for (int piso = 0; piso < capaPorPiso.Length; piso++)
            {
                capaPorPiso[piso] = CrearCapaAutos(piso);
            }
        }

        // La llama ModoReproduccion cada vez que cambia el minuto entero.
        // Corta los recorridos hacia la plaza que se quedaron atrás, arranca
        // los de los que se van y los de los que llegaron, y prende los autos
        // estacionados. Devuelve cuántos autos tienen plaza en ese minuto
        // (estacionados o yendo hacia ella), que tiene que coincidir con
        // curvaOcupacion.
        //
        // minutosSimuladosPorSegundo: a qué velocidad se está reproduciendo,
        // para saber si un recorrido entra en el tiempo disponible.
        public int MostrarMinuto(int minuto, float minutosSimuladosPorSegundo)
        {
            int minutoAnterior = minutoActual;
            minutoActual = minuto;

            CortarRecorridosVencidos(minuto);
            ArrancarRecorridosDeLosQueSeVan(minutoAnterior, minuto, minutosSimuladosPorSegundo);
            ArrancarRecorridosDeLosQueLlegaron(minuto, minutosSimuladosPorSegundo);
            return MostrarEstacionados(minuto);
        }

        // La llama ModoReproduccion cuando se cambia la velocidad de
        // reproducción. Cada recorrido se decidió con la velocidad que había
        // al arrancar; con la nueva se vuelve a mirar si lo que le falta
        // todavía entra, con el mismo criterio que al arrancarlo. Si no
        // entra se corta, en vez de acumular atraso (animacion/reglas.md):
        // pasar de x1 a x20 con un auto a mitad de un recorrido de 30
        // segundos lo dejaría circulando cientos de minutos simulados.
        //
        // La espera dentro del minuto está en segundos reales: se convierte
        // para que siga faltando lo mismo en minutos simulados.
        public void CambiarVelocidad(float minutosSimuladosPorSegundoAnterior, float minutosSimuladosPorSegundo)
        {
            for (int i = autosEnCamino.Count - 1; i >= 0; i--)
            {
                AutoEnCamino auto = autosEnCamino[i];

                auto.SegundosDeEspera = auto.SegundosDeEspera * minutosSimuladosPorSegundoAnterior
                                        / minutosSimuladosPorSegundo;

                float tramosQueFaltan = auto.Puntos.Length - 1 - auto.Avance;
                float segundosQueFaltan = Mathf.Max(0f, auto.SegundosDeEspera) + tramosQueFaltan / TramosPorSegundo;
                float minutosQueFaltan = segundosQueFaltan * minutosSimuladosPorSegundo;

                bool todaviaEntra;
                if (auto.Tipo == TipoRecorrido.HaciaLaPlaza)
                {
                    int minutoSalida = reproduccion.eventos[auto.IndiceEvento].minutoSalida;
                    todaviaEntra = minutoActual + minutosQueFaltan < minutoSalida;
                }
                else
                {
                    todaviaEntra = minutosQueFaltan <= MaximoMinutosRecorrido;
                }

                if (!todaviaEntra)
                {
                    TerminarRecorrido(auto);
                    autosEnCamino.RemoveAt(i);
                }
            }
        }

        // La llama ModoReproduccion en cada frame mientras se reproduce. Con
        // la reproducción en pausa no se llama, así que los autos se frenan.
        public void Avanzar(float segundosReales)
        {
            for (int i = autosEnCamino.Count - 1; i >= 0; i--)
            {
                AutoEnCamino auto = autosEnCamino[i];

                if (auto.SegundosDeEspera > 0f)
                {
                    auto.SegundosDeEspera -= segundosReales;
                    if (auto.SegundosDeEspera > 0f) continue;

                    auto.Vista.gameObject.SetActive(true);
                }

                auto.Avance += TramosPorSegundo * segundosReales;

                if (auto.Termino)
                {
                    TerminarRecorrido(auto);
                    autosEnCamino.RemoveAt(i);
                }
                else
                {
                    Ubicar(auto);
                }
            }
        }

        // --- Llegadas: hacia la plaza, o rechazado ---

        // Un recorrido hacia la plaza se muestra solo si termina antes de que
        // el auto tenga que irse, a la velocidad de reproducción de ese
        // momento, contando la espera dentro del minuto. Si no entra, el auto
        // aparece directo estacionado: se saltea en vez de acumular atraso
        // (animacion/reglas.md).
        private void ArrancarRecorridosDeLosQueLlegaron(int minuto, float minutosSimuladosPorSegundo)
        {
            while (siguienteEvento < reproduccion.eventos.Length
                   && reproduccion.eventos[siguienteEvento].minutoLlegada <= minuto)
            {
                int indiceEvento = siguienteEvento;
                siguienteEvento++;

                float minutosDeEspera = EsperaDentroDelMinuto(indiceEvento, minuto);
                float segundosDeEspera = minutosDeEspera / minutosSimuladosPorSegundo;

                EventoVehiculoDto evento = reproduccion.eventos[indiceEvento];
                if (evento.indicePlaza < 0)
                {
                    ArrancarRecorridoRechazadoSiEntra(indiceEvento, minutosDeEspera, segundosDeEspera,
                        minutosSimuladosPorSegundo);
                    continue;
                }

                PlazaReproduccionDto plaza = reproduccion.plazas[evento.indicePlaza];
                int tramos = plaza.caminoEntrada.Length;
                float minutosDelRecorrido = tramos / TramosPorSegundo * minutosSimuladosPorSegundo;

                if (minuto + minutosDeEspera + minutosDelRecorrido < evento.minutoSalida)
                {
                    ArrancarRecorridoHaciaLaPlaza(indiceEvento, evento.indicePlaza, segundosDeEspera);
                }
            }
        }

        // Todos los vehículos de un mismo minuto se procesan juntos en el
        // cambio de minuto. Si arrancaran a la vez, los que van a plazas con
        // la misma boca (dos plazas enfrentadas) harían el mismo camino
        // superpuestos y se verían como un solo auto. Por eso se reparten
        // parejo dentro del minuto: de n llegadas, la k-ésima (desde 0)
        // arranca k/n minutos después. Es solo visual: la plaza cuenta como
        // ocupada desde el minuto de llegada, como en el backend.
        //
        // Devuelve cuántos minutos simulados faltan para que arranque. Si el
        // reloj ya pasó ese momento (a velocidad alta salta minutos), 0.
        private float EsperaDentroDelMinuto(int indiceEvento, int minuto)
        {
            EventoVehiculoDto evento = reproduccion.eventos[indiceEvento];
            float momentoDeArranque = evento.minutoLlegada
                                      + (float)ordenEnSuMinuto[indiceEvento] / llegadasEnSuMinuto[indiceEvento];
            return Mathf.Max(0f, momentoDeArranque - minuto);
        }

        // Los eventos vienen en orden de llegada, así que los de un mismo
        // minuto están seguidos.
        private void ContarLlegadasPorMinuto()
        {
            int cantidadEventos = reproduccion.eventos.Length;
            llegadasEnSuMinuto = new int[cantidadEventos];
            ordenEnSuMinuto = new int[cantidadEventos];

            int inicioDelGrupo = 0;
            while (inicioDelGrupo < cantidadEventos)
            {
                int minutoDelGrupo = reproduccion.eventos[inicioDelGrupo].minutoLlegada;
                int finDelGrupo = inicioDelGrupo;
                while (finDelGrupo < cantidadEventos && reproduccion.eventos[finDelGrupo].minutoLlegada == minutoDelGrupo)
                {
                    finDelGrupo++;
                }

                for (int i = inicioDelGrupo; i < finDelGrupo; i++)
                {
                    llegadasEnSuMinuto[i] = finDelGrupo - inicioDelGrupo;
                    ordenEnSuMinuto[i] = i - inicioDelGrupo;
                }
                inicioDelGrupo = finDelGrupo;
            }
        }

        // Puntos: las celdas de caminoEntrada (de la Entrada a la boca) y,
        // al final, el centro de la plaza, que es donde queda el auto
        // estacionado. La plaza se marca como reservada desde ya, aunque el
        // auto todavía espere para arrancar.
        private void ArrancarRecorridoHaciaLaPlaza(int indiceEvento, int indicePlaza, float segundosDeEspera)
        {
            PlazaReproduccionDto plaza = reproduccion.plazas[indicePlaza];

            var puntos = new List<Vector3>();
            var pisos = new List<int>();
            AgregarCeldas(plaza.caminoEntrada, 0, puntos, pisos);
            puntos.Add(CentroDePlaza(indicePlaza));
            pisos.Add(plaza.piso);

            ArrancarRecorrido(TipoRecorrido.HaciaLaPlaza, indiceEvento, indicePlaza, puntos, pisos,
                primerTramoMarchaAtras: false, segundosDeEspera);
            marcaReservadaPorPlaza[indicePlaza].SetActive(true);
        }

        // Si el reloj pasó el minuto de salida de un auto que todavía va
        // hacia su plaza (por ejemplo, porque se subió la velocidad), el
        // recorrido se corta.
        private void CortarRecorridosVencidos(int minuto)
        {
            for (int i = autosEnCamino.Count - 1; i >= 0; i--)
            {
                AutoEnCamino auto = autosEnCamino[i];
                bool haciaLaPlaza = auto.Tipo == TipoRecorrido.HaciaLaPlaza;
                if (haciaLaPlaza && reproduccion.eventos[auto.IndiceEvento].minutoSalida <= minuto)
                {
                    TerminarRecorrido(auto);
                    autosEnCamino.RemoveAt(i);
                }
            }
        }

        // --- Recorridos hacia la salida ---

        // Se van los autos cuyo minuto de salida quedó entre el minuto
        // anterior (excluido) y este (incluido): a velocidad alta el reloj
        // puede saltear minutos. Los que salen después del fin del período
        // nunca llegan acá: quedan estacionados hasta el final.
        private void ArrancarRecorridosDeLosQueSeVan(int minutoAnterior, int minuto, float minutosSimuladosPorSegundo)
        {
            for (int i = 0; i < reproduccion.eventos.Length; i++)
            {
                EventoVehiculoDto evento = reproduccion.eventos[i];
                if (evento.indicePlaza < 0) continue;

                bool seVaAhora = minutoAnterior < evento.minutoSalida && evento.minutoSalida <= minuto;
                if (!seVaAhora) continue;

                PlazaReproduccionDto plaza = reproduccion.plazas[evento.indicePlaza];
                int tramos = plaza.caminoSalida.Length;
                float minutosDelRecorrido = tramos / TramosPorSegundo * minutosSimuladosPorSegundo;

                if (minutosDelRecorrido <= MaximoMinutosRecorrido)
                {
                    ArrancarRecorridoHaciaLaSalida(i, evento.indicePlaza);
                }
            }
        }

        // Puntos: el centro de la plaza y después las celdas de caminoSalida
        // (de la boca a la Salida).
        private void ArrancarRecorridoHaciaLaSalida(int indiceEvento, int indicePlaza)
        {
            PlazaReproduccionDto plaza = reproduccion.plazas[indicePlaza];

            var puntos = new List<Vector3>();
            var pisos = new List<int>();
            puntos.Add(CentroDePlaza(indicePlaza));
            pisos.Add(plaza.piso);
            AgregarCeldas(plaza.caminoSalida, 0, puntos, pisos);

            ArrancarRecorrido(TipoRecorrido.HaciaLaSalida, indiceEvento, indicePlaza, puntos, pisos,
                primerTramoMarchaAtras: true, segundosDeEspera: 0f);
        }

        // --- Rechazados ---

        // El rechazado no se desvanece en la Entrada: recorre el
        // estacionamiento y sale (animacion/reglas.md). Su camino son los dos
        // de la plaza usable más lejana: caminoEntrada hasta la boca y
        // caminoSalida sin la boca, que ya está. No ocupa nada: no marca la
        // plaza ni cuenta en la ocupación, y si se libera una plaza mientras
        // da la vuelta sigue de largo igual.
        //
        // Mismo salteo que la salida: si la espera dentro del minuto más el
        // recorrido duran más del máximo en minutos simulados, no se dibuja
        // (el contador de rechazados sube igual, lo lleva ModoReproduccion).
        // Sin plazas usables (indicePlazaRecorridoRechazados = -1) no hay
        // camino y tampoco se dibuja; con un diseño que pasó RF-20 no pasa.
        private void ArrancarRecorridoRechazadoSiEntra(int indiceEvento, float minutosDeEspera,
            float segundosDeEspera, float minutosSimuladosPorSegundo)
        {
            int indicePlazaDelRecorrido = reproduccion.indicePlazaRecorridoRechazados;
            if (indicePlazaDelRecorrido < 0) return;

            PlazaReproduccionDto plaza = reproduccion.plazas[indicePlazaDelRecorrido];

            var puntos = new List<Vector3>();
            var pisos = new List<int>();
            AgregarCeldas(plaza.caminoEntrada, 0, puntos, pisos);
            AgregarCeldas(plaza.caminoSalida, 1, puntos, pisos);

            int tramos = puntos.Count - 1;
            float minutosDelRecorrido = tramos / TramosPorSegundo * minutosSimuladosPorSegundo;
            if (minutosDeEspera + minutosDelRecorrido > MaximoMinutosRecorrido) return;

            ArrancarRecorrido(TipoRecorrido.Rechazado, indiceEvento, SinPlaza, puntos, pisos,
                primerTramoMarchaAtras: false, segundosDeEspera);
        }

        // --- Recorridos en general ---

        // Las posiciones se calculan al arrancar y no al preparar porque
        // dependen del tamaño de la pantalla.
        private void ArrancarRecorrido(TipoRecorrido tipo, int indiceEvento, int indicePlaza,
            List<Vector3> puntos, List<int> pisos, bool primerTramoMarchaAtras, float segundosDeEspera)
        {
            var auto = new AutoEnCamino
            {
                Tipo = tipo,
                IndiceEvento = indiceEvento,
                IndicePlaza = indicePlaza,
                Vista = CrearAutoEnCamino(capaPorPiso[pisos[0]], tipo == TipoRecorrido.Rechazado),
                PrimerTramoMarchaAtras = primerTramoMarchaAtras,
                SegundosDeEspera = segundosDeEspera,
                Puntos = puntos.ToArray(),
                Pisos = pisos.ToArray(),
                Avance = 0f
            };
            autosEnCamino.Add(auto);
            Ubicar(auto);
            auto.Vista.gameObject.SetActive(segundosDeEspera <= 0f);
        }

        // Agrega el centro de cada celda del camino, desde la posición
        // indicada.
        private static void AgregarCeldas(CeldaCaminoDto[] camino, int desde, List<Vector3> puntos, List<int> pisos)
        {
            for (int i = desde; i < camino.Length; i++)
            {
                CeldaView celda = GrillaGenerador.ObtenerCelda(camino[i].piso, camino[i].fila, camino[i].columna);
                puntos.Add(CentroEnElMundo((RectTransform)celda.transform));
                pisos.Add(camino[i].piso);
            }
        }

        private Vector3 CentroDePlaza(int indicePlaza)
        {
            return CentroEnElMundo((RectTransform)vistaPorPlaza[indicePlaza].transform);
        }

        // El auto en camino desaparece. Si iba hacia su plaza y todavía le
        // toca estar, lo reemplaza el estacionado, que está en el mismo lugar
        // y con la misma orientación.
        private void TerminarRecorrido(AutoEnCamino auto)
        {
            Destroy(auto.Vista.gameObject);

            if (auto.Tipo != TipoRecorrido.HaciaLaPlaza) return;

            marcaReservadaPorPlaza[auto.IndicePlaza].SetActive(false);
            EventoVehiculoDto evento = reproduccion.eventos[auto.IndiceEvento];
            bool todaviaLeTocaEstar = minutoActual < evento.minutoSalida;
            autoEstacionadoPorPlaza[auto.IndicePlaza].SetActive(todaviaLeTocaEstar);
        }

        // --- Autos estacionados ---

        // Un vehículo con plaza la ocupa en los minutos m con
        // minutoLlegada <= m < minutoSalida (contratos/api-contract.md), el
        // mismo criterio con el que el backend arma curvaOcupacion. Si va en
        // camino, cuenta igual pero todavía no se lo dibuja estacionado.
        private int MostrarEstacionados(int minuto)
        {
            bool[] plazaOcupada = new bool[autoEstacionadoPorPlaza.Length];
            foreach (EventoVehiculoDto evento in reproduccion.eventos)
            {
                bool tienePlaza = evento.indicePlaza >= 0;
                bool ocupa = evento.minutoLlegada <= minuto && minuto < evento.minutoSalida;
                if (tienePlaza && ocupa)
                {
                    plazaOcupada[evento.indicePlaza] = true;
                }
            }

            // Solo los que van hacia la plaza: el que se va ya la dejó libre.
            bool[] plazaConAutoEnCamino = new bool[autoEstacionadoPorPlaza.Length];
            foreach (AutoEnCamino auto in autosEnCamino)
            {
                if (auto.Tipo == TipoRecorrido.HaciaLaPlaza)
                {
                    plazaConAutoEnCamino[auto.IndicePlaza] = true;
                }
            }

            int autosConPlaza = 0;
            for (int i = 0; i < autoEstacionadoPorPlaza.Length; i++)
            {
                autoEstacionadoPorPlaza[i].SetActive(plazaOcupada[i] && !plazaConAutoEnCamino[i]);
                if (plazaOcupada[i])
                {
                    autosConPlaza++;
                }
            }
            return autosConPlaza;
        }

        // --- Movimiento ---

        // Entre dos puntos el auto avanza en línea recta y mira hacia donde
        // va. En el salto de una rampa los dos puntos están en el mismo lugar
        // de la pantalla, en pisos distintos: el auto no se mueve y a mitad
        // del tramo pasa a la capa del otro piso.
        private void Ubicar(AutoEnCamino auto)
        {
            int tramo = Mathf.Min((int)auto.Avance, auto.Puntos.Length - 2);
            float recorridoDelTramo = auto.Avance - tramo;
            Vector3 desde = auto.Puntos[tramo];
            Vector3 hasta = auto.Puntos[tramo + 1];

            // Las capas de todos los pisos tienen la misma escala, así que el
            // cambio de capa no altera el tamaño; la posición se fija abajo.
            int piso = recorridoDelTramo < 0.5f ? auto.Pisos[tramo] : auto.Pisos[tramo + 1];
            if (auto.Vista.parent != capaPorPiso[piso])
            {
                auto.Vista.SetParent(capaPorPiso[piso], false);
            }

            auto.Vista.position = Vector3.Lerp(desde, hasta, recorridoDelTramo);

            Vector3 direccion = hasta - desde;
            if (direccion.sqrMagnitude > 0.0001f)
            {
                // El sprite del auto mira hacia arriba (+Y), que es el ángulo
                // 90°: se resta para que mire hacia la dirección.
                float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg - 90f;
                bool marchaAtras = auto.PrimerTramoMarchaAtras && tramo == 0;
                if (marchaAtras)
                {
                    angulo += 180f;
                }
                auto.Vista.rotation = Quaternion.Euler(0f, 0f, angulo);
            }
        }

        private static Vector3 CentroEnElMundo(RectTransform rect)
        {
            return rect.TransformPoint(rect.rect.center);
        }

        // --- Creación de objetos ---

        // Fuera del GridLayoutGroup (ignoreLayout), estirada sobre el piso.
        // El Canvas que la dibuja encima de las piezas lo crea
        // CapaAutosReproduccion (ver ahí por qué). Sin GraphicRaycaster: no
        // recibe clics.
        private static RectTransform CrearCapaAutos(int piso)
        {
            var capa = new GameObject($"CapaAutos_Piso{piso}", typeof(RectTransform), typeof(LayoutElement));
            capa.transform.SetParent(GrillaGenerador.ContenedorDePiso(piso), false);
            capa.GetComponent<LayoutElement>().ignoreLayout = true;

            // Se agrega recién colgada del piso: su Awake crea el Canvas y,
            // si corriera con la capa todavía sin padre, sería un Canvas raíz
            // y Unity ignora el overrideSorting.
            capa.AddComponent<CapaAutosReproduccion>();

            RectTransform rect = capa.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return rect;
        }

        // Estirado sobre la plaza, menos el margen. La plaza es vertical
        // antes de rotar (una celda de ancho, dos de largo), igual que el
        // sprite del auto. Girado 180°: la boca de la plaza queda arriba en
        // su sprite y el auto entra de frente, así que queda mirando hacia
        // adentro, igual que llega el auto en camino.
        private GameObject CrearAutoEstacionado(Transform vistaPlaza)
        {
            GameObject auto = CrearImagenAuto("AutoEstacionado");
            auto.transform.SetParent(vistaPlaza, false);

            RectTransform rect = auto.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(MargenAncho, MargenLargo);
            rect.anchorMax = new Vector2(1f - MargenAncho, 1f - MargenLargo);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localEulerAngles = new Vector3(0f, 0f, 180f);

            return auto;
        }

        // Mismo tamaño que el estacionado, para que no cambie al llegar. Se
        // cuelga de la capa sin conservar la posición del mundo: así toma la
        // escala del Canvas como cualquier otro elemento de la grilla.
        //
        // El rechazado va en rojo durante todo el recorrido
        // (animacion/reglas.md). El color multiplica al sprite, que es
        // blanco y gris: la carrocería queda roja y los vidrios oscuros.
        private RectTransform CrearAutoEnCamino(RectTransform capa, bool esRechazado)
        {
            GameObject auto = CrearImagenAuto(esRechazado ? "AutoRechazado" : "AutoEnCamino");
            auto.transform.SetParent(capa, false);
            if (esRechazado)
            {
                auto.GetComponent<Image>().color = ColorRechazado;
            }
            RectTransform rect = auto.GetComponent<RectTransform>();

            Vector2 celda = GrillaGenerador.TamanioCelda;
            float largoPlaza = celda.y * 2f + GrillaGenerador.Espaciado.y;
            rect.sizeDelta = new Vector2(celda.x * (1f - 2f * MargenAncho), largoPlaza * (1f - 2f * MargenLargo));

            return rect;
        }

        private GameObject CrearImagenAuto(string nombre)
        {
            var auto = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            Image imagen = auto.GetComponent<Image>();
            imagen.sprite = spriteAuto;
            imagen.preserveAspect = true;
            imagen.raycastTarget = false;
            return auto;
        }

        private static GameObject CrearMarcaReservada(Transform vistaPlaza)
        {
            var marca = new GameObject("MarcaReservada", typeof(RectTransform), typeof(Image));
            marca.transform.SetParent(vistaPlaza, false);

            RectTransform rect = marca.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image imagen = marca.GetComponent<Image>();
            imagen.color = ColorReservada;
            imagen.raycastTarget = false;
            return marca;
        }
    }
}
