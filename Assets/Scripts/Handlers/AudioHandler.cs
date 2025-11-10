using System.Collections;
using UnityEngine;

/// <summary>
/// Handles audio playback for the sequence manager
/// Supports non-blocking audio playback with automatic waiting for previous audio to finish
/// </summary>
public class AudioHandler : MonoBehaviour
{
    private AudioSource audioSource;
    private bool isAudioPlaying = false;
    private Coroutine audioMonitorCoroutine;
    private AudioClip currentAudioClip = null;
    
    /// <summary>
    /// Initialize the audio handler with an AudioSource
    /// </summary>
    public void Initialize(AudioSource source)
    {
        audioSource = source;
    }
    
    /// <summary>
    /// Check if audio is currently playing
    /// </summary>
    public bool IsPlaying()
    {
        return isAudioPlaying && audioSource != null && audioSource.isPlaying;
    }
    
    /// <summary>
    /// Plays an audio file (non-blocking - audio plays in background while sequence continues)
    /// If another audio is playing, waits for it to finish before starting the new one
    /// </summary>
    public IEnumerator PlayAudio(string resourcePath, System.Action onAudioStart = null)
    {
        if (audioSource == null)
        {
            Debug.LogWarning("No AudioSource assigned. Cannot play audio.");
            yield break;
        }
        
        // If audio is already playing, wait for it to finish
        if (isAudioPlaying && audioSource.isPlaying)
        {
            Debug.Log("Audio already playing. Waiting for current audio to finish...");
            yield return StartCoroutine(WaitForAudioToFinish());
        }
        
        // Load the audio clip
        AudioClip clip = null;
        
        // Try to load from Resources first
        clip = Resources.Load<AudioClip>(resourcePath);
        
        if (clip == null)
        {
            // Try loading from StreamingAssets (requires UnityWebRequest for compressed formats)
            Debug.LogWarning($"Audio clip not found in Resources: {resourcePath}");
            Debug.LogWarning("Trying to load from StreamingAssets...");
            
            string basePath = System.IO.Path.Combine(Application.streamingAssetsPath, resourcePath);
            string fullPath = "";
            AudioType audioType = AudioType.WAV;
            
            // Try different audio formats
            string[] extensions = { ".wav", ".mp3", ".ogg", ".m4a" };
            AudioType[] audioTypes = { AudioType.WAV, AudioType.MPEG, AudioType.OGGVORBIS, AudioType.MPEG };
            
            bool fileFound = false;
            for (int i = 0; i < extensions.Length; i++)
            {
                fullPath = basePath + extensions[i];
                if (System.IO.File.Exists(fullPath))
                {
                    audioType = audioTypes[i];
                    fileFound = true;
                    break;
                }
            }
            
            if (fileFound)
            {
                using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(
                    "file://" + fullPath, audioType))
                {
                    yield return www.SendWebRequest();
                    
                    if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                    }
                    else
                    {
                        Debug.LogError($"Failed to load audio: {www.error}");
                    }
                }
            }
            else
            {
                Debug.LogError($"Audio file not found at: {basePath} (tried .wav, .mp3, .ogg, .m4a)");
            }
        }
        
        if (clip != null)
        {
            // Ensure previous audio is fully stopped and state is cleared
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            
            // Clear previous audio state
            isAudioPlaying = false;
            if (audioMonitorCoroutine != null)
            {
                StopCoroutine(audioMonitorCoroutine);
                audioMonitorCoroutine = null;
            }
            
            // Set and play the new audio
            audioSource.clip = clip;
            currentAudioClip = clip;
            audioSource.Play();
            
            // Mark audio as playing
            isAudioPlaying = true;
            
            // Start monitoring audio playback (to update isAudioPlaying flag when it finishes)
            audioMonitorCoroutine = StartCoroutine(MonitorAudioPlayback());
            
            // Wait until audio actually starts playing (with timeout for safety)
            float timeout = 2f;
            float elapsed = 0f;
            while (!audioSource.isPlaying && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (!audioSource.isPlaying)
            {
                Debug.LogWarning("Audio failed to start playing within timeout period.");
                isAudioPlaying = false;
            }
            else
            {
                // Audio started successfully - wait a brief moment to ensure it's fully started
                yield return new WaitForSeconds(0.05f);
                
                // Callback for when audio starts (e.g., to move Laia)
                onAudioStart?.Invoke();
            }
        }
        else
        {
            Debug.LogError($"Failed to load audio: {resourcePath}");
        }
    }
    
    /// <summary>
    /// Waits for the current audio to finish playing
    /// </summary>
    public IEnumerator WaitForAudioToFinish()
    {
        // Wait until audio is no longer playing
        while (audioSource != null && audioSource.isPlaying)
        {
            yield return null;
        }
        
        // Ensure state is cleared
        isAudioPlaying = false;
        if (audioMonitorCoroutine != null)
        {
            StopCoroutine(audioMonitorCoroutine);
            audioMonitorCoroutine = null;
        }
    }
    
    /// <summary>
    /// Monitors audio playback and updates the isAudioPlaying flag when audio finishes
    /// </summary>
    private IEnumerator MonitorAudioPlayback()
    {
        while (audioSource != null && audioSource.isPlaying)
        {
            yield return null;
        }
        
        // Audio finished playing
        isAudioPlaying = false;
        currentAudioClip = null;
        audioMonitorCoroutine = null;
    }
    
    /// <summary>
    /// Stop the current audio playback
    /// </summary>
    public void Stop()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        if (audioMonitorCoroutine != null)
        {
            StopCoroutine(audioMonitorCoroutine);
            audioMonitorCoroutine = null;
        }
        
        isAudioPlaying = false;
        currentAudioClip = null;
    }
    
    /// <summary>
    /// Pause the current audio playback
    /// </summary>
    public void Pause()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }
    
    /// <summary>
    /// Resume the current audio playback
    /// </summary>
    public void UnPause()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.UnPause();
            // If audio was playing, restore the monitoring
            if (isAudioPlaying && audioMonitorCoroutine == null)
            {
                audioMonitorCoroutine = StartCoroutine(MonitorAudioPlayback());
            }
        }
    }
}

