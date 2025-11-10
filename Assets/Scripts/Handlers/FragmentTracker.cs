using UnityEngine;

/// <summary>
/// Tracks the number of fragments found by the player
/// </summary>
public class FragmentTracker : MonoBehaviour
{
    private int fragmentsFound = 0;
    
    /// <summary>
    /// Get the current number of fragments found
    /// </summary>
    public int GetFragmentsFound()
    {
        return fragmentsFound;
    }
    
    /// <summary>
    /// Increment the fragment count (called when trivia is answered correctly)
    /// </summary>
    public void AddFragment()
    {
        fragmentsFound++;
        Debug.Log($"Fragment found! Total fragments: {fragmentsFound}");
    }
    
    /// <summary>
    /// Reset the fragment count
    /// </summary>
    public void ResetFragments()
    {
        fragmentsFound = 0;
        Debug.Log("Fragments reset to 0");
    }
}

