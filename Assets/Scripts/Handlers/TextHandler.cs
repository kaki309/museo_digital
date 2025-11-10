using UnityEngine;
using TMPro;

/// <summary>
/// Handles text display for the sequence manager
/// </summary>
public class TextHandler : MonoBehaviour
{
    private TMPro.TextMeshProUGUI targetText;
    
    /// <summary>
    /// Initialize the text handler with a TextMeshProUGUI component
    /// </summary>
    public void Initialize(TMPro.TextMeshProUGUI text)
    {
        targetText = text;
    }
    
    /// <summary>
    /// Shows text on the UI
    /// </summary>
    public void ShowText(string text)
    {
        if (targetText != null)
        {
            targetText.text = text;
            targetText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("No TextMeshProUGUI assigned. Cannot display text.");
        }
    }
    
    /// <summary>
    /// Cleans/hides the text display
    /// </summary>
    public void Clean()
    {
        if (targetText != null)
        {
            targetText.gameObject.SetActive(false);
            targetText.text = "";
        }
    }
}

