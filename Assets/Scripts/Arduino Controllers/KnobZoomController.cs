using UnityEngine;

[RequireComponent(typeof(Camera))]
public class KnobZoomController : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;             // Modelo a enfocar

    [Header("Zoom Settings")]
    public float minZoomFactor = 0.5f;   // Escala mínima respecto al tamaño del modelo
    public float maxZoomFactor = 3f;     // Escala máxima respecto al tamaño del modelo
    public float zoomSmoothness = 10f;   // Suavizado del movimiento (opcional)

    private Camera cam;
    private float currentDistance;
    private float minDistance;
    private float maxDistance;

    private Bounds modelBounds;

    private void Start()
    {
        cam = GetComponent<Camera>();

        if (target == null)
        {
            Debug.LogWarning("CameraZoomController: No se asignó un target.");
            return;
        }

        // 1️⃣ Bounds del modelo
        modelBounds = CalculateBounds(target.gameObject);

        // 2️⃣ Tamaño base
        float modelSize = modelBounds.extents.magnitude;

        // 3️⃣ Distancias mínima y máxima según el tamaño del modelo
        minDistance = modelSize * minZoomFactor;
        maxDistance = modelSize * maxZoomFactor;

        // 4️⃣ Distancia inicial (al medio)
        currentDistance = (minDistance + maxDistance) / 2f;

        // 5️⃣ Posicionar cámara
        Vector3 direction = transform.forward * -1;
        transform.position = modelBounds.center + direction * currentDistance;
        transform.LookAt(modelBounds.center);
    }

    private void Update()
    {
        HandleZoomFromPot();
    }

    private void HandleZoomFromPot()
    {
        if (target == null) return;

        // 1️⃣ Leer valor del Arduino (0–1023)
        string potRaw = ArduinoConnector.Instance.Data["POT"];
        if (string.IsNullOrEmpty(potRaw)) return;

        if (!int.TryParse(potRaw, out int potValue)) return;

        potValue = Mathf.Clamp(potValue, 0, 1023);

        // 2️⃣ Convertir rango 0–1023 → minDistance–maxDistance
        float normalized = potValue / 1023f; // 0 a 1
        float targetDistance = Mathf.Lerp(minDistance, maxDistance, normalized);

        // 3️⃣ Suavizado (opcional)
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * zoomSmoothness);

        // 4️⃣ Mover cámara hacia/desde el modelo
        Vector3 direction = (transform.position - modelBounds.center).normalized;
        transform.position = modelBounds.center + direction * currentDistance;
    }

    // Bounds combinados del target
    private Bounds CalculateBounds(GameObject obj)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(obj.transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer rend in renderers)
            bounds.Encapsulate(rend.bounds);

        return bounds;
    }
}
