using System;
using System.IO.Ports;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Deployment.Internal;

[Serializable]
public class Payload {
    public string RFID;
    public string JOYSTICK;
    public string POT;
    public string BUTTON;
}

public class ArduinoConnector : MonoBehaviour
{
    public static ArduinoConnector Instance { get; private set; }

    [Header("Serial Settings")]
    public int baudRate = 9600;
    public float scanInterval = 2f;   // tiempo entre intentos de escaneo

    private SerialPort serial;
    private string lastRawLine = "";
    public bool isSearching { get; private set; } = false;

    private const string IDENTIFICATION_MSG = "Museo Digital";
    private const string RESPONSE_MSG = "Te encontre";
    private const string RESET_CONNECTION_MSG = "Reset Connection";

    // ---- Diccionario protegido ----
    private Dictionary<string, string> internalData = new Dictionary<string, string>()
    {
        { "RFID", null },
        { "JOYSTICK", null },
        { "POT", null },
        { "BUTTON", null }
    };

    public IReadOnlyDictionary<string, string> Data => internalData;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(AutoSearchLoop());
    }

    void Update()
    {
        //foreach (string key in internalData.Keys)
        //{
        //    Debug.Log(key + internalData[key]);
        //}
    }

    // ---------------------------------------------------------------
    // AUTOSEARCH LOOP – busca continuamente hasta encontrar Arduino
    // ---------------------------------------------------------------
    private IEnumerator AutoSearchLoop()
    {
        isSearching = true;

        while (true)
        {
            if (serial == null || !serial.IsOpen)
            {
                yield return StartCoroutine(SearchArduinoPort());
            }

            yield return new WaitForSeconds(scanInterval);
        }
    }

    // ---------------------------------------------------------------
    // SCAN ALL COM PORTS
    // ---------------------------------------------------------------
    private IEnumerator SearchArduinoPort()
    {
        Debug.Log("Buscando Arduino...");

        string[] ports = SerialPort.GetPortNames();
        foreach (string port in ports)
        {
            Debug.Log("Probando: " + port);

            SerialPort testPort = new SerialPort(port, baudRate);
            testPort.ReadTimeout = 500;
            testPort.NewLine = "\n";

            try
            {
                testPort.Open();
            }
            catch
            {
                continue; // no se pudo abrir → siguiente puerto
            }

            bool found = false;
            float timer = 0f;

            while (timer < 1.5f) // tiempo para recibir mensaje
            {
                timer += Time.deltaTime;

                try
                {
                    string line = testPort.ReadLine().Trim();
                    if (line == IDENTIFICATION_MSG)
                    {
                        Debug.Log("Arduino encontrado en " + port);
                        found = true;

                        testPort.WriteLine(RESPONSE_MSG);
                        testPort.BaseStream.Flush();

                        serial = testPort; // usamos este puerto como oficial
                        StartCoroutine(ReadLoop());
                        break;
                    }
                }
                catch { }

                yield return null;
            }

            if (!found)
            {
                testPort.Close();
            }
            else
            {
                yield break;
            }
        }

        Debug.LogWarning("Arduino NO encontrado. Reintentando...");
    }

    // ---------------------------------------------------------------
    // READ LOOP – lectura continua del puerto seleccionado
    // ---------------------------------------------------------------
    private IEnumerator ReadLoop()
    {
        Debug.Log("Iniciando lectura desde Arduino...");

        while (serial != null && serial.IsOpen)
        {
            try
            {
                if (serial.BytesToRead > 0)
                {
                    lastRawLine = serial.ReadLine();
                    ProcessRawLine(lastRawLine);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Error de lectura (posible desconexión): " + ex.Message);

                // cerrar puerto y volver a escanear
                TryCloseSerial();
                yield break;
            }

            yield return null;
        }
    }

    // ---------------------------------------------------------------
    // Procesar KEY=VALUE
    // ---------------------------------------------------------------
    private void ProcessRawLine(string rawLine)
    {
        try
        {
            var dict = JsonUtility.FromJson<Payload>(rawLine);
            internalData["RFID"] = dict.RFID;
            internalData["JOYSTICK"] = dict.JOYSTICK;
            internalData["POT"] = dict.POT;
            internalData["BUTTON"] = dict.BUTTON;
        }
        catch
        {
            Debug.LogWarning("Paquete JSON inválido: " + rawLine);
        }
    }

    private void TryCloseSerial()
    {
        if (serial != null)
        {
            try
            {
                // Enviar mensaje antes de cerrar
                serial.WriteLine(RESET_CONNECTION_MSG);
                serial.BaseStream.Flush(); // asegurarse que se envíe
                serial.Close();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Error enviando mensaje en cierre: " + e.Message);
            }
            serial = null;
        }
    }

    private void OnApplicationQuit()
    {
        TryCloseSerial();
    }
}
