using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EzySlice;

public class RuntimeAutoFracture : MonoBehaviour
{

    [Header("Configuración de fractura")]
    [Tooltip("Material para las secciones de corte (opcional)")]
    public Material crossSectionMaterial;

    [Header("Fuerza de separación")]
    [Tooltip("Fuerza con la que se separan las partes")]
    public float separationForce = 5f;

    [Header("Auto-destrucción de fragmentos")]
    public bool destroyAfter = true;
    public float destroyDelay = 5f;

    [Header("Pre-procesamiento")]
    [Tooltip("Pre-procesar los cortes al inicio para evitar congelamiento")]
    public bool preprocessSlices = true;

    private bool hasBroken = false;
    private Transform modelChild;
    private List<GameObject> preSlicedParts = new List<GameObject>();
    private bool slicesPrepared = false;
    private Vector3[] separationDirections = new Vector3[3];

    void Start()
    {

        // Asume que el modelo es el primer hijo del contenedor
        if (transform.childCount > 0)
            modelChild = transform.GetChild(0);
        else
            Debug.LogWarning($"{name} no tiene un modelo hijo asignado para fracturar.");

        // Pre-procesar los cortes si está habilitado
        if (preprocessSlices && modelChild != null)
        {
            StartCoroutine(PreprocessSlices());
        }
    }

    public void BreakModel()
    {
        if (hasBroken || modelChild == null) return;
        hasBroken = true;

        GameObject target = modelChild.gameObject;
        Vector3 originalPosition = target.transform.position;
        Quaternion originalRotation = target.transform.rotation;

        // Ocultar el modelo original
        target.SetActive(false);

        // Si los cortes están pre-procesados, usarlos instantáneamente
        if (slicesPrepared && preSlicedParts.Count >= 3)
        {
            InstantiatePreSlicedParts(originalPosition, originalRotation);
        }
        else
        {
            // Si no están pre-procesados, crear en tiempo real (más lento)
            CreateThreeParts(target, originalPosition, originalRotation);
        }
    }

    IEnumerator PreprocessSlices()
    {
        // Esperar un frame para asegurar que todo esté inicializado
        yield return null;

        GameObject target = modelChild.gameObject;
        Vector3 originalPosition = target.transform.position;
        Quaternion originalRotation = target.transform.rotation;

        // Crear un contenedor oculto para las partes pre-procesadas
        GameObject preprocessContainer = new GameObject("PreSlicedPartsContainer");
        preprocessContainer.transform.SetParent(transform);
        preprocessContainer.SetActive(false); // Oculto pero no destruido

        // Crear una copia para pre-procesar (no afecta el original)
        GameObject processingCopy = Instantiate(target, originalPosition, originalRotation);
        processingCopy.SetActive(true);

        // Pre-calcular los cortes
        CreateThreePartsForPreprocessing(processingCopy, originalPosition, originalRotation, preprocessContainer);

        // Limpiar la copia de procesamiento
        Destroy(processingCopy);

        slicesPrepared = true;
    }

    void InstantiatePreSlicedParts(Vector3 position, Quaternion rotation)
    {
        // Instanciar las partes pre-cortadas instantáneamente
        for (int i = 0; i < Mathf.Min(preSlicedParts.Count, 3); i++)
        {
            GameObject instantiatedPart = Instantiate(preSlicedParts[i], position, rotation);
            instantiatedPart.SetActive(true);
            SetupFragment(instantiatedPart, separationDirections[i], i);
        }
    }

