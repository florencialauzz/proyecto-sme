using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sme.Managers
{
    public class NavegadorEscenas : MonoBehaviour
    {
        public void IrAAuth()
        {
            SceneManager.LoadScene("Auth");
        }

        public void IrAInicio()
        {
            SceneManager.LoadScene("Inicio");
        }

        public void IrAEditor()
        {
            SceneManager.LoadScene("Editor");
        }

        public void IrAConfiguracion()
        {
            SceneManager.LoadScene("Configuracion");
        }

        public void IrAResultados()
        {
            SceneManager.LoadScene("Resultados");
        }
    }
}
