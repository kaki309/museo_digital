using UnityEngine;

public class MouseRotationController : MonoBehaviour
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
        // Solo rotar cuando se mantiene presionado el botón derecho del mouse
        if (Input.GetMouseButton(1))
        {
            // Leer entrada del mouse
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");

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
}

