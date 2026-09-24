using System;
using System.Collections.Generic;

namespace Sme.Grid
{
    // RF-20: Validar consistencia del diseño. Las 7 verificaciones no
    // bloqueantes de editor/validaciones.md — dejan colocar, resaltan en
    // rojo las celdas afectadas y, mientras haya alguna, el diseño no se
    // puede simular (DisenoValido, lo va a leer el botón de RF-26).
    //
    // Se recalcula entera cada vez que cambia algo en la grilla: CeldaView
    // pide la validación al ocupar o liberar una celda, y GrillaGenerador
    // la corre una sola vez por frame (LateUpdate), así colocar una rampa en
    // 3 pisos o reconstruir un proyecto guardado no la dispara cientos de
    // veces seguidas.
    public static class ValidadorDiseno
    {
        private static readonly CaraAcceso[] Direcciones =
        {
            CaraAcceso.NORTE, CaraAcceso.SUR, CaraAcceso.ESTE, CaraAcceso.OESTE
        };

        // Decreto 35.865, Art. 5: cada zona de bicicletas/motos cuenta como
        // 5 espacios (editor/catalogo-piezas.md).
        private const int EspaciosPorZonaBiciMoto = 5;

        public class Resultado
        {
            public readonly HashSet<CeldaGrilla> CeldasMarcadas = new();
            public readonly HashSet<int> PisosConAdvertencias = new();
            public readonly List<string> Mensajes = new();

            public bool DisenoValido => Mensajes.Count == 0;
        }

        public static bool HayValidacionPendiente { get; private set; }
        public static Resultado UltimoResultado { get; private set; }

        // PanelAdvertencias y SelectorPisos se suscriben para mostrar los
        // mensajes y marcar los pisos con problemas.
        public static event Action<Resultado> AlValidar;

        public static void PedirValidacion()
        {
            HayValidacionPendiente = true;
        }

        public static Resultado Validar()
        {
            HayValidacionPendiente = false;

            var resultado = new Resultado();
            var celdas = new List<CeldaGrilla>(GrillaModelo.TodasLasCeldas());

            VerificarSalidasValidas(celdas, resultado);
            bool hayEntradaYSalida = VerificarEntradaYSalida(celdas, resultado);
            // Sin Entrada o sin Salida todas las plazas serían inalcanzables:
            // se muestra solo lo que falta, no un rojo en cada plaza.
            if (hayEntradaYSalida)
            {
                VerificarAlcanzabilidad(celdas, resultado);
            }
            VerificarCaminoPeatonalEscaleras(celdas, resultado);
            VerificarPlazasLleganAEscalera(celdas, resultado);
            VerificarRampasDeIdaYVuelta(celdas, resultado);
            VerificarPlazasAccesibles(celdas, resultado);
            VerificarEspaciosBiciMoto(celdas, resultado);

            UltimoResultado = resultado;
            AlValidar?.Invoke(resultado);
            return resultado;
        }

        // --- 1. Ninguna celda de circulación sin salida válida ---

        private static void VerificarSalidasValidas(List<CeldaGrilla> celdas, Resultado resultado)
        {
            var sinSalidaPorPiso = new SortedDictionary<int, int>();
            var enfrentadasPorPiso = new SortedDictionary<int, int>();

            foreach (CeldaGrilla celda in celdas)
            {
                if (!GrafoCirculacion.EsCeldaDeCirculacion(celda.Tipo)) continue;
                if (GrafoCirculacion.TieneSalidaValida(celda)) continue;

                Marcar(celda, resultado);
                Sumar(sinSalidaPorPiso, celda.Piso);
                if (GrafoCirculacion.EstaEnfrentada(celda))
                {
                    Sumar(enfrentadasPorPiso, celda.Piso);
                }
            }

            foreach (KeyValuePair<int, int> par in sinSalidaPorPiso)
            {
                string mensaje = par.Value == 1
                    ? $"{EncabezadoPiso(par.Key)}: hay 1 celda donde el auto queda atrapado, sin ninguna salida posible."
                    : $"{EncabezadoPiso(par.Key)}: hay {par.Value} celdas donde el auto queda atrapado, sin ninguna salida posible.";
                if (enfrentadasPorPiso.TryGetValue(par.Key, out int enfrentadas))
                {
                    mensaje += enfrentadas == 1
                        ? " En 1 de ellas la calle choca contra otra que viene de frente."
                        : $" En {enfrentadas} de ellas la calle choca contra otra que viene de frente.";
                }
                resultado.Mensajes.Add(mensaje);
            }
        }

