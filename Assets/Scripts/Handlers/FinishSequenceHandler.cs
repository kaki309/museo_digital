using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Handles the finish/goodbye sequence when the main sequence completes
/// </summary>
public class FinishSequenceHandler : MonoBehaviour
{
    private AudioHandler audioHandler;
    private ImageHandler imageHandler;
    private TextHandler textHandler;
    private LaiaHandler laiaHandler;
    
    private string finishAudioName = "FinishNarrative";
    private string finishLaiaImageName = "LaiaHappy"; // Default Laia image for finish
    private string nextSceneName = ""; // Scene to load after finish sequence
    
    // Reference to check if audio is playing
    private System.Func<bool> isAudioPlayingFunc;
    private System.Func<System.Collections.IEnumerator> waitForAudioFunc;
    
    /// <summary>
    /// Initialize the finish sequence handler with required handlers
    /// </summary>
    public void Initialize(AudioHandler audio, ImageHandler image, TextHandler text, LaiaHandler laia,
                          System.Func<bool> isAudioPlaying, System.Func<System.Collections.IEnumerator> waitForAudio)
    {
        audioHandler = audio;
        imageHandler = image;
        textHandler = text;
        laiaHandler = laia;
        isAudioPlayingFunc = isAudioPlaying;
        waitForAudioFunc = waitForAudio;
    }
    
    /// <summary>
    /// Set the finish sequence configuration
    /// </summary>
    public void SetFinishConfig(string audioName, string laiaImageName, string sceneName)
    {
        finishAudioName = audioName;
        finishLaiaImageName = laiaImageName;
        nextSceneName = sceneName;
    }
    
    /// <summary>
    /// Start the finish sequence
    /// </summary>
    public IEnumerator PlayFinishSequence()
    {
        Debug.Log("Starting finish sequence...");
        
        // Step 0: Wait for any audio from the sequence to finish playing
        if (isAudioPlayingFunc != null && isAudioPlayingFunc())
        {
            Debug.Log("Finish sequence: Waiting for sequence audio to finish...");
            if (waitForAudioFunc != null)
            {
                yield return StartCoroutine(waitForAudioFunc());
            }
        }
        
        // Step 1: Clean the screen (hide images, text, Laia image, etc.) - part of goodbye sequence
        CleanScreen();
        yield return new WaitForSeconds(0.5f); // Brief pause after cleaning
        
        // Step 2: Show LaiaHappy image in the main image display (like from txt)
        if (!string.IsNullOrEmpty(finishLaiaImageName) && imageHandler != null)
        {
            // Load LaiaHappy from StreamingAssets/System/LaiaImage and display in main image component
            imageHandler.ShowImage($"System/LaiaImage/{finishLaiaImageName}");
            yield return new WaitForSeconds(1f); // Brief pause to show Laia
        }

        // Step 3: Play finish audio
        if (!string.IsNullOrEmpty(finishAudioName) && audioHandler != null)
        {
            yield return StartCoroutine(audioHandler.PlayAudio($"System/{finishAudioName}", null));
            
            // Wait for audio to finish
            yield return StartCoroutine(audioHandler.WaitForAudioToFinish());
        }
        
        // Step 4: Load next scene
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log($"Loading scene: {nextSceneName}");
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("No next scene name set. Finish sequence complete.");
        }
    }
    
    /// <summary>
    /// Clean the screen by hiding all UI elements
    /// </summary>
    private void CleanScreen()
    {
        Debug.Log("Cleaning screen...");
        
        // Hide images (from .txt file)
        if (imageHandler != null)
        {
            imageHandler.Clean();
        }
        
        // Hide text
        if (textHandler != null)
        {
            textHandler.Clean();
        }
        
        // Hide Laia image (assistant image)
        if (laiaHandler != null)
        {
            laiaHandler.Clean();
        }
    }
}

