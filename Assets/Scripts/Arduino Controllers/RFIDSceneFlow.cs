using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class RFIDSceneFlow : MonoBehaviour
{
    [Header("Canvas References")]
    public GameObject canvas1;   // activo por defecto
    public GameObject canvas2;   // se activa cuando encuentra la carpeta
    public GameObject canvas3;   // se activa después de 5 segundos

    [Header("Target Scene")]
    public string sceneToLoad = "NuevaEscena";

    private string lastRFID = null;
    private bool flowStarted = false;

    void Start()
    {
        // Asegura que solo canvas1 está activo
        canvas1.SetActive(true);
        canvas2.SetActive(false);
        canvas3.SetActive(false);
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
            StartCoroutine(RFIDFlow(currentRFID));
            flowStarted = true; // evita que se ejecute más de una vez
        }
        else
        {
            Debug.LogWarning($"NO existe carpeta para RFID: {currentRFID}");
        }
    }

    private IEnumerator RFIDFlow(string rfid)
    {
        // Activar canvas 2
        canvas1.SetActive(false);
        canvas2.SetActive(true);
        yield return new WaitForSeconds(3.5f);

        // Activar canvas 3
        canvas2.SetActive(false);
        canvas3.SetActive(true);
        yield return new WaitForSeconds(2f);

        // Cargar la escena de manera asincrónica pero sin activarla todavía
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneToLoad);
        op.allowSceneActivation = true;

        while (!op.isDone) yield return null;
    }
}
