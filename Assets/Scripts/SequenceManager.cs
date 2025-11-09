using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class SequenceManager : MonoBehaviour
{
    [Header("Sequence File")]
    [Tooltip("Name of the .txt file in StreamingAssets folder (without .txt extension)")]
    public string sequenceFileName = "sequence";
    
    [Header("Audio Settings")]
    public AudioSource audioSource;
    
    [Header("Image Settings")]
    public RawImage targetImage; // UI RawImage to display images
    public Image targetSpriteImage; // Alternative: UI Image for sprites
    
    [Header("Text Settings")]
    public TMPro.TextMeshProUGUI targetText; // TextMeshPro component for text display
    
    [Header("Sequence Control")]
    public bool playOnStart = false;
    public bool loopSequence = false;
    
    // List of parsed instructions
    private List<SequenceInstruction> instructions = new List<SequenceInstruction>();
    private int currentInstructionIndex = 0;
    private bool isPlaying = false;
    private Coroutine currentSequenceCoroutine;
    
    // Audio state tracking for non-blocking playback
    private bool isAudioPlaying = false;
    private Coroutine audioMonitorCoroutine;
    private AudioClip currentAudioClip = null;
    
    // Events
    public System.Action<SequenceInstruction> OnInstructionStart;
    public System.Action<SequenceInstruction> OnInstructionComplete;
    public System.Action OnSequenceComplete;
    
    void Start()
    {
        // Load and parse the sequence file
        LoadSequence();
        
        // Auto-play if enabled
        if (playOnStart && instructions.Count > 0)
        {
            StartSequence();
        }
    }
    
    /// <summary>
    /// Loads and parses the sequence file from StreamingAssets
    /// </summary>
    public void LoadSequence()
    {
        instructions.Clear();
        
        string filePath = Path.Combine(Application.streamingAssetsPath, sequenceFileName + ".txt");
        
        // Check if file exists
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Sequence file not found: {filePath}");
            Debug.LogWarning("Make sure the file is in the StreamingAssets folder!");
            return;
        }
        
        // Read all lines from the file
        string[] lines = File.ReadAllLines(filePath);
        
        // Parse each line into an instruction
        foreach (string line in lines)
        {
            SequenceInstruction instruction = new SequenceInstruction(line);
            
            // Only add valid instructions (skip empty lines and comments)
            if (instruction.type != InstructionType.Unknown)
            {
                instructions.Add(instruction);
            }
        }
        
        Debug.Log($"Loaded {instructions.Count} instructions from {filePath}");
    }
    
    /// <summary>
    /// Starts playing the sequence from the beginning
    /// </summary>
    public void StartSequence()
    {
        if (instructions.Count == 0)
        {
            Debug.LogWarning("No instructions loaded. Cannot start sequence.");
            return;
        }
        
        StopSequence();
        currentInstructionIndex = 0;
        isPlaying = true;
        currentSequenceCoroutine = StartCoroutine(PlaySequence());
    }
    
    /// <summary>
    /// Stops the current sequence
    /// </summary>
    public void StopSequence()
    {
        if (currentSequenceCoroutine != null)
        {
            StopCoroutine(currentSequenceCoroutine);
            currentSequenceCoroutine = null;
        }
        
        isPlaying = false;
        
        // Stop audio if playing
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        // Stop audio monitoring
        if (audioMonitorCoroutine != null)
        {
            StopCoroutine(audioMonitorCoroutine);
            audioMonitorCoroutine = null;
        }
        
        isAudioPlaying = false;
        currentAudioClip = null;
    }
    
    /// <summary>
    /// Pauses the sequence
    /// </summary>
    public void PauseSequence()
    {
        isPlaying = false;
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }
    
    /// <summary>
    /// Resumes the sequence
    /// </summary>
    public void ResumeSequence()
    {
        isPlaying = true;
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.UnPause();
            // If audio was playing, restore the monitoring
            if (isAudioPlaying && audioMonitorCoroutine == null)
            {
                audioMonitorCoroutine = StartCoroutine(MonitorAudioPlayback());
            }
        }
        if (currentSequenceCoroutine == null)
        {
            currentSequenceCoroutine = StartCoroutine(PlaySequence());
        }
    }
    
    /// <summary>
    /// Jumps to a specific instruction index
    /// </summary>
    public void JumpToInstruction(int index)
    {
        if (index >= 0 && index < instructions.Count)
        {
            StopSequence();
            currentInstructionIndex = index;
            StartSequence();
        }
    }
    
    /// <summary>
    /// Main coroutine that plays the sequence
    /// </summary>
    private IEnumerator PlaySequence()
    {
        while (currentInstructionIndex < instructions.Count && isPlaying)
        {
            SequenceInstruction instruction = instructions[currentInstructionIndex];
            
            // Notify instruction start
            OnInstructionStart?.Invoke(instruction);
            
            // Execute the instruction
            yield return StartCoroutine(ExecuteInstruction(instruction));
            
            // Notify instruction complete
            OnInstructionComplete?.Invoke(instruction);
            
            // Move to next instruction
            currentInstructionIndex++;
        }
        
        // Sequence complete
        isPlaying = false;
        OnSequenceComplete?.Invoke();
        
        // Loop if enabled
        if (loopSequence)
        {
            currentInstructionIndex = 0;
            yield return new WaitForSeconds(1f); // Brief pause before looping
            StartSequence();
        }
    }
    
    /// <summary>
    /// Executes a single instruction
    /// </summary>
    private IEnumerator ExecuteInstruction(SequenceInstruction instruction)
    {
        switch (instruction.type)
        {
            case InstructionType.Audio:
                // For audio: wait for previous audio to finish, then start new one (non-blocking)
                yield return StartCoroutine(PlayAudio(instruction));
                break;
                
            case InstructionType.Image:
                ShowImage(instruction);
                break;
                
            case InstructionType.Wait:
                yield return new WaitForSeconds(instruction.waitDuration);
                break;
                
            case InstructionType.Text:
                ShowText(instruction);
                break;
                
            case InstructionType.Action:
                ExecuteAction(instruction);
                break;
        }
    }
    
    /// <summary>
    /// Plays an audio file (non-blocking - audio plays in background while sequence continues)
    /// If another audio is playing, waits for it to finish before starting the new one
    /// </summary>
    private IEnumerator PlayAudio(SequenceInstruction instruction)
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
        clip = Resources.Load<AudioClip>(instruction.resourcePath);
        
        if (clip == null)
        {
            // Try loading from StreamingAssets (requires UnityWebRequest for compressed formats)
            Debug.LogWarning($"Audio clip not found in Resources: {instruction.resourcePath}");
            Debug.LogWarning("Trying to load from StreamingAssets...");
            
            string basePath = Path.Combine(Application.streamingAssetsPath, instruction.resourcePath);
            string fullPath = "";
            AudioType audioType = AudioType.WAV;
            
            // Try different audio formats
            string[] extensions = { ".wav", ".mp3", ".ogg", ".m4a" };
            AudioType[] audioTypes = { AudioType.WAV, AudioType.MPEG, AudioType.OGGVORBIS, AudioType.MPEG };
            
            bool fileFound = false;
            for (int i = 0; i < extensions.Length; i++)
            {
                fullPath = basePath + extensions[i];
                if (File.Exists(fullPath))
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
            }
        }
        else
        {
            Debug.LogError($"Failed to load audio: {instruction.resourcePath}");
        }
    }
    
    /// <summary>
    /// Waits for the current audio to finish playing
    /// </summary>
    private IEnumerator WaitForAudioToFinish()
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
    /// Shows an image
    /// </summary>
    private void ShowImage(SequenceInstruction instruction)
    {
        // Try to load as Texture2D from Resources
        Texture2D texture = Resources.Load<Texture2D>(instruction.resourcePath);
        
        if (texture != null)
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
        else
        {
            // Try loading from StreamingAssets
            string basePath = Path.Combine(Application.streamingAssetsPath, instruction.resourcePath);
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
                    if (targetImage != null)
                    {
                        targetImage.texture = texture;
                        targetImage.gameObject.SetActive(true);
                    }
                    else if (targetSpriteImage != null)
                    {
                        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        targetSpriteImage.sprite = sprite;
                        targetSpriteImage.gameObject.SetActive(true);
                    }
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
    /// Shows text
    /// </summary>
    private void ShowText(SequenceInstruction instruction)
    {
        if (targetText != null)
        {
            targetText.text = instruction.displayText;
            targetText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("No TextMeshProUGUI assigned. Cannot display text.");
        }
    }
    
    /// <summary>
    /// Executes a custom action (extensible for custom behaviors)
    /// </summary>
    private void ExecuteAction(SequenceInstruction instruction)
    {
        Debug.Log($"Executing action: {instruction.resourcePath}");
        // You can extend this to handle custom actions
        // For example, trigger animations, change scenes, etc.
    }
    
    // Public getters
    public List<SequenceInstruction> GetInstructions() => instructions;
    public int GetCurrentInstructionIndex() => currentInstructionIndex;
    public bool IsPlaying() => isPlaying;
    public int GetInstructionCount() => instructions.Count;
}