    void CreateThreePartsForPreprocessing(GameObject originalModel, Vector3 position, Quaternion rotation, GameObject container)
    {
        // Obtener los bounds del modelo para calcular planos de corte estratégicos
        Bounds bounds = GetModelBounds(originalModel);
        Vector3 center = bounds.center;
        Vector3 size = bounds.size;

        // Estrategia para crear 3 partes de tamaño similar:
        // Primer corte: a 1/3 de la altura (divide en top 33% y bottom 67%)
        Vector3 firstCutPosition = bounds.min + Vector3.up * (size.y / 3f);
        Vector3 firstCutNormal = Vector3.up;

        SlicedHull firstSlice = originalModel.Slice(firstCutPosition, firstCutNormal, crossSectionMaterial);

        if (firstSlice != null && firstSlice.upperHull != null && firstSlice.lowerHull != null)
        {
            GameObject topPart = firstSlice.CreateUpperHull(originalModel, crossSectionMaterial);
            GameObject bottomPart = firstSlice.CreateLowerHull(originalModel, crossSectionMaterial);

            if (topPart != null && bottomPart != null)
            {
                // Segundo corte: en la parte inferior (67%) a la mitad de su altura
                // Esto crea: top 33%, middle 33%, bottom 33%
                Bounds bottomBounds = GetModelBounds(bottomPart);
                Vector3 secondCutPosition = bottomBounds.min + Vector3.up * (bottomBounds.size.y / 2f);
                Vector3 secondCutNormal = Vector3.up;

                SlicedHull secondSlice = bottomPart.Slice(secondCutPosition, secondCutNormal, crossSectionMaterial);

                if (secondSlice != null && secondSlice.upperHull != null && secondSlice.lowerHull != null)
                {
                    GameObject middlePart = secondSlice.CreateUpperHull(bottomPart, crossSectionMaterial);
                    GameObject bottomPartFinal = secondSlice.CreateLowerHull(bottomPart, crossSectionMaterial);

                    // Guardar las partes pre-cortadas (inactivas) en el contenedor
                    topPart.transform.SetParent(container.transform);
                    middlePart.transform.SetParent(container.transform);
                    bottomPartFinal.transform.SetParent(container.transform);
                    topPart.SetActive(false);
                    middlePart.SetActive(false);
                    bottomPartFinal.SetActive(false);

                    preSlicedParts.Add(topPart);
                    preSlicedParts.Add(middlePart);
                    preSlicedParts.Add(bottomPartFinal);

                    // Pre-calcular direcciones de separación (solo X e Y, sin Z)
                    separationDirections[0] = new Vector3(-0.5f, 0.3f, 0f).normalized;
                    separationDirections[1] = new Vector3(0f, 0.3f, 0f).normalized;
                    separationDirections[2] = new Vector3(0.5f, -0.3f, 0f).normalized;

                    Destroy(bottomPart);
                    return;
                }
                else
                {
                    // Fallback: usar solo 2 partes
                    topPart.transform.SetParent(container.transform);
                    bottomPart.transform.SetParent(container.transform);
                    topPart.SetActive(false);
                    bottomPart.SetActive(false);
                    preSlicedParts.Add(topPart);
                    preSlicedParts.Add(bottomPart);
                    // Fallback: solo 2 partes (solo X e Y)
                    separationDirections[0] = new Vector3(-0.5f, 0.3f, 0f).normalized;
                    separationDirections[1] = new Vector3(0.5f, -0.3f, 0f).normalized;
                }
            }
        }

        // Si falla, intentar método alternativo
        if (preSlicedParts.Count == 0)
        {
            CreateThreePartsAlternativeForPreprocessing(originalModel, position, rotation, container);
        }
    }

