using UnityEngine;

public class JoystickController : MonoBehaviour
{
    public float rotationSpeed = 100f;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (ArduinoManager.Instance == null) return;

        Vector2 joy = ArduinoManager.Instance.joystickInput;
        if (joy.sqrMagnitude < 0.01f) return; // evita ruido

        Vector3 camUp = mainCamera.transform.up;
        Vector3 camRight = mainCamera.transform.right;

        Quaternion rotH = Quaternion.AngleAxis(joy.x * rotationSpeed * Time.deltaTime, camUp);
        Quaternion rotV = Quaternion.AngleAxis(-joy.y * rotationSpeed * Time.deltaTime, camRight);

        transform.rotation = rotH * rotV * transform.rotation;
    }
}
