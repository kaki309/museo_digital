using UnityEngine;

public class RFIDHandler : MonoBehaviour
{
    private string lastTag = "";

    void Update()
    {
        if (ArduinoManager.Instance == null) return;

        string tag = ArduinoManager.Instance.lastRFID;
        if (!string.IsNullOrEmpty(tag) && tag != lastTag)
        {
            lastTag = tag;
            OnTagScanned(tag);
        }
    }

    void OnTagScanned(string tag)
    {
        Debug.Log("RFID detectado: " + tag);

        // Aquí defines la acción según la etiqueta
        switch (tag)
        {
            case "12345678":
                Debug.Log("Mostrar modelo 1");
                break;
            case "87654321":
                Debug.Log("Mostrar modelo 2");
                break;
            default:
                Debug.Log("Etiqueta no reconocida");
                break;
        }
    }
}