    void CreateThreePartsAlternativeForPreprocessing(GameObject originalModel, Vector3 position, Quaternion rotation, GameObject container)
    {
        Bounds bounds = GetModelBounds(originalModel);
        Vector3 size = bounds.size;

        // Método alternativo: cortes horizontales para partes más iguales
        // Primer corte a 1/3 de la altura
        Vector3 firstCutPosition = bounds.min + Vector3.up * (size.y / 3f);
        Vector3 firstCutNormal = Vector3.up;

        GameObject currentTarget = originalModel;
        List<GameObject> parts = new List<GameObject>();

        SlicedHull firstSlice = currentTarget.Slice(firstCutPosition, firstCutNormal, crossSectionMaterial);
        if (firstSlice != null && firstSlice.upperHull != null && firstSlice.lowerHull != null)
        {
            GameObject part1 = firstSlice.CreateUpperHull(currentTarget, crossSectionMaterial);
            GameObject part2 = firstSlice.CreateLowerHull(currentTarget, crossSectionMaterial);

            if (part1 != null) parts.Add(part1);
            if (part2 != null)
            {
                // Segundo corte en la parte inferior a la mitad de su altura
                Bounds part2Bounds = GetModelBounds(part2);
                Vector3 secondCutPosition = part2Bounds.min + Vector3.up * (part2Bounds.size.y / 2f);
                SlicedHull secondSlice = part2.Slice(secondCutPosition, firstCutNormal, crossSectionMaterial);
                if (secondSlice != null && secondSlice.upperHull != null && secondSlice.lowerHull != null)
                {
                    GameObject part2a = secondSlice.CreateUpperHull(part2, crossSectionMaterial);
                    GameObject part2b = secondSlice.CreateLowerHull(part2, crossSectionMaterial);
                    if (part2a != null) parts.Add(part2a);
                    if (part2b != null) parts.Add(part2b);
                    Destroy(part2);
                }
                else
                {
                    parts.Add(part2);
                }
            }
        }

        // Guardar las partes pre-cortadas en el contenedor
        int partCount = Mathf.Min(parts.Count, 3);
        for (int i = 0; i < partCount; i++)
        {
            parts[i].transform.SetParent(container.transform);
            parts[i].SetActive(false);
            preSlicedParts.Add(parts[i]);
            // Direcciones de separación distribuidas uniformemente (solo X e Y)
            float angle = (i * 360f / partCount) * Mathf.Deg2Rad;
            separationDirections[i] = new Vector3(Mathf.Cos(angle), 0.2f, 0f).normalized;
        }
    }

    void CreateThreeParts(GameObject originalModel, Vector3 position, Quaternion rotation)
    {
        // Crear una copia activa para cortar (el original está inactivo)
        GameObject activeCopy = Instantiate(originalModel, position, rotation);
        activeCopy.SetActive(true);

        // Obtener los bounds del modelo para calcular planos de corte estratégicos
        Bounds bounds = GetModelBounds(activeCopy);
        Vector3 center = bounds.center;
        Vector3 size = bounds.size;

        // Estrategia: Hacer 2 cortes para obtener 3 partes de tamaño similar
        // Primer corte: a 1/3 de la altura
        Vector3 firstCutPosition = bounds.min + Vector3.up * (size.y / 3f);
        Vector3 firstCutNormal = Vector3.up;

        SlicedHull firstSlice = activeCopy.Slice(firstCutPosition, firstCutNormal, crossSectionMaterial);

        if (firstSlice != null && firstSlice.upperHull != null && firstSlice.lowerHull != null)
        {
            // Crear las dos partes del primer corte
            GameObject upperPart = firstSlice.CreateUpperHull(activeCopy, crossSectionMaterial);
            GameObject lowerPart = firstSlice.CreateLowerHull(activeCopy, crossSectionMaterial);

            if (upperPart != null && lowerPart != null)
            {
                // Segundo corte: en la parte inferior a la mitad de su altura
                // Esto crea: top 33%, middle 33%, bottom 33%
                Bounds lowerBounds = GetModelBounds(lowerPart);
                Vector3 secondCutPosition = lowerBounds.min + Vector3.up * (lowerBounds.size.y / 2f);
                Vector3 secondCutNormal = Vector3.up;

                SlicedHull secondSlice = lowerPart.Slice(secondCutPosition, secondCutNormal, crossSectionMaterial);

                if (secondSlice != null && secondSlice.upperHull != null && secondSlice.lowerHull != null)
                {
                    // Crear las dos partes del segundo corte
                    GameObject middlePart = secondSlice.CreateUpperHull(lowerPart, crossSectionMaterial);
                    GameObject bottomPart = secondSlice.CreateLowerHull(lowerPart, crossSectionMaterial);

                    // Destruir la parte intermedia
                    Destroy(lowerPart);

                    // Destruir la copia activa ya que ya no la necesitamos
                    Destroy(activeCopy);

                    // Configurar las 3 partes finales (solo movimiento en X e Y)
                    SetupFragment(upperPart, new Vector3(-0.5f, 0.3f, 0f).normalized, 0);
                    SetupFragment(middlePart, new Vector3(0f, 0.3f, 0f).normalized, 1);
                    SetupFragment(bottomPart, new Vector3(0.5f, -0.3f, 0f).normalized, 2);
                }
                else
                {
                    // Si el segundo corte falla, usar solo las 2 partes (solo X e Y)
                    Destroy(activeCopy);
                    SetupFragment(upperPart, new Vector3(-0.5f, 0.3f, 0f).normalized, 0);
                    SetupFragment(lowerPart, new Vector3(0.5f, -0.3f, 0f).normalized, 1);
                }
            }
            else
            {
                // Si el primer corte falla, intentar método alternativo
                Destroy(activeCopy);
                CreateThreePartsAlternative(originalModel, position, rotation);
            }
        }
        else
        {
            // Si el slicing falla, usar método alternativo
            Destroy(activeCopy);
            CreateThreePartsAlternative(originalModel, position, rotation);
        }
    }

