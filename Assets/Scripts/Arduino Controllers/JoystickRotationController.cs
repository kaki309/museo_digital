using UnityEngine;

public class JoystickRotationController : MonoBehaviour
{
    [Header("Joystick Settings")]
    public float rotationSpeed = 100f;
    public bool invertX = false;
    public bool invertY = true;

    // Deadzone evita movimientos involuntarios
    public float deadZone = 0.1f;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // 1. Leer la cadena del Arduino: "512-512"
        string joyRaw = ArduinoConnector.Instance.Data["JOYSTICK"];

        if (string.IsNullOrEmpty(joyRaw))
            return; // aún no hay datos

        // 2. Dividir en X e Y
        string[] parts = joyRaw.Split('-');
        if (parts.Length != 2)
            return; // formato inválido

        if (!int.TryParse(parts[0], out int rawX)) return;
        if (!int.TryParse(parts[1], out int rawY)) return;

        // 3. Convertir 0–1023 → -1 a +1 con centro en 512
        float normX = (rawX - 512f) / 512f;
        float normY = (rawY - 512f) / 512f;

        // Limitar a rango [-1,1]
        normX = Mathf.Clamp(normX, -1f, 1f);
        normY = Mathf.Clamp(normY, -1f, 1f);

        // 4. Aplicar deadzone (evitar vibraciones del joystick)
        if (Mathf.Abs(normX) < deadZone) normX = 0;
        if (Mathf.Abs(normY) < deadZone) normY = 0;

        // 5. Ajustar inversión opcional
        if (invertX) normX = -normX;
        if (invertY) normY = -normY;

        // 6. Aplicar sensibilidad
        float mx = normX * rotationSpeed * Time.deltaTime;
        float my = normY * rotationSpeed * Time.deltaTime;

        // 7. Ejes de la cámara
        Vector3 camUp = mainCamera.transform.up;
        Vector3 camRight = mainCamera.transform.right;

        // 8. Crear rotaciones
        Quaternion rotHorizontal = Quaternion.AngleAxis(mx, camUp);
        Quaternion rotVertical = Quaternion.AngleAxis(-my, camRight);

        // 9. Aplicar al objeto
        transform.rotation = rotHorizontal * rotVertical * transform.rotation;
    }
}
