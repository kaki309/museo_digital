using System.Collections;
using UnityEngine;

/// <summary>
/// Handles displaying fragment GameObjects when fragments are found
/// </summary>
public class FragmentDisplayHandler : MonoBehaviour
{
    private GameObject fragmentObject; // Predefined GameObject in trivia canvas to show
    private float displayDuration = 2f; // How long to show the fragment
    
    /// <summary>
    /// Initialize the fragment display handler
    /// </summary>
    public void Initialize(GameObject fragmentObj, float duration = 2f)
    {
        fragmentObject = fragmentObj;
        displayDuration = duration;
        
        // Ensure fragment is hidden initially
        if (fragmentObject != null)
        {
            fragmentObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Show a fragment GameObject for a short time
    /// </summary>
    public void ShowFragment()
    {
        if (fragmentObject == null)
        {
            Debug.LogWarning("Fragment GameObject not set. Cannot show fragment.");
            return;
        }
        
        StartCoroutine(DisplayFragmentCoroutine());
    }
    
    /// <summary>
    /// Coroutine that displays the fragment GameObject
    /// </summary>
    private IEnumerator DisplayFragmentCoroutine()
    {
        // Enable the fragment GameObject
        fragmentObject.SetActive(true);
        
        Debug.Log("Fragment displayed");
        
        // Wait for the display duration
        yield return new WaitForSeconds(displayDuration);
        
        // Disable the fragment GameObject
        if (fragmentObject != null)
        {
            fragmentObject.SetActive(false);
        }
    }
}

