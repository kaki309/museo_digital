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
    
    [Header("Trivia Settings")]
    public GameObject triviaCanvas; // Canvas GameObject that contains trivia UI
    public TMPro.TextMeshProUGUI triviaQuestionText; // Text component for the question
    [Tooltip("Buttons for answers. Text will be automatically set on button's child Text/TextMeshProUGUI component.")]
    public Button triviaAnswer1Button; // Button for answer 1 (text auto-filled from button's child)
    public Button triviaAnswer2Button; // Button for answer 2 (text auto-filled from button's child)
    public Button triviaAnswer3Button; // Button for answer 3 (text auto-filled from button's child)
     // AudioSource on trivia canvas for playing answer feedback sounds
    private AudioSource triviaAudioSource;
    [Tooltip("Audio file names in StreamingAssets/System folder (without extension). Will try .wav, .mp3, .ogg, .m4a")]
    public string correctAnswerAudio = "correct"; // Audio file name for correct answer
    public string incorrectAnswerAudio = "incorrect"; // Audio file name for incorrect answer
    
    [Header("Laia Assistant Settings")]
    public GameObject laiaObject; // GameObject representing Laia (will be moved to positions)
    public RawImage laiaImage; // UI RawImage to display Laia's facial expressions
    [Tooltip("Array of Transform positions where Laia can move. Index is 1-based (1 = first position, 2 = second, etc.)")]
    public Transform[] laiaPositions; // Predefined positions for Laia to move to
    
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
    
    // Trivia state tracking
    private bool isTriviaActive = false;
    private bool triviaAnswerSelected = false;
    private int selectedAnswerIndex = -1;
    private SequenceInstruction currentTriviaInstruction = null;
    
    // Laia position tracking
    private int currentLaiaPositionIndex = 0; // Current position index (0-based for array)
    
    // Events
    public System.Action<SequenceInstruction> OnInstructionStart;
    public System.Action<SequenceInstruction> OnInstructionComplete;
    public System.Action OnSequenceComplete;
    
    void Start()
    {
        // Load and parse the sequence file
        LoadSequence();

        // Setup trivia answer buttons if they exist
        SetupTriviaButtons();
        
        // Hide trivia canvas initially
        if (triviaCanvas != null)
        {
            triviaCanvas.SetActive(false);
        }
        
        // Initialize trivia AudioSource from trivia canvas
        InitializeTriviaAudioSource();
        
        // Initialize Laia image to LaiaHappy
        InitializeLaiaImage();

        // Auto-play if enabled
        if (playOnStart && instructions.Count > 0)
        {
            StartSequence();
        }
    }
    
    /// <summary>
    /// Initializes Laia's image to LaiaHappy automatically
    /// </summary>
    private void InitializeLaiaImage()
    {
        if (laiaImage == null)
        {
            return; // Laia image not assigned, skip initialization
        }
        
        // Try to load LaiaHappy from StreamingAssets/System/LaiaImage
        string imagePath = Path.Combine(Application.streamingAssetsPath, "System", "LaiaImage", "LaiaHappy");
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
            Debug.LogWarning($"LaiaHappy image file not found at: {imagePath} (tried .png, .jpg, .jpeg). Laia image will not be initialized.");
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
    /// Sets up button listeners for trivia answers
    /// </summary>
    private void SetupTriviaButtons()
    {
        if (triviaAnswer1Button != null)
        {
            triviaAnswer1Button.onClick.RemoveAllListeners();
            triviaAnswer1Button.onClick.AddListener(() => OnTriviaAnswerSelected(0));
        }
        
        if (triviaAnswer2Button != null)
        {
            triviaAnswer2Button.onClick.RemoveAllListeners();
            triviaAnswer2Button.onClick.AddListener(() => OnTriviaAnswerSelected(1));
        }
        
        if (triviaAnswer3Button != null)
        {
            triviaAnswer3Button.onClick.RemoveAllListeners();
            triviaAnswer3Button.onClick.AddListener(() => OnTriviaAnswerSelected(2));
        }
    }
    
    /// <summary>
    /// Called when a trivia answer is selected
    /// </summary>
    private void OnTriviaAnswerSelected(int answerIndex)
    {
        if (!isTriviaActive || currentTriviaInstruction == null)
        {
            return;
        }
        
        selectedAnswerIndex = answerIndex;
        
        // Check if the answer is correct
        if (answerIndex == currentTriviaInstruction.correctAnswerIndex)
        {
            // Correct answer - play correct audio and resume sequence
            PlayTriviaAudio(correctAnswerAudio);
            triviaAnswerSelected = true;
            isTriviaActive = false;
        }
        else
        {
            // Wrong answer - play incorrect audio and let user try again
            PlayTriviaAudio(incorrectAnswerAudio);
            Debug.Log($"Wrong answer selected. Correct answer is: {currentTriviaInstruction.correctAnswerIndex + 1}");
        }
    }
    
    /// <summary>
    /// Initializes the trivia AudioSource from the trivia canvas GameObject
    /// </summary>
    private void InitializeTriviaAudioSource()
    {
        if (triviaCanvas == null)
        {
            Debug.LogWarning("Trivia canvas is not assigned. Cannot initialize trivia AudioSource.");
            return;
        }
        
        triviaAudioSource = triviaCanvas.GetComponent<AudioSource>();
        
        if (triviaAudioSource == null)
        {
            Debug.LogWarning("No AudioSource found on trivia canvas. Trivia answer audio will not play.");
        }
        else
        {
            Debug.Log("Trivia AudioSource initialized successfully.");
        }
    }
    
    /// <summary>
    /// Plays an audio file from StreamingAssets/System folder
    /// </summary>
    private void PlayTriviaAudio(string audioFileName)
    {
        if (triviaAudioSource == null)
        {
            Debug.LogWarning("Trivia AudioSource is not initialized. Cannot play answer feedback audio.");
            return;
        }
        
        if (string.IsNullOrEmpty(audioFileName))
        {
            Debug.LogWarning("Audio file name is empty. Cannot play trivia audio.");
            return;
        }
        
        // Start coroutine to load and play audio
        StartCoroutine(LoadAndPlayTriviaAudio(audioFileName));
    }
    
    /// <summary>
    /// Loads and plays an audio file from StreamingAssets/System folder
    /// </summary>
    private IEnumerator LoadAndPlayTriviaAudio(string audioFileName)
    {
        string systemPath = Path.Combine(Application.streamingAssetsPath, "System", audioFileName);
        string fullPath = "";
        AudioType audioType = AudioType.WAV;
        
        // Try different audio formats
        string[] extensions = { ".wav", ".mp3", ".ogg", ".m4a" };
        AudioType[] audioTypes = { AudioType.WAV, AudioType.MPEG, AudioType.OGGVORBIS, AudioType.MPEG };
        
        bool fileFound = false;
        for (int i = 0; i < extensions.Length; i++)
        {
            fullPath = systemPath + extensions[i];
            if (File.Exists(fullPath))
            {
                audioType = audioTypes[i];
                fileFound = true;
                break;
            }
        }
        
        if (!fileFound)
        {
            Debug.LogWarning($"Trivia audio file not found at: {systemPath} (tried .wav, .mp3, .ogg, .m4a)");
            yield break;
        }
        
        // Load audio using UnityWebRequest
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(
            "file://" + fullPath, audioType))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                
                if (clip != null && triviaAudioSource != null)
                {
                    // Stop any currently playing audio on trivia audio source
                    if (triviaAudioSource.isPlaying)
                    {
                        triviaAudioSource.Stop();
                    }
                    
                    // Play the audio
                    triviaAudioSource.clip = clip;
                    triviaAudioSource.Play();
                }
                else if (clip != null && triviaAudioSource == null)
                {
                    Debug.LogWarning("Trivia AudioSource is null. Cannot play audio.");
                }
            }
            else
            {
                Debug.LogError($"Failed to load trivia audio: {www.error}");
            }
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
        
        // Hide trivia if active
        if (isTriviaActive && triviaCanvas != null)
        {
            triviaCanvas.SetActive(false);
        }
        isTriviaActive = false;
        triviaAnswerSelected = false;
        currentTriviaInstruction = null;
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
                
            case InstructionType.Trivia:
                // Trivia: wait for any audio to finish, then show trivia and wait for correct answer
                yield return StartCoroutine(ShowTrivia(instruction));
                break;
                
            case InstructionType.LaiaImage:
                // Change Laia's facial expression
                ChangeLaiaImage(instruction);
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
                
                // Automatically move Laia to next position when audio starts
                MoveLaiaToNextPosition();
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
    
    /// <summary>
    /// Shows trivia and waits for the correct answer to be selected
    /// Waits for any audio to finish before showing trivia
    /// </summary>
    private IEnumerator ShowTrivia(SequenceInstruction instruction)
    {
        // Wait for any audio to finish playing first
        if (isAudioPlaying && audioSource != null && audioSource.isPlaying)
        {
            Debug.Log("Trivia: Waiting for audio to finish...");
            yield return StartCoroutine(WaitForAudioToFinish());
        }
        
        // Validate trivia instruction
        if (instruction.triviaAnswers == null || instruction.triviaAnswers.Length < 3)
        {
            Debug.LogError("Invalid trivia instruction: missing answers");
            yield break;
        }
        
        // Store current trivia instruction
        currentTriviaInstruction = instruction;
        isTriviaActive = true;
        triviaAnswerSelected = false;
        selectedAnswerIndex = -1;
        
        // Show trivia canvas
        if (triviaCanvas != null)
        {
            triviaCanvas.SetActive(true);
        }
        else
        {
            Debug.LogError("Trivia canvas is not assigned!");
            yield break;
        }
        
        // Setup button listeners (in case they weren't set up at start or were changed)
        SetupTriviaButtons();
        
        // Display question
        if (triviaQuestionText != null)
        {
            triviaQuestionText.text = instruction.triviaQuestion;
            triviaQuestionText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Trivia question text is not assigned!");
        }
        
        // Display answers on buttons - automatically set text on button's child text component
        SetAnswerText(0, instruction.triviaAnswers[0]);
        SetAnswerText(1, instruction.triviaAnswers[1]);
        SetAnswerText(2, instruction.triviaAnswers[2]);
        
        // Enable answer buttons
        if (triviaAnswer1Button != null)
        {
            triviaAnswer1Button.gameObject.SetActive(true);
            triviaAnswer1Button.interactable = true;
        }
        
        if (triviaAnswer2Button != null)
        {
            triviaAnswer2Button.gameObject.SetActive(true);
            triviaAnswer2Button.interactable = true;
        }
        
        if (triviaAnswer3Button != null)
        {
            triviaAnswer3Button.gameObject.SetActive(true);
            triviaAnswer3Button.interactable = true;
        }
        
        // Warn if buttons are not assigned
        if (triviaAnswer1Button == null || triviaAnswer2Button == null || triviaAnswer3Button == null)
        {
            Debug.LogWarning("Trivia: One or more answer buttons are not assigned. Make sure to assign all three answer buttons.");
        }
        
        // Wait until correct answer is selected
        while (isTriviaActive && !triviaAnswerSelected)
        {
            yield return null;
        }
        
        // Wait 2 seconds after correct answer is selected
        yield return new WaitForSeconds(2f);
        
        // Hide trivia canvas
        if (triviaCanvas != null)
        {
            triviaCanvas.SetActive(false);
        }
        
        // Clear trivia state
        isTriviaActive = false;
        currentTriviaInstruction = null;
        triviaAnswerSelected = false;
        selectedAnswerIndex = -1;
        
        Debug.Log("Trivia completed. Resuming sequence...");
    }
    
    /// <summary>
    /// Sets answer text on the button's child text component
    /// </summary>
    private void SetAnswerText(int answerIndex, string answerText)
    {
        Button button = null;
        
        // Get the button for this answer
        switch (answerIndex)
        {
            case 0:
                button = triviaAnswer1Button;
                break;
            case 1:
                button = triviaAnswer2Button;
                break;
            case 2:
                button = triviaAnswer3Button;
                break;
        }
        
        if (button == null)
        {
            return;
        }
        
        // Find TextMeshProUGUI in button's children
        TMPro.TextMeshProUGUI textComponent = button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        
        if (textComponent != null)
        {
            textComponent.text = answerText;
            textComponent.gameObject.SetActive(true);
        }
        else
        {
            // Try legacy Text component as fallback
            UnityEngine.UI.Text legacyTextComponent = button.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (legacyTextComponent != null)
            {
                legacyTextComponent.text = answerText;
                legacyTextComponent.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"Trivia: Button {answerIndex + 1} does not have a Text or TextMeshProUGUI component in its children.");
            }
        }
    }
    
    /// <summary>
    /// Automatically moves Laia to the next position (cycles through positions)
    /// Called automatically when audio starts playing
    /// </summary>
    private void MoveLaiaToNextPosition()
    {
        if (laiaObject == null)
        {
            return; // Laia object not assigned, skip
        }
        
        if (laiaPositions == null || laiaPositions.Length == 0)
        {
            return; // No positions defined, skip
        }
        
        // Cycle to next position
        Transform targetPosition = laiaPositions[currentLaiaPositionIndex];
        
        if (targetPosition == null)
        {
            Debug.LogWarning($"Laia position at index {currentLaiaPositionIndex} is null.");
            // Move to next position anyway
            currentLaiaPositionIndex = (currentLaiaPositionIndex + 1) % laiaPositions.Length;
            return;
        }
        
        // Move Laia to the target position
        laiaObject.transform.position = targetPosition.position;
        laiaObject.transform.rotation = targetPosition.rotation;
        
        Debug.Log($"Laia moved to position {currentLaiaPositionIndex + 1} (automatically on audio start)");
        
        // Move to next position for next audio
        currentLaiaPositionIndex = (currentLaiaPositionIndex + 1) % laiaPositions.Length;
    }
    
    /// <summary>
    /// Changes Laia's facial expression image (RawImage - supports .jpg files)
    /// </summary>
    private void ChangeLaiaImage(SequenceInstruction instruction)
    {
        if (laiaImage == null)
        {
            Debug.LogWarning("Laia image (RawImage component) is not assigned. Cannot change Laia image.");
            return;
        }
        
        if (string.IsNullOrEmpty(instruction.laiaImageName))
        {
            Debug.LogWarning("Laia image name is empty. Cannot change Laia image.");
            return;
        }
        
        // Try to load from StreamingAssets/System/LaiaImage
        string imagePath = Path.Combine(Application.streamingAssetsPath, "System", "LaiaImage", instruction.laiaImageName);
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
    
    // Public getters
    public List<SequenceInstruction> GetInstructions() => instructions;
    public int GetCurrentInstructionIndex() => currentInstructionIndex;
    public bool IsPlaying() => isPlaying;
    public int GetInstructionCount() => instructions.Count;
}