        // --- 2. Debe existir una Entrada y una Salida ---

        private static bool VerificarEntradaYSalida(List<CeldaGrilla> celdas, Resultado resultado)
        {
            bool hayEntrada = BuscarPrimeraDeTipo(celdas, TipoPieza.ENTRADA) != null;
            bool haySalida = BuscarPrimeraDeTipo(celdas, TipoPieza.SALIDA) != null;

            if (!hayEntrada)
            {
                resultado.Mensajes.Add("Falta colocar la Entrada de vehículos.");
            }
            if (!haySalida)
            {
                resultado.Mensajes.Add("Falta colocar la Salida de vehículos.");
            }
            return hayEntrada && haySalida;
        }

        // --- 3. Toda Plaza y ZonaBicicletasMotos alcanzable ---

        // Una plaza se engancha al grafo por la celda vecina de su boca
        // (editor/grafo-circulacion.md). Es alcanzable si a esa celda se
        // llega desde la Entrada siguiendo las aristas, y si desde esa
        // celda se puede seguir hasta la Salida — con calles de un sentido es
        // fácil armar una red de la que el vehículo no encuentra cómo salir.
        private static void VerificarAlcanzabilidad(List<CeldaGrilla> celdas, Resultado resultado)
        {
            CeldaGrilla entrada = BuscarPrimeraDeTipo(celdas, TipoPieza.ENTRADA);
            CeldaGrilla salida = BuscarPrimeraDeTipo(celdas, TipoPieza.SALIDA);

            var sucesores = new Dictionary<CeldaGrilla, List<CeldaGrilla>>();
            var predecesores = new Dictionary<CeldaGrilla, List<CeldaGrilla>>();
            foreach (CeldaGrilla celda in celdas)
            {
                if (!GrafoCirculacion.EsCeldaDeCirculacion(celda.Tipo)) continue;

                sucesores[celda] = GrafoCirculacion.Sucesores(celda);
                if (!predecesores.ContainsKey(celda))
                {
                    predecesores[celda] = new List<CeldaGrilla>();
                }
                foreach (CeldaGrilla sucesor in sucesores[celda])
                {
                    if (!predecesores.ContainsKey(sucesor))
                    {
                        predecesores[sucesor] = new List<CeldaGrilla>();
                    }
                    predecesores[sucesor].Add(celda);
                }
            }

            HashSet<CeldaGrilla> alcanzablesDesdeEntrada = Recorrer(entrada, sucesores);
            HashSet<CeldaGrilla> puedenLlegarASalida = Recorrer(salida, predecesores);

            // Dos problemas distintos, con mensajes distintos: no poder llegar
            // desde la Entrada, o llegar pero no poder volver a la Salida (el
            // auto queda dando vueltas en un circuito cerrado o atrapado).
            var sinLlegadaPorPiso = new SortedDictionary<int, ConteoPiezas>();
            var sinVueltaPorPiso = new SortedDictionary<int, ConteoPiezas>();
            foreach (CeldaGrilla celda in celdas)
            {
                if (!EsBocaDeEstacionamiento(celda)) continue;

                (int deltaFila, int deltaColumna) = GrillaModelo.Delta(celda.Direccion.Value);
                CeldaGrilla boca = GrillaModelo.ObtenerCelda(celda.Piso, celda.Fila + deltaFila, celda.Columna + deltaColumna);

                bool llegaDesdeEntrada = boca != null && alcanzablesDesdeEntrada.Contains(boca);
                bool vuelveASalida = boca != null && puedenLlegarASalida.Contains(boca);
                if (llegaDesdeEntrada && vuelveASalida) continue;

                MarcarPiezaDeEstacionamiento(celda, resultado);
                SumarPieza(llegaDesdeEntrada ? sinVueltaPorPiso : sinLlegadaPorPiso, celda);
            }

            foreach (KeyValuePair<int, ConteoPiezas> par in sinLlegadaPorPiso)
            {
                resultado.Mensajes.Add($"{EncabezadoPiso(par.Key)}: el auto no puede llegar desde la Entrada a {par.Value.Describir()}.");
            }
            foreach (KeyValuePair<int, ConteoPiezas> par in sinVueltaPorPiso)
            {
                resultado.Mensajes.Add($"{EncabezadoPiso(par.Key)}: el auto llega a {par.Value.Describir()}, pero desde ahí no puede volver a la Salida (queda dando vueltas o sin salida).");
            }
        }

