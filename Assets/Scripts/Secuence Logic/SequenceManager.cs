using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// Main sequence manager that orchestrates the execution of sequence instructions
/// Uses specialized handlers for different instruction types
/// </summary>
public class SequenceManager : MonoBehaviour
{
    [Header("Directly initialize secuence")]
    [Tooltip("Turn on only if is needed to ignore the RFID lecture and start a secuence directly")]
    public bool initializeSecuenceDirectly = false;
    public string sequenceFileName;

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
    [Tooltip("Audio file names in StreamingAssets/System folder (without extension). Will try .wav, .mp3, .ogg, .m4a")]
    public string correctAnswerAudio = "correct"; // Audio file name for correct answer
    public string incorrectAnswerAudio = "incorrect"; // Audio file name for incorrect answer
    
    [Header("Laia Assistant Settings")]
    public GameObject laiaObject; // GameObject representing Laia (will be moved to positions)
    public RawImage laiaImage; // UI RawImage to display Laia's facial expressions
    [Tooltip("Array of Transform positions where Laia can move. Index is 1-based (1 = first position, 2 = second, etc.)")]
    public Transform[] laiaPositions; // Predefined positions for Laia to move to
    
    [Header("Fragment Settings")]
    [Tooltip("Predefined GameObject in trivia canvas to show when a fragment is found")]
    public GameObject fragmentObject;
    [Tooltip("Duration in seconds to show the fragment")]
    public float fragmentDisplayDuration = 2f;
    
    [Header("Finish Sequence Settings")]
    [Tooltip("Audio file name for finish sequence (in StreamingAssets/audio/)")]
    public string finishAudioName = "FinishNarrative";
    [Tooltip("Laia image name for finish sequence (in StreamingAssets/System/LaiaImage/)")]
    public string finishLaiaImageName = "LaiaHappy";
    [Tooltip("Scene name to load after finish sequence")]
    public string nextSceneName = "";
    
    [Header("Sequence Control")]
    public bool playOnStart = false;
    public bool loopSequence = false;
    
    // Base Folder for the scanned RFID
    public string folderNameForCurrentRFID {get; private set;}
    
    // Handler components
    private AudioHandler audioHandler;
    private ImageHandler imageHandler;
    private TextHandler textHandler;
    private TriviaHandler triviaHandler;
    private LaiaHandler laiaHandler;
    private FragmentDisplayHandler fragmentDisplayHandler;
    private FinishSequenceHandler finishSequenceHandler;
    
    // List of parsed instructions
    private List<SequenceInstruction> instructions = new List<SequenceInstruction>();
    private int currentInstructionIndex = 0;
    private bool isPlaying = false;
    private Coroutine currentSequenceCoroutine;
    
    // Events
    public System.Action<SequenceInstruction> OnInstructionStart;
    public System.Action<SequenceInstruction> OnInstructionComplete;
    public System.Action OnSequenceComplete;
    
    void Start()
    {
        // Initialize handlers
        InitializeHandlers();
        
        // Load and parse the sequence file
        LoadSequence();

        // Auto-play if enabled
        if (playOnStart && instructions.Count > 0)
        {
            StartSequence();
        }
    }
    
    /// <summary>
    /// Initialize all handler components
    /// </summary>
    private void InitializeHandlers()
    {
        // Initialize AudioHandler
        audioHandler = gameObject.AddComponent<AudioHandler>();
        audioHandler.Initialize(audioSource);
        
        // Initialize ImageHandler
        imageHandler = gameObject.AddComponent<ImageHandler>();
        imageHandler.Initialize(targetImage, targetSpriteImage);
        
        // Initialize TextHandler
        textHandler = gameObject.AddComponent<TextHandler>();
        textHandler.Initialize(targetText);
        
        // Initialize FragmentDisplayHandler (needed before TriviaHandler)
        fragmentDisplayHandler = gameObject.AddComponent<FragmentDisplayHandler>();
        fragmentDisplayHandler.Initialize(fragmentObject, fragmentDisplayDuration);
        
        // Initialize TriviaHandler with fragment callback (only for visual display)
        triviaHandler = gameObject.AddComponent<TriviaHandler>();
        triviaHandler.Initialize(triviaCanvas, triviaQuestionText, 
                                 triviaAnswer1Button, triviaAnswer2Button, triviaAnswer3Button,
                                 correctAnswerAudio, incorrectAnswerAudio,
                                 OnFragmentFound);
        
        // Initialize LaiaHandler
        laiaHandler = gameObject.AddComponent<LaiaHandler>();
        laiaHandler.Initialize(laiaObject, laiaImage, laiaPositions);
        laiaHandler.InitializeImage(); // Initialize Laia image to LaiaHappy
        
        // Initialize FinishSequenceHandler
        finishSequenceHandler = gameObject.AddComponent<FinishSequenceHandler>();
        finishSequenceHandler.Initialize(audioHandler, imageHandler, textHandler, laiaHandler,
                                        () => audioHandler != null && audioHandler.IsPlaying(),
                                        () => audioHandler.WaitForAudioToFinish());
        finishSequenceHandler.SetFinishConfig(finishAudioName, finishLaiaImageName, nextSceneName);
    }
    
