using UnityEngine;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// Handles Laia assistant position and image changes
/// </summary>
public class LaiaHandler : MonoBehaviour
{   
    private GameObject laiaObject;
    private RawImage laiaImage;
    private Transform[] laiaPositions;
    
    /// <summary>
    /// Initialize the Laia handler with required components
    /// </summary>
    public void Initialize(GameObject laiaObj, RawImage image, Transform[] positions)
    {
        laiaObject = laiaObj;
        laiaImage = image;
        laiaPositions = positions;
    }
    
    /// <summary>
    /// Initialize Laia's image to LaiaHappy automatically
    /// </summary>
    public void InitializeImage()
    {
        if (laiaImage == null)
        {
            return; // Laia image not assigned, skip initialization
        }
        
        // Try to load LaiaHappy from StreamingAssets/System/LaiaImage
        string imagePath = Path.Combine(Application.streamingAssetsPath, "System", "LaiaImage", "LaiaFeliz");
        string[] extensions = { ".png", ".jpg", ".jpeg" };
        string fullPath = "";
        bool fileFound = false;
        
        // Try different image formats
        foreach (string ext in extensions)
        {
            fullPath = imagePath + ext;
            if (File.Exists(fullPath))
            {
                fileFound = true;
                break;
            }
        }
        
        if (!fileFound)
        {
            Debug.LogWarning($"LaiaFeliz image file not found at: {imagePath} (tried .png, .jpg, .jpeg). Laia image will not be initialized.");
            return;
        }
        
        // Load and display the image
        byte[] fileData = File.ReadAllBytes(fullPath);
        Texture2D texture = new Texture2D(2, 2);
        
        if (texture.LoadImage(fileData))
        {
            laiaImage.texture = texture;
            laiaImage.gameObject.SetActive(true);
            Debug.Log("Laia image initialized to LaiaHappy");
        }
        else
        {
            Debug.LogError($"Failed to load LaiaHappy image data from: {fullPath}");
        }
    }
    
    /// <summary>
    /// Automatically moves Laia to a random position from available positions
    /// Called automatically when audio starts playing
    /// </summary>
    public void MoveToNextPosition()
    {
        if (laiaObject == null)
        {
            return; // Laia object not assigned, skip
        }
        
        if (laiaPositions == null || laiaPositions.Length == 0)
        {
            return; // No positions defined, skip
        }
        
        // Select a random position
        int randomIndex = Random.Range(0, laiaPositions.Length);
        Transform targetPosition = laiaPositions[randomIndex];
        
        if (targetPosition == null)
        {
            Debug.LogWarning($"Laia position at index {randomIndex} is null.");
            return;
        }
        
        // Move Laia to the target position
        laiaObject.transform.position = targetPosition.position;
        laiaObject.transform.rotation = targetPosition.rotation;
        
        Debug.Log($"Laia moved to random position {randomIndex + 1} (automatically on audio start)");
    }
    
    /// <summary>
    /// Changes Laia's facial expression image (RawImage - supports .jpg files)
    /// </summary>
    public void ChangeImage(string imageName)
    {
        if (laiaImage == null)
        {
            Debug.LogWarning("Laia image (RawImage component) is not assigned. Cannot change Laia image.");
            return;
        }
        
        if (string.IsNullOrEmpty(imageName))
        {
            Debug.LogWarning("Laia image name is empty. Cannot change Laia image.");
            return;
        }
        
        // Try to load from StreamingAssets/System/LaiaImage
        string imagePath = Path.Combine(Application.streamingAssetsPath, "System", "LaiaImage", imageName);
        string[] extensions = { ".png", ".jpg", ".jpeg" };
        string fullPath = "";
        bool fileFound = false;
        
        // Try different image formats
        foreach (string ext in extensions)
        {
            fullPath = imagePath + ext;
            if (File.Exists(fullPath))
            {
                fileFound = true;
                break;
            }
        }
        
        if (!fileFound)
        {
            Debug.LogError($"Laia image file not found at: {imagePath} (tried .png, .jpg, .jpeg)");
            return;
        }
        
        // Load and display the image as a Texture2D (for RawImage)
        byte[] fileData = File.ReadAllBytes(fullPath);
        Texture2D texture = new Texture2D(2, 2);
        
        if (texture.LoadImage(fileData))
        {
            laiaImage.texture = texture;
            laiaImage.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError($"Failed to load Laia image data from: {fullPath}");
        }
    }
    
    /// <summary>
    /// Cleans/hides the Laia image
    /// </summary>
    public void Clean()
    {
        if (laiaImage != null)
        {
            laiaImage.gameObject.SetActive(false);
            laiaImage.texture = null;
        }
    }
}

