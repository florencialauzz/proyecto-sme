using System;
using System.Collections.Generic;

namespace Sme.Grid
{
    public static class ValidadorDiseno
    {
        private static readonly CaraAcceso[] Direcciones =
        {
            CaraAcceso.NORTE, CaraAcceso.SUR, CaraAcceso.ESTE, CaraAcceso.OESTE
        };

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
            if (hayEntradaYSalida)
            {
                VerificarAlcanzabilidad(celdas, resultado);
            }
            VerificarCaminoPeatonalEscaleras(celdas, resultado);
            VerificarEscalerasConectadasArriba(celdas, resultado);
            VerificarPlazasLleganAEscalera(celdas, resultado);
            VerificarRampasDeIdaYVuelta(celdas, resultado);
            VerificarPlazasAccesibles(celdas, resultado);
            VerificarEspaciosBiciMoto(celdas, resultado);

            UltimoResultado = resultado;
            AlValidar?.Invoke(resultado);
            return resultado;
        }

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

        private static bool EsBocaDeEstacionamiento(CeldaGrilla celda)
        {
            bool esAnclaDePlaza = celda.Tipo == TipoPieza.PLAZA && celda.Direccion != null;
            return esAnclaDePlaza || celda.Tipo == TipoPieza.ZONA_BICI_MOTO;
        }

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

        private static void VerificarEscalerasConectadasArriba(List<CeldaGrilla> celdas, Resultado resultado)
        {
            foreach (CeldaGrilla celda in celdas)
            {
                if (celda.Tipo != TipoPieza.ESCALERA || celda.Piso == 0) continue;
                if (EstaConectadaAPie(celda)) continue;

                Marcar(celda, resultado);
                resultado.Mensajes.Add($"{EncabezadoPiso(celda.Piso)}: la escalera de la fila {celda.Fila + 1}, columna {celda.Columna + 1} no está conectada a ninguna calle ni plaza.");
            }
        }

        private static bool EstaConectadaAPie(CeldaGrilla escalera)
        {
            foreach (CeldaGrilla alcanzada in RecorrerAPie(new List<CeldaGrilla> { escalera }))
            {
                if (alcanzada.Tipo != TipoPieza.ESCALERA) return true;
            }
            return false;
        }

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

        public static int MinimoEspaciosBiciMoto(int plazas)
        {
            return DividirRedondeandoArriba(plazas, 5);
        }

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