        // La celda que tiene la boca: el ancla de una Plaza (la del fondo
        // guarda PLAZA con Direccion null) o la única celda de una
        // ZonaBicicletasMotos.
        private static bool EsBocaDeEstacionamiento(CeldaGrilla celda)
        {
            bool esAnclaDePlaza = celda.Tipo == TipoPieza.PLAZA && celda.Direccion != null;
            return esAnclaDePlaza || celda.Tipo == TipoPieza.ZONA_BICI_MOTO;
        }

        // Una Plaza se marca entera: el ancla y la celda del fondo, que se
        // extiende en la dirección opuesta a caraAcceso.
        private static void MarcarPiezaDeEstacionamiento(CeldaGrilla boca, Resultado resultado)
        {
            Marcar(boca, resultado);
            if (boca.Tipo != TipoPieza.PLAZA) return;

            (int deltaFila, int deltaColumna) = GrillaModelo.Delta(GrillaModelo.Opuesta(boca.Direccion.Value));
            CeldaGrilla fondo = GrillaModelo.ObtenerCelda(boca.Piso, boca.Fila + deltaFila, boca.Columna + deltaColumna);
            if (fondo != null)
            {
                Marcar(fondo, resultado);
            }
        }

        // Recorrido en anchura desde origen siguiendo las listas de vecinos
        // dadas (sucesores para ir hacia adelante, predecesores para ir hacia
        // atrás). Devuelve todas las celdas visitadas, origen incluido.
        private static HashSet<CeldaGrilla> Recorrer(CeldaGrilla origen, Dictionary<CeldaGrilla, List<CeldaGrilla>> vecinos)
        {
            var visitadas = new HashSet<CeldaGrilla> { origen };
            var pendientes = new Queue<CeldaGrilla>();
            pendientes.Enqueue(origen);

            while (pendientes.Count > 0)
            {
                CeldaGrilla actual = pendientes.Dequeue();
                if (!vecinos.TryGetValue(actual, out List<CeldaGrilla> siguientes)) continue;

                foreach (CeldaGrilla siguiente in siguientes)
                {
                    if (visitadas.Add(siguiente))
                    {
                        pendientes.Enqueue(siguiente);
                    }
                }
            }
            return visitadas;
        }

        // --- 4. Camino peatonal de la escalera ---

        // Desde donde la escalera llega a planta baja tiene que haber un
        // camino a pie, sin direcciones, hasta una Entrada o una Salida
        // (editor/grafo-circulacion.md). Se camina por Plaza, Calle, Entrada,
        // Salida, ZonaBicicletasMotos y Escalera; no por celdas vacías ni por
        // Rampa. Si no hay camino se marca la escalera en todos los pisos.
        private static void VerificarCaminoPeatonalEscaleras(List<CeldaGrilla> celdas, Resultado resultado)
        {
            foreach (CeldaGrilla celda in celdas)
            {
                if (celda.Tipo != TipoPieza.ESCALERA || celda.Piso != 0) continue;
                if (TieneCaminoPeatonalASalida(celda)) continue;

                for (int piso = 0; piso < GrillaModelo.CantidadPisos; piso++)
                {
                    CeldaGrilla deLaColumna = GrillaModelo.ObtenerCelda(piso, celda.Fila, celda.Columna);
                    if (deLaColumna != null && deLaColumna.Tipo == TipoPieza.ESCALERA)
                    {
                        Marcar(deLaColumna, resultado);
                    }
                }
                resultado.Mensajes.Add($"La escalera de la fila {celda.Fila + 1}, columna {celda.Columna + 1} no tiene camino a pie en planta baja hasta la Entrada o la Salida.");
            }
        }

