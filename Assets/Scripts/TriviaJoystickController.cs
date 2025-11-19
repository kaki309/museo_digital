using UnityEngine;

/// <summary>
/// Controla la selección de opciones en la trivia usando el joystick físico
/// No modifica el SequenceManager ni el TriviaHandler.
/// Simplemente llama a métodos públicos del TriviaHandler cuando detecta movimiento.
/// </summary>
public class TriviaJoystickController : MonoBehaviour
{
    [Header("Referencia directa (Inspector)")]
    public TriviaHandler triviaHandler;

    [Header("Configuración de Joystick")]
    [Tooltip("Valor mínimo para detectar que el joystick está hacia abajo")]
    public int downThreshold = 300;

    [Tooltip("Valor mínimo para detectar que el joystick está hacia arriba")]
    public int upThreshold = 700;

    [Tooltip("Zona muerta para evitar movimientos accidentales")]
    public int deadzoneMin = 450;
    public int deadzoneMax = 650;

    [Tooltip("Tiempo de espera para evitar lecturas muy rápidas")]
    public float inputCooldown = 0.4f;

    [Header("Opcional: Activar debug en consola")]
    public bool showDebugLogs = false;

    private float lastInputTime = 0f;
    private int lastJoystickValue = 511; // Valor de reposo inicial
    //private TriviaHandler triviaHandler;

    private bool lastButtonPressed = false;  // Para detectar cambio

    void Start()
    {
        triviaHandler = FindObjectOfType<TriviaHandler>();
        if (triviaHandler == null)
        {
            Debug.LogError("⚠ No se encontró un TriviaHandler en la escena.");
        }
    }

    void Update()
    {
        if (triviaHandler == null) return;
        if (!triviaHandler.IsTriviaActive()) return;
        if (ArduinoConnector.Instance == null) return;

        // ---- JOYSTICK ----
        if (ArduinoConnector.Instance.Data.ContainsKey("JOYSTICK"))
        {
            string joyValueString = ArduinoConnector.Instance.Data["JOYSTICK"];
            joyValueString = joyValueString.Replace("JOYSTICK", "");
            string[] parts = joyValueString.Split('-');

            if (parts.Length == 2 && int.TryParse(parts[1], out int joyValue))
            {
                DetectJoystickMovement(joyValue);
                lastJoystickValue = joyValue;
            }
        }

        // ---- BOTÓN ----
        if (ArduinoConnector.Instance.Data.ContainsKey("BUTTON"))
        {
            string buttonValue = ArduinoConnector.Instance.Data["BUTTON"]; // "S" o "P"
            bool isPressed = buttonValue == "P";  // ahora sí

            if (showDebugLogs)
                Debug.Log("Estado botón: " + buttonValue);

            // Detectar flanco: cuando pasa de NO presionado -> presionado
            if (isPressed && !lastButtonPressed)
            {
                triviaHandler.SubmitAnswer();
                Debug.Log("Respuesta confirmada");
            }

            lastButtonPressed = isPressed;
        }
    }

    void DetectJoystickMovement(int value)
    {
        if (Time.time - lastInputTime < inputCooldown) return;

        if (showDebugLogs)
            Debug.Log("Detectado valor joystick: " + value);

        // ARRIBA
        if (value > upThreshold && lastJoystickValue <= deadzoneMax)
        {
            if (showDebugLogs) Debug.Log("↑↑↑ ARRIBA");
            triviaHandler.SelectPreviousAnswer();
            lastInputTime = Time.time;
        }
        // ABAJO
        else if (value < downThreshold && lastJoystickValue >= deadzoneMin)
        {
            if (showDebugLogs) Debug.Log("↓↓↓ ABAJO");
            triviaHandler.SelectNextAnswer();
            lastInputTime = Time.time;
        }
    }

    // BONUS (opcional): confirmar respuesta con botón físico
    void TryConfirmAnswer()
    {
        string buttonData = ArduinoConnector.Instance.Data["BUTTON"];
        if (buttonData == "P")  // Puedes ajustar el valor según Arduino
        {
            triviaHandler.SubmitAnswer();
        }
    }
}
