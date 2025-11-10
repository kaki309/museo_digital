using System.Collections;
using Unity.VisualScripting;
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
    
    [Header("Sequence Settings")]
    public GameObject canvas;
    [Tooltip("SequenceManager component to use for sequences. If null, will search in scene.")]
    public SequenceManager sequenceManager;
    [Tooltip("Name of the sequence file to play at the beginning (only if split hasn't occurred yet, without .txt extension)")]
    public string beginningSequenceFileName = "welcomeSecuence";
    [Tooltip("Name of the sequence file to load when split occurs (without .txt extension)")]
    public string splitSequenceFileName = "onModelSplit";
    [Tooltip("Scene name to load after the split sequence completes")]
    public string sequenceCompleteSceneName = "interfaz";

    [Header("Debug")]
    public bool showDebugInfo = true;

    // Referencias para tracking
    private Quaternion lastRotation;
    private Vector3 lastPosition;
    private float accumulatedMovement = 0f;
    private bool hasSplit = false;
    private bool isTracking = false;
    private bool isBeginningSequencePlaying = false;

    private void Awake(){
        modelTransform = this.transform;
    }

    private void Start()
    {   
        // Buscar SequenceManager si no está asignado
        if (sequenceManager == null)
        {
            sequenceManager = FindObjectOfType<SequenceManager>();
            if (sequenceManager == null && showDebugInfo)
            {
                Debug.LogWarning("ModelMovementTracker: No SequenceManager found in scene. Sequence functionality will not work.");
            }
        }

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

        // Iniciar la secuencia de bienvenida si el split no ha ocurrido
        if (!hasSplitOccurred && sequenceManager != null && !string.IsNullOrEmpty(beginningSequenceFileName))
        {
            StartBeginningSequence();
        }
    }

    private void Update()
    {
        // No rastrear si la secuencia de bienvenida está reproduciéndose
        if (isBeginningSequencePlaying) return;
        
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

        // Iniciar la secuencia si hay un SequenceManager disponible
        if (sequenceManager != null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"Iniciando secuencia de split: {splitSequenceFileName}");
            }
            
            // Asegurarse de que la secuencia de bienvenida no esté reproduciéndose
            isBeginningSequencePlaying = false;
            
            // Desuscribirse del evento de la secuencia de bienvenida si está suscrito
            sequenceManager.OnSequenceComplete -= OnBeginningSequenceCompleted;
            
            // Detener cualquier secuencia que esté reproduciéndose
            sequenceManager.StopSequence();
            
            // Configurar la secuencia de split
            sequenceManager.sequenceFileName = splitSequenceFileName;
            sequenceManager.nextSceneName = ""; // Desactivar el cambio de escena automático del finish sequence
            
            // Cargar la secuencia
            sequenceManager.LoadSequence();
            
            // Suscribirse al evento de completado de secuencia de split
            sequenceManager.OnSequenceComplete += OnSequenceCompleted;
            
            // Iniciar la secuencia
            sequenceManager.StartSequence();
            
            // Esperar a que la secuencia termine (el evento OnSequenceCompleted cambiará la escena)
            yield break; // Salir aquí, el evento manejará el cambio de escena
        }
        else
        {
            // Si no hay SequenceManager, usar el comportamiento anterior
            if (showDebugInfo)
            {
                Debug.LogWarning("ModelMovementTracker: No SequenceManager disponible. Usando cambio de escena directo.");
            }
            
            // Esperar antes de cambiar de escena
            yield return new WaitForSeconds(timeBeforeSceneChange);

            // Cambiar de escena
            ChangeScene(nextSceneName);
        }
    }
    
    /// <summary>
    /// Starts the beginning sequence (welcome sequence)
    /// </summary>
    private void StartBeginningSequence()
    {
        canvas.SetActive(true);
        if (sequenceManager == null || string.IsNullOrEmpty(beginningSequenceFileName))
        {
            return;
        }

        if (showDebugInfo)
        {
            Debug.Log($"Iniciando secuencia de bienvenida: {beginningSequenceFileName}");
        }

        isBeginningSequencePlaying = true;

        // Configurar la secuencia de bienvenida
        sequenceManager.sequenceFileName = beginningSequenceFileName;
        sequenceManager.nextSceneName = ""; // No cambiar de escena después de la secuencia de bienvenida
        sequenceManager.loopSequence = false; // No repetir la secuencia de bienvenida

        // Cargar la secuencia
        sequenceManager.LoadSequence();

        // Suscribirse al evento de completado de secuencia
        sequenceManager.OnSequenceComplete += OnBeginningSequenceCompleted;

        // Iniciar la secuencia
        sequenceManager.StartSequence();
    }

    /// <summary>
    /// Called when the beginning sequence completes
    /// </summary>
    private void OnBeginningSequenceCompleted()
    {
        if (sequenceManager != null)
        {
            // Desuscribirse del evento
            sequenceManager.OnSequenceComplete -= OnBeginningSequenceCompleted;
        }

        isBeginningSequencePlaying = false;

        if (showDebugInfo)
        {
            Debug.Log("Secuencia de bienvenida completada. Iniciando tracking de movimiento.");
        }
    }

    /// <summary>
    /// Called when the split sequence completes
    /// </summary>
    private void OnSequenceCompleted()
    {
        if (sequenceManager != null)
        {
            // Desuscribirse del evento
            sequenceManager.OnSequenceComplete -= OnSequenceCompleted;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Secuencia de split completada. Cambiando a la escena: {sequenceCompleteSceneName}");
        }
        
        // Cambiar de escena
        ChangeScene(sequenceCompleteSceneName);
    }
    
    /// <summary>
    /// Helper method to change scene
    /// </summary>
    private void ChangeScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            if (showDebugInfo)
            {
                Debug.Log($"Cambiando a la escena: {sceneName}");
            }
            
            try
            {
                SceneManager.LoadScene(sceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error al cargar la escena '{sceneName}': {e.Message}");
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

