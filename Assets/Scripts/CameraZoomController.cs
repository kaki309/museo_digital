using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraZoomController : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;             // Modelo a enfocar

    [Header("Zoom Settings")]
    public float zoomSpeed = 2f;         // Velocidad del zoom
    public float minZoomFactor = 0.5f;   // Escala mínima respecto al tamaño del modelo
    public float maxZoomFactor = 3f;     // Escala máxima respecto al tamaño del modelo

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

        // 1️⃣ Calcula el tamaño total del modelo (Bounds combinados)
        modelBounds = CalculateBounds(target.gameObject);

        // 2️⃣ Determina una distancia inicial óptima
        float modelSize = modelBounds.extents.magnitude;
        currentDistance = modelSize * 2.0f; // cámara inicia un poco alejada

        // 3️⃣ Calcula límites de zoom según el tamaño
        minDistance = modelSize * minZoomFactor;
        maxDistance = modelSize * maxZoomFactor;

        // 4️⃣ Posiciona la cámara automáticamente
        Vector3 direction = transform.forward * -1; // detrás del punto de vista
        transform.position = modelBounds.center + direction * currentDistance;
        transform.LookAt(modelBounds.center);
    }

    private void Update()
    {
        HandleZoom();
    }

    private void HandleZoom()
    {
        if (target == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentDistance -= scroll * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

            Vector3 direction = (transform.position - modelBounds.center).normalized;
            transform.position = modelBounds.center + direction * currentDistance;
        }
    }

    // 🔍 Calcula los bounds combinados de todo el modelo (incluso con hijos)
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
