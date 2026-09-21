using System.Collections.Generic;

namespace Sme.Grid
{
    // Qué forma de fondo le toca a una Calle y con qué rotación, según
    // editor/catalogo-piezas.md. Recta cubre 0, 1 y 2 conexiones
    // colineales — no hay sprite propio para "aislada" ni "extremo".
    //
    // Convención de los sprites SIN ROTAR (0°), fija por el arte que se
    // asigna en el prefab — este es el único lugar que la conoce:
    //   Recta   conecta NORTE + SUR
    //   Curva   conecta OESTE + SUR
    //   CruceT  conecta todo menos OESTE (NORTE + ESTE + SUR) — el paso
    //           peatonal recto del sprite queda sobre la cara sin calle
    //           (OESTE), las rampas de cordón sobre las esquinas con calle
    //   CrucePlus es simétrico, nunca rota
    // Si el arte final usa otra orientación de base, ajustar acá los
    // arrays Base* — no hace falta tocar nada más.
    public static class AutotilingCalle
    {
        public enum Forma
        {
            Recta,
            Curva,
            CruceT,
            CrucePlus
        }

        private static readonly CaraAcceso[] BaseCurva = { CaraAcceso.OESTE, CaraAcceso.SUR };
        private static readonly CaraAcceso[] BaseCruceT = { CaraAcceso.NORTE, CaraAcceso.ESTE, CaraAcceso.SUR };

        public static (Forma forma, int pasosRotacion) Elegir(CeldaGrilla celda)
        {
            CaraAcceso[] conectadas = GrafoCirculacion.ObtenerDireccionesConectadas(celda);

            if (conectadas.Length == 0)
            {
                // Sin ninguna vecina conectada no hay de dónde derivar el
                // ángulo — se usa la direccion propia de la celda
                // (editor/catalogo-piezas.md).
                return (Forma.Recta, (int)(celda.Direccion ?? CaraAcceso.NORTE));
            }

            if (conectadas.Length == 1)
            {
                return (Forma.Recta, (int)conectadas[0]);
            }

            if (conectadas.Length == 2)
            {
                bool colineales = GrillaModelo.Opuesta(conectadas[0]) == conectadas[1];
                return colineales
                    ? (Forma.Recta, (int)conectadas[0])
                    : (Forma.Curva, PasosDeRotacion(BaseCurva, conectadas));
            }

            if (conectadas.Length == 3)
            {
                return (Forma.CruceT, PasosDeRotacion(BaseCruceT, conectadas));
            }

            return (Forma.CrucePlus, 0);
        }

        // Cuántos pasos de 90° en sentido horario (mismo signo que el resto
        // del editor, ver PiezaSimpleView.AplicarRotacion) hacen falta para
        // que baseDirecciones coincida con objetivo, sin importar el orden.
        private static int PasosDeRotacion(CaraAcceso[] baseDirecciones, CaraAcceso[] objetivo)
        {
            var objetivoSet = new HashSet<CaraAcceso>(objetivo);

            for (int pasos = 0; pasos < 4; pasos++)
            {
                var rotadas = new HashSet<CaraAcceso>();
                foreach (CaraAcceso direccion in baseDirecciones)
                {
                    rotadas.Add((CaraAcceso)(((int)direccion + pasos) % 4));
                }

                if (rotadas.SetEquals(objetivoSet))
                {
                    return pasos;
                }
            }

            return 0;
        }
    }
}