        private static bool TieneCaminoPeatonalASalida(CeldaGrilla escaleraEnPlantaBaja)
        {
            foreach (CeldaGrilla alcanzada in RecorrerAPie(new List<CeldaGrilla> { escaleraEnPlantaBaja }))
            {
                if (alcanzada.Tipo == TipoPieza.ENTRADA || alcanzada.Tipo == TipoPieza.SALIDA) return true;
            }
            return false;
        }

        // --- 4b. En los pisos de arriba, toda plaza llega a pie a una escalera ---

        // Quien estaciona en un piso que no es planta baja tiene que poder
        // bajar caminando: desde cada Plaza y ZonaBicicletasMotos de ese piso
        // tiene que haber camino a pie hasta alguna escalera del mismo piso.
        // Si el proyecto no tiene ninguna escalera, todas las plazas de arriba
        // quedan sin camino. En planta baja no aplica: ahí el peatón sale por
        // la Entrada o la Salida sin bajar.
        private static void VerificarPlazasLleganAEscalera(List<CeldaGrilla> celdas, Resultado resultado)
        {
            for (int piso = 1; piso < GrillaModelo.CantidadPisos; piso++)
            {
                var escaleras = new List<CeldaGrilla>();
                var bocas = new List<CeldaGrilla>();
                foreach (CeldaGrilla celda in celdas)
                {
                    if (celda.Piso != piso) continue;

                    if (celda.Tipo == TipoPieza.ESCALERA)
                    {
                        escaleras.Add(celda);
                    }
                    else if (EsBocaDeEstacionamiento(celda))
                    {
                        bocas.Add(celda);
                    }
                }
                if (bocas.Count == 0) continue;

                HashSet<CeldaGrilla> alcanzadasAPie = RecorrerAPie(escaleras);

                var sinCamino = new ConteoPiezas();
                foreach (CeldaGrilla boca in bocas)
                {
                    // Llegar al ancla alcanza: la celda del fondo de una Plaza
                    // es contigua y también caminable.
                    if (alcanzadasAPie.Contains(boca)) continue;

                    MarcarPiezaDeEstacionamiento(boca, resultado);
                    sinCamino.Sumar(boca);
                }
                if (sinCamino.Total == 0) continue;

                resultado.Mensajes.Add(escaleras.Count == 0
                    ? $"{EncabezadoPiso(piso)}: no hay ninguna escalera, así que desde {sinCamino.Describir()} no se puede bajar a pie."
                    : $"{EncabezadoPiso(piso)}: desde {sinCamino.Describir()} no hay camino a pie hasta una escalera para bajar.");
            }
        }

        // Recorrido a pie, sin direcciones, dentro del mismo piso: desde los
        // orígenes, por celdas caminables contiguas. Devuelve todas las celdas
        // alcanzadas, orígenes incluidos.
        private static HashSet<CeldaGrilla> RecorrerAPie(List<CeldaGrilla> origenes)
        {
            var visitadas = new HashSet<CeldaGrilla>(origenes);
            var pendientes = new Queue<CeldaGrilla>(origenes);

            while (pendientes.Count > 0)
            {
                CeldaGrilla actual = pendientes.Dequeue();
                foreach (CaraAcceso direccion in Direcciones)
                {
                    (int deltaFila, int deltaColumna) = GrillaModelo.Delta(direccion);
                    CeldaGrilla vecina = GrillaModelo.ObtenerCelda(actual.Piso, actual.Fila + deltaFila, actual.Columna + deltaColumna);
                    if (vecina != null && EsCaminable(vecina.Tipo) && visitadas.Add(vecina))
                    {
                        pendientes.Enqueue(vecina);
                    }
                }
            }
            return visitadas;
        }

