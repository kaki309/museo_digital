using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;

/// <summary>
/// Handles trivia UI and logic for the sequence manager
/// </summary>
public class TriviaHandler : MonoBehaviour
{
    private GameObject triviaCanvas;
    private TMPro.TextMeshProUGUI triviaQuestionText;
    private Button triviaAnswer1Button;
    private Button triviaAnswer2Button;
    private Button triviaAnswer3Button;
    private AudioSource triviaAudioSource;
    
    private string correctAnswerAudio = "correct";
    private string incorrectAnswerAudio = "incorrect";
    
    // Trivia state tracking
    private bool isTriviaActive = false;
    private bool triviaAnswerSelected = false;
    private SequenceInstruction currentTriviaInstruction = null;
    
    // Fragment tracking callback
    private System.Action onFragmentFound;
    
    /// <summary>
    /// Initialize the trivia handler with required components
    /// </summary>
    public void Initialize(GameObject canvas, TMPro.TextMeshProUGUI questionText, 
                           Button answer1Button, Button answer2Button, Button answer3Button,
                           string correctAudio, string incorrectAudio, System.Action onFragmentFoundCallback = null)
    {
        triviaCanvas = canvas;
        triviaQuestionText = questionText;
        triviaAnswer1Button = answer1Button;
        triviaAnswer2Button = answer2Button;
        triviaAnswer3Button = answer3Button;
        correctAnswerAudio = correctAudio;
        incorrectAnswerAudio = incorrectAudio;
        onFragmentFound = onFragmentFoundCallback;
        
        // Initialize AudioSource from trivia canvas
        if (triviaCanvas != null)
        {
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
        
        // Setup button listeners
        SetupButtons();
        
        // Hide trivia canvas initially
        if (triviaCanvas != null)
        {
            triviaCanvas.SetActive(false);
        }
    }
    
    /// <summary>
    /// Sets up button listeners for trivia answers
    /// </summary>
    private void SetupButtons()
    {
        if (triviaAnswer1Button != null)
        {
            triviaAnswer1Button.onClick.RemoveAllListeners();
            triviaAnswer1Button.onClick.AddListener(() => OnAnswerSelected(0));
        }
        
        if (triviaAnswer2Button != null)
        {
            triviaAnswer2Button.onClick.RemoveAllListeners();
            triviaAnswer2Button.onClick.AddListener(() => OnAnswerSelected(1));
        }
        
        if (triviaAnswer3Button != null)
        {
            triviaAnswer3Button.onClick.RemoveAllListeners();
            triviaAnswer3Button.onClick.AddListener(() => OnAnswerSelected(2));
        }
    }
    
    /// <summary>
    /// Called when a trivia answer is selected
    /// </summary>
    private void OnAnswerSelected(int answerIndex)
    {
        if (!isTriviaActive || currentTriviaInstruction == null)
        {
            return;
        }
        
        // Check if the answer is correct
        if (answerIndex == currentTriviaInstruction.correctAnswerIndex)
        {
            // Correct answer - play correct audio and resume sequence
            PlayAudio(correctAnswerAudio);
            triviaAnswerSelected = true;
            isTriviaActive = false;
            
            // Notify that a fragment was found
            onFragmentFound?.Invoke();
        }
        else
        {
            // Wrong answer - play incorrect audio and let user try again
            PlayAudio(incorrectAnswerAudio);
            Debug.Log($"Wrong answer selected. Correct answer is: {currentTriviaInstruction.correctAnswerIndex + 1}");
        }
    }
    
    /// <summary>
    /// Shows trivia and waits for the correct answer to be selected
    /// Waits for any audio to finish before showing trivia
    /// </summary>
    public IEnumerator ShowTrivia(SequenceInstruction instruction, System.Func<bool> isAudioPlaying, System.Func<IEnumerator> waitForAudio)
    {
        // Wait for any audio to finish playing first
        if (isAudioPlaying != null && isAudioPlaying())
        {
            Debug.Log("Trivia: Waiting for audio to finish...");
            if (waitForAudio != null)
            {
                yield return StartCoroutine(waitForAudio());
            }
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
        SetupButtons();
        
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
        yield return new WaitForSeconds(3f);
        
        // Hide trivia canvas
        if (triviaCanvas != null)
        {
            triviaCanvas.SetActive(false);
        }
        
        // Clear trivia state
        isTriviaActive = false;
        currentTriviaInstruction = null;
        triviaAnswerSelected = false;
        
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
    /// Plays an audio file from StreamingAssets/System folder
    /// </summary>
    private void PlayAudio(string audioFileName)
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
        StartCoroutine(LoadAndPlayAudio(audioFileName));
    }
    
    /// <summary>
    /// Loads and plays an audio file from StreamingAssets/System folder
    /// </summary>
    private IEnumerator LoadAndPlayAudio(string audioFileName)
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
            if (System.IO.File.Exists(fullPath))
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
    /// Hide trivia canvas and clear state
    /// </summary>
    public void Hide()
    {
        if (triviaCanvas != null)
        {
            triviaCanvas.SetActive(false);
        }
        isTriviaActive = false;
        currentTriviaInstruction = null;
        triviaAnswerSelected = false;
    }
}

