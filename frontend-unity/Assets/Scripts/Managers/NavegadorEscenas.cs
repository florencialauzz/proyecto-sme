using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sme.Managers
{
    // Wrapper fino sobre SceneManager: un UnityEvent del Inspector no puede
    // apuntar a un método estático, así que este componente le da algo a lo
    // que engancharse. Poner una instancia (GameObject "Navegador") en cada
    // escena y cablear los botones/eventos de cambio de escena acá.
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
    }
}
