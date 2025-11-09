using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ModelMovementTracker : MonoBehaviour
{
    // Static flag para rastrear si el split ya ocurrió (persiste entre cargas de escena)
    private static bool hasSplitOccurred = false;

    private Transform modelTransform; // El transform del modelo 3D que se está controlando

    [Header("Movement Threshold")]
    [Tooltip("Distancia acumulada de movimiento (rotación + zoom) necesaria para activar el split")]
    public float movementThreshold = 1000f;

    [Header("Split Settings")]
    [Tooltip("Tiempo en segundos antes de cambiar de escena después del split")]
    public float timeBeforeSceneChange = 2f;
    [Tooltip("Nombre de la escena a la que cambiar después del split")]
    public string nextSceneName = "interfaz";

    [Header("Debug")]
    public bool showDebugInfo = true;

    // Referencias para tracking
    private Quaternion lastRotation;
    private Vector3 lastPosition;
    private float accumulatedMovement = 0f;
    private bool hasSplit = false;
    private bool isTracking = false;

    private void Awake(){
        modelTransform = this.transform;
    }

    private void Start()
    {   
        // Si el split ya ocurrió, desactivar el tracker completamente
        if (hasSplitOccurred)
        {
            isTracking = false;
            if (showDebugInfo)
            {
                Debug.Log("ModelMovementTracker: El split ya ocurrió anteriormente. Tracker desactivado.");
            }
            return;
        }

        // Inicializar tracking solo si el split no ha ocurrido
        lastRotation = modelTransform.rotation;
        lastPosition = modelTransform.position;

        isTracking = true;

        if (showDebugInfo)
        {
            Debug.Log($"ModelMovementTracker: Iniciado. Threshold: {movementThreshold}");
        }
    }

    private void Update()
    {
        if (!isTracking || modelTransform == null || hasSplit) return;

        // Calcular movimiento acumulado
        float movementThisFrame = CalculateMovement();
        accumulatedMovement += movementThisFrame;

        if (showDebugInfo && Time.frameCount % 60 == 0) // Cada segundo aproximadamente
        {
            Debug.Log($"Movimiento acumulado: {accumulatedMovement:F2} / {movementThreshold:F2}");
        }

        // Verificar si se alcanzó el umbral
        if (accumulatedMovement >= movementThreshold)
        {
            StartCoroutine(SplitAndChangeScene());
        }
    }

    private float CalculateMovement()
    {
        float totalMovement = 0f;

        // 1. Calcular movimiento por rotación
        Quaternion currentRotation = modelTransform.rotation;
        float rotationDelta = Quaternion.Angle(lastRotation, currentRotation);
        totalMovement += rotationDelta * 5f; // Multiplicador para dar más peso a la rotación
        lastRotation = currentRotation;

        return totalMovement;
    }

    private IEnumerator SplitAndChangeScene()
    {
        if (hasSplit || hasSplitOccurred) yield break;
        
        hasSplit = true;
        hasSplitOccurred = true; // Marcar que el split ya ocurrió (persiste entre escenas)
        isTracking = false;

        if (showDebugInfo)
        {
            Debug.Log($"¡Umbral alcanzado! Movimiento acumulado: {accumulatedMovement:F2}. Iniciando split...");
        }

        // Desactivar el movimiento del modelo
        MouseRotationController mouseController = modelTransform.GetComponent<MouseRotationController>();
        JoystickRotationController joystickController = modelTransform.GetComponent<JoystickRotationController>();
        
        if (mouseController != null)
        {
            mouseController.enabled = false;
        }
        if (joystickController != null)
        {
            joystickController.enabled = false;
        }

        // Crear el efecto de split
        SplitModel();

        // Esperar antes de cambiar de escena
        yield return new WaitForSeconds(timeBeforeSceneChange);

        // Cambiar de escena
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (showDebugInfo)
            {
                Debug.Log($"Cambiando a la escena: {nextSceneName}");
            }
            
            try
            {
                SceneManager.LoadScene(nextSceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error al cargar la escena '{nextSceneName}': {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("ModelMovementTracker: No se especificó el nombre de la siguiente escena.");
        }
    }

    private void SplitModel()
    {
        RuntimeAutoFracture autoFractureScript = modelTransform.gameObject.GetComponent<RuntimeAutoFracture>();
        if (autoFractureScript != null)
        {
            autoFractureScript.BreakModel();
        } else {
            Debug.LogError("ModelMovementTracker: No se encontró el componente RuntimeAutoFracture en el modelo.");
        }
    }

    // Método público para resetear el tracking (útil para testing)
    public void ResetTracking()
    {
        accumulatedMovement = 0f;
        hasSplit = false;
        hasSplitOccurred = false; // Resetear el flag estático también
        isTracking = true;
        
        if (modelTransform != null)
        {
            lastRotation = modelTransform.rotation;
            lastPosition = modelTransform.position;
        }
    }

    // Método estático para resetear el flag de split (útil para testing o reiniciar la aplicación)
    public static void ResetSplitFlag()
    {
        hasSplitOccurred = false;
    }

    // Método público para obtener el movimiento acumulado
    public float GetAccumulatedMovement()
    {
        return accumulatedMovement;
    }

    // Método público para establecer el umbral manualmente
    public void SetThreshold(float newThreshold)
    {
        movementThreshold = newThreshold;
    }
}