        private static bool EsCaminable(TipoPieza? tipo)
        {
            return tipo == TipoPieza.PLAZA || tipo == TipoPieza.CALLE || tipo == TipoPieza.ENTRADA
                || tipo == TipoPieza.SALIDA || tipo == TipoPieza.ZONA_BICI_MOTO || tipo == TipoPieza.ESCALERA;
        }

        // --- 5. Rampas de ida y vuelta ---

        // Por cada par de pisos contiguos que tenga una rampa que sube tiene
        // que existir una que baja entre esos mismos pisos. Una que sube entre
        // k y k+1 entra por k; una que baja entre k y k+1 entra por k+1.
        private static void VerificarRampasDeIdaYVuelta(List<CeldaGrilla> celdas, Resultado resultado)
        {
            var subenPorPisoInferior = new SortedDictionary<int, List<CeldaGrilla>>();
            var bajanPorPisoInferior = new HashSet<int>();

            foreach (CeldaGrilla celda in celdas)
            {
                if (celda.Tipo != TipoPieza.RAMPA || !celda.EsEntradaDeRampa) continue;

                if (celda.SentidoVertical == SentidoVertical.SUBE)
                {
                    if (!subenPorPisoInferior.ContainsKey(celda.Piso))
                    {
                        subenPorPisoInferior[celda.Piso] = new List<CeldaGrilla>();
                    }
                    subenPorPisoInferior[celda.Piso].Add(celda);
                }
                else
                {
                    bajanPorPisoInferior.Add(celda.Piso - 1);
                }
            }

            foreach (KeyValuePair<int, List<CeldaGrilla>> par in subenPorPisoInferior)
            {
                int pisoInferior = par.Key;
                if (bajanPorPisoInferior.Contains(pisoInferior)) continue;

                foreach (CeldaGrilla entradaDeRampa in par.Value)
                {
                    Marcar(entradaDeRampa, resultado);
                    CeldaGrilla salidaDeRampa = GrafoCirculacion.ParDeRampa(entradaDeRampa);
                    if (salidaDeRampa != null)
                    {
                        Marcar(salidaDeRampa, resultado);
                    }
                }
                resultado.Mensajes.Add($"Entre {GrillaGenerador.NombrePiso(pisoInferior)} y {GrillaGenerador.NombrePiso(pisoInferior + 1)} hay una rampa que sube pero ninguna que baja: los autos que suben no pueden volver.");
            }
        }

        // --- 6. Plazas accesibles (Resolución IM Nº 0868/22) ---

        private static void VerificarPlazasAccesibles(List<CeldaGrilla> celdas, Resultado resultado)
        {
            int plazas = 0;
            int accesibles = 0;
            foreach (CeldaGrilla celda in celdas)
            {
                bool esAnclaDePlaza = celda.Tipo == TipoPieza.PLAZA && celda.Direccion != null;
                if (!esAnclaDePlaza) continue;

                plazas++;
                if (celda.EsAccesible)
                {
                    accesibles++;
                }
            }

            int minimo = MinimoPlazasAccesibles(plazas);
            if (accesibles < minimo)
            {
                resultado.Mensajes.Add($"Faltan plazas accesibles: hay {accesibles} y con {Cantidad(plazas, "plaza", "plazas")} se necesitan al menos {minimo}.");
            }
        }

        // Escala de la resolución, tal como la definió el equipo: hasta 300
        // plazas por tramos fijos, y desde ahí una accesible más cada 100
        // plazas o fracción.
        public static int MinimoPlazasAccesibles(int plazas)
        {
            if (plazas <= 0) return 0;
            if (plazas <= 10) return 1;
            if (plazas <= 25) return 2;
            if (plazas <= 50) return 3;
            if (plazas <= 75) return 4;
            if (plazas <= 100) return 5;
            if (plazas <= 150) return 6;
            if (plazas <= 200) return 7;
            if (plazas <= 300) return 8;
            return 8 + DividirRedondeandoArriba(plazas - 300, 100);
        }

