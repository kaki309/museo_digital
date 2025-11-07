using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class ArduinoManager : MonoBehaviour
{
    public static ArduinoManager Instance; // Singleton de acceso global

    [Header("Serial Settings")]
    public string portName = "COM3";
    public int baudRate = 115200;

    // Datos procesados accesibles por otros scripts
    [HideInInspector] public Vector2 joystickInput = Vector2.zero;
    [HideInInspector] public float zoomValue = 0.5f;
    [HideInInspector] public string lastRFID = "";

    private SerialPort stream;
    private Thread readThread;
    private bool isRunning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        try
        {
            stream = new SerialPort(portName, baudRate);
            stream.Open();
            stream.ReadTimeout = 50;

            isRunning = true;
            readThread = new Thread(ReadSerial);
            readThread.Start();
        }
        catch (System.Exception e)
        {
            Debug.LogError("No se pudo abrir el puerto serial: " + e.Message);
        }
    }

    void ReadSerial()
    {
        while (isRunning)
        {
            try
            {
                string line = stream.ReadLine().Trim();
                ParseLine(line);
            }
            catch { }
        }
    }

    void ParseLine(string line)
    {
        if (line.StartsWith("JOY"))
        {
            string[] parts = line.Split(',');
            if (parts.Length >= 3)
            {
                float.TryParse(parts[1], out joystickInput.x);
                float.TryParse(parts[2], out joystickInput.y);
            }
        }
        else if (line.StartsWith("ZOOM"))
        {
            string[] parts = line.Split(',');
            if (parts.Length >= 2)
            {
                int val;
                if (int.TryParse(parts[1], out val))
                    zoomValue = Mathf.InverseLerp(0, 1023, val);
            }
        }
        else if (line.StartsWith("RFID"))
        {
            string[] parts = line.Split(',');
            if (parts.Length >= 2)
                lastRFID = parts[1];
        }
    }

    private void OnApplicationQuit()
    {
        isRunning = false;
        if (readThread != null && readThread.IsAlive)
            readThread.Join();
        if (stream != null && stream.IsOpen)
            stream.Close();
    }
}
