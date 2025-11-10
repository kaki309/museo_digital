using UnityEngine;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// Handles image display for the sequence manager
/// Supports both RawImage and Image (Sprite) components
/// </summary>
public class ImageHandler : MonoBehaviour
{
    private RawImage targetImage;
    private Image targetSpriteImage;
    
    /// <summary>
    /// Initialize the image handler with UI components
    /// </summary>
    public void Initialize(RawImage rawImage, Image spriteImage)
    {
        targetImage = rawImage;
        targetSpriteImage = spriteImage;
    }
    
    /// <summary>
    /// Shows an image from the given resource path
    /// </summary>
    public void ShowImage(string resourcePath)
    {
        // Try to load as Texture2D from Resources
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        
        if (texture != null)
        {
            DisplayTexture(texture);
        }
        else
        {
            // Try loading from StreamingAssets
            string basePath = Path.Combine(Application.streamingAssetsPath, resourcePath);
            string[] extensions = { ".png", ".jpg", ".jpeg" };
            string fullPath = "";
            bool fileFound = false;
            
            // Try different image formats
            foreach (string ext in extensions)
            {
                fullPath = basePath + ext;
                if (File.Exists(fullPath))
                {
                    fileFound = true;
                    break;
                }
            }
            
            if (fileFound)
            {
                byte[] fileData = File.ReadAllBytes(fullPath);
                texture = new Texture2D(2, 2);
                
                if (texture.LoadImage(fileData))
                {
                    DisplayTexture(texture);
                }
                else
                {
                    Debug.LogError($"Failed to load image data from: {fullPath}");
                }
            }
            else
            {
                Debug.LogError($"Image file not found at: {basePath} (tried .png, .jpg, .jpeg)");
            }
        }
    }
    
    /// <summary>
    /// Displays a texture on the appropriate UI component
    /// </summary>
    private void DisplayTexture(Texture2D texture)
    {
        if (targetImage != null)
        {
            targetImage.texture = texture;
            targetImage.gameObject.SetActive(true);
        }
        else if (targetSpriteImage != null)
        {
            // Convert texture to sprite
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            targetSpriteImage.sprite = sprite;
            targetSpriteImage.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("No image target assigned (RawImage or Image).");
        }
    }
    
    /// <summary>
    /// Cleans/hides the image display
    /// </summary>
    public void Clean()
    {
        if (targetImage != null)
        {
            targetImage.gameObject.SetActive(false);
            targetImage.texture = null;
        }
        
        if (targetSpriteImage != null)
        {
            targetSpriteImage.gameObject.SetActive(false);
            targetSpriteImage.sprite = null;
        }
    }
}