    /// <summary>
    /// Called when a fragment is found (trivia answered correctly)
    /// Only shows visual representation, no tracking
    /// </summary>
    private void OnFragmentFound()
    {
        fragmentDisplayHandler?.ShowFragment();
    }
    
    /// <summary>
    /// Loads and parses the sequence file from StreamingAssets
    /// </summary>
    public void LoadSequence()
    {
        if (ArduinoConnector.Instance)
        folderNameForCurrentRFID = ArduinoConnector.Instance.Data["RFID"];

        instructions.Clear();
        
        string filePath;
        if (initializeSecuenceDirectly)
        {
            filePath = Path.Combine(Application.streamingAssetsPath, sequenceFileName + ".txt");
        }else
        {
            filePath = Path.Combine(Application.streamingAssetsPath, folderNameForCurrentRFID + "/secuence.txt");
        }
        
        
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
                if (!initializeSecuenceDirectly)
                {
                    string modifiedResourcePath = Path.Combine(Application.streamingAssetsPath, folderNameForCurrentRFID + "/" + instruction.resourcePath);
                    instruction.resourcePath = modifiedResourcePath;
                }
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
        if (audioHandler != null)
        {
            audioHandler.Stop();
        }
        
        // Hide trivia if active
        if (triviaHandler != null)
        {
            triviaHandler.Hide();
        }
    }
    
    /// <summary>
    /// Pauses the sequence
    /// </summary>
    public void PauseSequence()
    {
        isPlaying = false;
        if (audioHandler != null)
        {
            audioHandler.Pause();
        }
    }
    
    /// <summary>
    /// Resumes the sequence
    /// </summary>
    public void ResumeSequence()
    {
        isPlaying = true;
        if (audioHandler != null)
        {
            audioHandler.UnPause();
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
        
        // Only proceed if we actually completed all instructions (not stopped early)
        if (currentInstructionIndex >= instructions.Count && isPlaying)
        {
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
            else
            {
                // Start finish sequence only after all instructions are complete
                yield return StartCoroutine(finishSequenceHandler.PlayFinishSequence());
            }
        }
        else
        {
            // Sequence was stopped or paused, don't run finish sequence
            isPlaying = false;
        }
    }
    
    /// <summary>
    /// Executes a single instruction using the appropriate handler
    /// </summary>
    private IEnumerator ExecuteInstruction(SequenceInstruction instruction)
    {
        switch (instruction.type)
        {
            case InstructionType.Audio:
                // For audio: wait for previous audio to finish, then start new one (non-blocking)
                // Automatically move Laia to next position when audio starts
                yield return StartCoroutine(audioHandler.PlayAudio(instruction.resourcePath, 
                    () => laiaHandler?.MoveToNextPosition()));
                break;
                
            case InstructionType.Image:
                if (instruction.isCleanCommand)
                {
                    imageHandler?.Clean();
                }
                else
                {
                    imageHandler?.ShowImage(instruction.resourcePath);
                }
                break;
                
            case InstructionType.Wait:
                yield return new WaitForSeconds(instruction.waitDuration);
                break;
                
            case InstructionType.Text:
                if (instruction.isCleanCommand)
                {
                    textHandler?.Clean();
                }
                else
                {
                    textHandler?.ShowText(instruction.displayText);
                }
                break;
                
            case InstructionType.Action:
                ExecuteAction(instruction);
                break;
                
            case InstructionType.Trivia:
                // Trivia: wait for any audio to finish, then show trivia and wait for correct answer
                yield return StartCoroutine(triviaHandler.ShowTrivia(instruction,
                    () => audioHandler != null && audioHandler.IsPlaying(),
                    () => audioHandler.WaitForAudioToFinish()));
                break;
                
            case InstructionType.LaiaImage:
                if (instruction.isCleanCommand)
                {
                    // Clean Laia image
                    laiaHandler?.Clean();
                }
                else
                {
                    // Change Laia's facial expression
                    laiaHandler?.ChangeImage(instruction.laiaImageName);
                }
                break;
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
