using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ApiTest : MonoBehaviour
{
    private const string BaseUrl = "http://localhost:8080/api";

    // Se ejecuta solo una vez al darle Play
    void Start()
    {
        StartCoroutine(ProbarPing());
    }

    private IEnumerator ProbarPing()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(BaseUrl + "/ping"))
        {
            // Pausa acá hasta que la petición termine, sin congelar nada
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Respuesta del backend: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error al conectar: " + request.error);
            }
        }
    }
}