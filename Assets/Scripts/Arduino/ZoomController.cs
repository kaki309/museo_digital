using UnityEngine;

public class ZoomController : MonoBehaviour
{
    public Transform target;
    public float minDistance = 2f;
    public float maxDistance = 15f;
    public float smooth = 5f;

    private float currentDistance;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        currentDistance = (minDistance + maxDistance) / 2f;
    }

    void LateUpdate()
    {
        if (ArduinoManager.Instance == null) return;

        float t = 1f - ArduinoManager.Instance.zoomValue;
        float targetDistance = Mathf.Lerp(minDistance, maxDistance, t);
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * smooth);

        cam.transform.position = target.position - cam.transform.forward * currentDistance;
    }
}
