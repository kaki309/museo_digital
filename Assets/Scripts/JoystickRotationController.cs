// using UnityEngine;

// public class JoystickRotationController : MonoBehaviour
// {
//     [Header("Rotation Settings")]
//     public float rotationSpeed = 100f; // Sensibilidad
//     public bool invertY = true;
//     public bool invertX = false;

//     private float yaw = 0f;
//     private float pitch = 0f;

//     void Start()
//     {
//         Vector3 e = transform.eulerAngles;
//         yaw = e.y;
//         pitch = e.x;
//     }

//     void Update()
//     {
//         // Leer ejes del joystick (usa ejes del mando)
//         float mx = Input.GetAxis("Horizontal"); // Stick derecho horizontal
//         float my = Input.GetAxis("Vertical");   // Stick derecho vertical

//         // Si quieres usar el stick derecho, cambia a:
//         // float mx = Input.GetAxis("RightStickHorizontal");
//         // float my = Input.GetAxis("RightStickVertical");

//         // Aplica inversión
//         if (invertX) mx = -mx;
//         if (invertY) my = -my;

//         // Ajusta sensibilidad
//         mx *= rotationSpeed * Time.deltaTime;
//         my *= rotationSpeed * Time.deltaTime;

//         // Actualiza rotaciones
//         yaw += mx;
//         pitch += my;

//         // 🔒 Evita inclinación lateral (roll)
//         Quaternion qYaw = Quaternion.AngleAxis(yaw, Vector3.up);
//         Vector3 rightAfterYaw = qYaw * Vector3.right;
//         Quaternion qPitch = Quaternion.AngleAxis(pitch, rightAfterYaw);

//         transform.rotation = qPitch * qYaw;
//     }
// }

using UnityEngine;

public class JoystickRotationController : MonoBehaviour
{
    public float rotationSpeed = 100f;
    public bool invertY = true;
    public bool invertX = false;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // Leer entrada del joystick
        float mx = Input.GetAxis("Horizontal"); // o el eje asignado a tu stick
        float my = Input.GetAxis("Vertical");

        if (invertX) mx = -mx;
        if (invertY) my = -my;

        // Multiplicar por sensibilidad
        mx *= rotationSpeed * Time.deltaTime;
        my *= rotationSpeed * Time.deltaTime;

        // Obtener ejes de la cámara
        Vector3 camUp = mainCamera.transform.up;
        Vector3 camRight = mainCamera.transform.right;

        // Crear rotaciones relativas a la cámara
        Quaternion rotHorizontal = Quaternion.AngleAxis(mx, camUp);
        Quaternion rotVertical = Quaternion.AngleAxis(-my, camRight);

        // Aplicar combinación de ambas
        transform.rotation = rotHorizontal * rotVertical * transform.rotation;
    }
}
