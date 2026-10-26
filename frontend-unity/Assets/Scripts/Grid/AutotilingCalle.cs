using System.Collections.Generic;

namespace Sme.Grid
{
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