        // --- 7. Espacios de bicicletas y motos (Decreto 35.865, Art. 5) ---

        private static void VerificarEspaciosBiciMoto(List<CeldaGrilla> celdas, Resultado resultado)
        {
            int plazas = 0;
            int zonas = 0;
            foreach (CeldaGrilla celda in celdas)
            {
                if (celda.Tipo == TipoPieza.PLAZA && celda.Direccion != null)
                {
                    plazas++;
                }
                else if (celda.Tipo == TipoPieza.ZONA_BICI_MOTO)
                {
                    zonas++;
                }
            }

            int espacios = zonas * EspaciosPorZonaBiciMoto;
            int minimo = MinimoEspaciosBiciMoto(plazas);
            if (espacios < minimo)
            {
                resultado.Mensajes.Add($"Faltan espacios para bicis/motos: hay {espacios} (cada zona tiene {EspaciosPorZonaBiciMoto}) y con {Cantidad(plazas, "plaza", "plazas")} se necesitan al menos {minimo}.");
            }
        }

        // Al menos un espacio cada 5 plazas de auto, redondeando hacia
        // arriba: 1 a 5 plazas ya piden 1 espacio.
        public static int MinimoEspaciosBiciMoto(int plazas)
        {
            return DividirRedondeandoArriba(plazas, 5);
        }

        // --- Auxiliares ---

        private static void Marcar(CeldaGrilla celda, Resultado resultado)
        {
            resultado.CeldasMarcadas.Add(celda);
            resultado.PisosConAdvertencias.Add(celda.Piso);
        }

        private static CeldaGrilla BuscarPrimeraDeTipo(List<CeldaGrilla> celdas, TipoPieza tipo)
        {
            foreach (CeldaGrilla celda in celdas)
            {
                if (celda.Tipo == tipo) return celda;
            }
            return null;
        }

        // Cuenta plazas y zonas de bicis/motos por separado para que el
        // mensaje diga "2 plazas y 1 zona de bicis/motos" en vez de mezclarlas.
        private class ConteoPiezas
        {
            public int Plazas;
            public int Zonas;
            public int Total => Plazas + Zonas;

            public void Sumar(CeldaGrilla boca)
            {
                if (boca.Tipo == TipoPieza.ZONA_BICI_MOTO)
                {
                    Zonas++;
                }
                else
                {
                    Plazas++;
                }
            }

            public string Describir()
            {
                string plazas = Cantidad(Plazas, "plaza", "plazas");
                string zonas = Cantidad(Zonas, "zona de bicis/motos", "zonas de bicis/motos");
                if (Zonas == 0) return plazas;
                if (Plazas == 0) return zonas;
                return $"{plazas} y {zonas}";
            }
        }

        private static void SumarPieza(SortedDictionary<int, ConteoPiezas> conteoPorPiso, CeldaGrilla boca)
        {
            if (!conteoPorPiso.ContainsKey(boca.Piso))
            {
                conteoPorPiso[boca.Piso] = new ConteoPiezas();
            }
            conteoPorPiso[boca.Piso].Sumar(boca);
        }

        private static void Sumar(SortedDictionary<int, int> cuentaPorPiso, int piso)
        {
            cuentaPorPiso.TryGetValue(piso, out int actual);
            cuentaPorPiso[piso] = actual + 1;
        }

        private static int DividirRedondeandoArriba(int dividendo, int divisor)
        {
            if (dividendo <= 0) return 0;
            return (dividendo + divisor - 1) / divisor;
        }

        private static string EncabezadoPiso(int piso)
        {
            return piso == 0 ? "Planta baja" : $"Piso {piso}";
        }

        private static string Cantidad(int cantidad, string singular, string plural)
        {
            return cantidad == 1 ? $"1 {singular}" : $"{cantidad} {plural}";
        }
    }
}
