using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

public class RFIDSceneFlow : MonoBehaviour
{
    [Header("Canvas References")]
    public Canvas canvas1;   // activo por defecto
    public Canvas canvas2;   // se activa cuando encuentra la carpeta
    public Canvas canvas3;   // se activa después de 5 segundos

    [Header("Target Scene")]
    public string sceneToLoad = "NuevaEscena";

    private string lastRFID = null;
    private bool flowStarted = false;

    void Start()
    {
        // Asegura que solo canvas1 está activo
        canvas1.gameObject.SetActive(true);
        canvas2.gameObject.SetActive(false);
        canvas3.gameObject.SetActive(false);
    }

    void Update()
    {
        if (flowStarted) 
            return;

        var data = ArduinoConnector.Instance.Data;

        if (!data.ContainsKey("RFID"))
            return;

        string currentRFID = data["RFID"];

        if (string.IsNullOrEmpty(currentRFID))
            return;

        if (currentRFID == lastRFID)
            return;

        lastRFID = currentRFID;

        // Revisar si existe la carpeta asociada
        string folderPath = Path.Combine(Application.streamingAssetsPath, currentRFID);

        if (Directory.Exists(folderPath))
        {
            Debug.Log($"Carpeta encontrada para RFID: {currentRFID}");
            StartCoroutine(RFIDFlow());
            flowStarted = true; // evita que se ejecute más de una vez
        }
        else
        {
            Debug.LogWarning($"NO existe carpeta para RFID: {currentRFID}");
        }
    }

    private System.Collections.IEnumerator RFIDFlow()
    {
        // Activar canvas 2
        canvas1.gameObject.SetActive(false);
        canvas2.gameObject.SetActive(true);

        // Esperar 3.5 segundos
        yield return new WaitForSeconds(3.5f);

        // Activar canvas 3
        canvas2.gameObject.SetActive(false);
        canvas3.gameObject.SetActive(true);

        // Esperar 2 segundos
        yield return new WaitForSeconds(2f);

        // Iniciar carga asincrónica de la escena
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneToLoad);

        // Esperar hasta que termine
        while (!op.isDone)
        {
            yield return null;
        }
    }
}
