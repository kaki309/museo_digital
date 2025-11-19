using System;
using System.IO.Ports;
using System.Collections.Generic;
using UnityEngine;

public class ArduinoConnectorCopy : MonoBehaviour
{
    public static ArduinoConnectorCopy Instance { get; private set; }

    [Header("Serial Port Settings")]
    public string portName = "COM5";
    public int baudRate = 9600;

    private SerialPort serial;
    private string lastRawLine = "";

    // --- Diccionario protegido ---
    private Dictionary<string, string> internalData = new Dictionary<string, string>()
    {
        { "RFID", null },
        { "JOYSTICK", null },
        { "POT", null },
        { "BUTTON", null }
    };

    public IReadOnlyDictionary<string, string> Data => internalData; // getter público

    private void Awake()
    {
        // ---- SINGLETON ----
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);   // <<< Mantener entre escenas
    }

    private void Start()
    {
        try
        {
            serial = new SerialPort(portName, baudRate);
            serial.NewLine = "\n"; // obliga a terminar línea por \n
            serial.ReadTimeout = 50;
            serial.Open();
            Debug.Log("Serial CONNECTED on " + portName);
        }
        catch (Exception e)
        {
            Debug.LogError("Error opening serial port: " + e);
        }
    }

    private void Update()
    {
        if (serial == null || !serial.IsOpen) return;

        try
        {
            // Leer todas las líneas disponibles
            while (serial.BytesToRead > 0)
            {
                lastRawLine = serial.ReadLine();
                ProcessRawLine(lastRawLine);
            }
        }
        catch (TimeoutException)
        {
            // No pasa nada, solo significa que no había línea completa
        }
    }

    private void ProcessRawLine(string rawLine)
    {
        // --- Limpieza profunda ---
        rawLine = rawLine.Trim();       // quita espacios, \n, \r
        rawLine = rawLine.Trim('\0');   // elimina caracteres nulos

        if (string.IsNullOrWhiteSpace(rawLine))
            return;

        Debug.Log("RAW SERIAL → [" + rawLine + "]");

        // --- Parseo KEY=VALUE ---
        string[] parts = rawLine.Split('=');

        if (parts.Length != 2)
        {
            Debug.LogWarning("Formato inválido recibido: " + rawLine);
            return;
        }

        string key = parts[0].Trim();
        string value = parts[1].Trim();

        // --- verificar si la clave existe en el diccionario ---
        if (!internalData.ContainsKey(key))
        {
            Debug.LogWarning($"Clave '{key}' no existe en el diccionario.");
            return;
        }

        // --- Actualizar valor ---
        internalData[key] = value;
        Debug.Log($"Actualizado: {key} = {value}");
    }

    private void OnApplicationQuit()
    {
        if (serial != null && serial.IsOpen)
            serial.Close();
    }
}