    void CreateThreePartsAlternative(GameObject originalModel, Vector3 position, Quaternion rotation)
    {
        // Método alternativo: crear una copia activa para cortar
        GameObject activeCopy = Instantiate(originalModel, position, rotation);
        activeCopy.SetActive(true);
        
        Bounds bounds = GetModelBounds(activeCopy);
        Vector3 center = bounds.center;

        // Cortar en 2 direcciones diferentes para obtener 3 partes
        Vector3[] cutNormals = { Vector3.up, Vector3.right };
        Vector3[] cutPositions = {
            center + Vector3.up * bounds.extents.y * 0.3f,
            center + Vector3.right * bounds.extents.x * 0.3f
        };

        GameObject currentTarget = activeCopy;
        List<GameObject> parts = new List<GameObject>();

        // Primer corte
        SlicedHull firstSlice = currentTarget.Slice(cutPositions[0], cutNormals[0], crossSectionMaterial);
        if (firstSlice != null && firstSlice.upperHull != null && firstSlice.lowerHull != null)
        {
            GameObject part1 = firstSlice.CreateUpperHull(currentTarget, crossSectionMaterial);
            GameObject part2 = firstSlice.CreateLowerHull(currentTarget, crossSectionMaterial);

            if (part1 != null) parts.Add(part1);
            if (part2 != null) 
            {
                // Segundo corte en una de las partes
                SlicedHull secondSlice = part2.Slice(cutPositions[1], cutNormals[1], crossSectionMaterial);
                if (secondSlice != null && secondSlice.upperHull != null && secondSlice.lowerHull != null)
                {
                    GameObject part2a = secondSlice.CreateUpperHull(part2, crossSectionMaterial);
                    GameObject part2b = secondSlice.CreateLowerHull(part2, crossSectionMaterial);
                    if (part2a != null) parts.Add(part2a);
                    if (part2b != null) parts.Add(part2b);
                    Destroy(part2);
                }
                else
                {
                    parts.Add(part2);
                }
            }

            Destroy(currentTarget);
        }

        // Configurar las partes resultantes (máximo 3, solo movimiento en X e Y)
        int partCount = Mathf.Min(parts.Count, 3);
        for (int i = 0; i < partCount; i++)
        {
            float angle = (i * 360f / partCount) * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0.2f, 0f).normalized;
            SetupFragment(parts[i], direction, i);
        }
    }

    Bounds GetModelBounds(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
            return renderer.bounds;

        // Si no hay renderer, buscar en los hijos
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
                bounds.Encapsulate(r.bounds);
            return bounds;
        }

        return new Bounds(obj.transform.position, Vector3.one);
    }

    void SetupFragment(GameObject obj, Vector3 direction, int index)
    {
        if (obj == null) return;

        obj.name = $"SplitPart_{index + 1}";
        obj.transform.SetParent(transform);
        obj.SetActive(true);

        // Aplicar movimiento visual simple (sin física)
        // Usar un script simple de movimiento o simplemente mover la posición
        StartCoroutine(MoveFragment(obj, direction));

        // Auto-destruir si está configurado
        if (destroyAfter)
        {
            Destroy(obj, destroyDelay);
        }
    }

    IEnumerator MoveFragment(GameObject obj, Vector3 direction)
    {
        float elapsedTime = 0f;
        float duration = 1f; // Duración del movimiento
        Vector3 startPosition = obj.transform.position;
        Vector3 targetPosition = startPosition + direction * separationForce * 1.5f; // Movimiento más sutil

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            // Easing out suave
            t = 1f - Mathf.Pow(1f - t, 3f);
            obj.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            // Sin rotación - solo movimiento
            yield return null;
        }
    }
}
