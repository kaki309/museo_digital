using UnityEngine;

[System.Serializable]
public enum InstructionType
{
    Audio,      // Audio: {path}/audioName.mp3
    Image,      // Image: {path}/imageName.png
    Wait,       // Wait: 2.5
    Text,       // Text: "Display this text"
    Action,     // Action: actionName
    Trivia,     // Trivia: "Question"|"Answer1"|"Answer2"|"Answer3"|correctIndex
    LaiaImage,    // LaiaImage: LaiaHappy (image name from StreamingAssets/System/LaiaImage)
    Unknown     // Invalid instruction
}

[System.Serializable]
public class SequenceInstruction
{
    public InstructionType type;
    public string content;      // The full content after the type indicator
    public string resourcePath; // Extracted resource path
    public float waitDuration;  // For Wait instructions
    public string displayText;  // For Text instructions
    public bool isCleanCommand; // True if instruction is a "clean" command (e.g., "Image: clean")
    
    // Trivia data
    public string triviaQuestion;      // The trivia question
    public string[] triviaAnswers;     // Array of 3 answers
    public int correctAnswerIndex;     // Index of correct answer (0, 1, or 2)
    
    // Laia data
    public string laiaImageName;       // Image name for Laia's facial expression
    
    public SequenceInstruction(string line)
    {
        ParseLine(line);
    }
    
    private void ParseLine(string line)
    {
        // Remove leading/trailing whitespace
        line = line.Trim();
        
        // Skip empty lines and comments (lines starting with #)
        if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
        {
            type = InstructionType.Unknown;
            return;
        }
        
        // Find the colon separator
        int colonIndex = line.IndexOf(':');
        if (colonIndex == -1)
        {
            Debug.LogWarning($"Invalid instruction format (no colon found): {line}");
            type = InstructionType.Unknown;
            return;
        }
        
        // Extract type and content
        string typeString = line.Substring(0, colonIndex).Trim();
        content = line.Substring(colonIndex + 1).Trim();
        
        // Remove inline comments (everything after #)
        int commentIndex = content.IndexOf('#');
        if (commentIndex != -1)
        {
            content = content.Substring(0, commentIndex).Trim();
        }
        
        // Check if this is a "clean" command
        isCleanCommand = content.ToLower() == "clean";
        
        // Determine instruction type
        switch (typeString.ToLower())
        {
            case "audio":
                type = InstructionType.Audio;
                if (!isCleanCommand)
                {
                    resourcePath = ProcessResourcePath(content);
                }
                break;
                
            case "image":
                type = InstructionType.Image;
                if (!isCleanCommand)
                {
                    resourcePath = ProcessResourcePath(content);
                }
                break;
                
            case "wait":
                type = InstructionType.Wait;
                if (float.TryParse(content, out waitDuration))
                {
                    // Parsed successfully
                }
                else
                {
                    Debug.LogWarning($"Invalid wait duration: {content}");
                    waitDuration = 0f;
                }
                break;
                
            case "text":
                type = InstructionType.Text;
                if (!isCleanCommand)
                {
                    displayText = content.Trim('"', '\''); // Remove quotes if present
                }
                break;
                
            case "action":
                type = InstructionType.Action;
                resourcePath = content;
                break;
                
            case "trivia":
                type = InstructionType.Trivia;
                ParseTriviaData(content);
                break;
                
            case "laiaimage":
                type = InstructionType.LaiaImage;
                if (!isCleanCommand)
                {
                    laiaImageName = content.Trim().Trim('"', '\'');
                }
                break;
                
            default:
                Debug.LogWarning($"Unknown instruction type: {typeString}");
                type = InstructionType.Unknown;
                break;
        }
    }
    
    /// <summary>
    /// Parses trivia data from format: "Question"|"Answer1"|"Answer2"|"Answer3"|correctIndex
    /// </summary>
    private void ParseTriviaData(string triviaContent)
    {
        triviaAnswers = new string[3];
        correctAnswerIndex = 0;
        
        // Split by pipe character
        string[] parts = triviaContent.Split('|');
        
        if (parts.Length < 5)
        {
            Debug.LogError($"Invalid trivia format. Expected: \"Question\"|\"Answer1\"|\"Answer2\"|\"Answer3\"|correctIndex. Got: {triviaContent}");
            type = InstructionType.Unknown;
            return;
        }
        
        // Extract question (remove quotes if present)
        triviaQuestion = parts[0].Trim().Trim('"', '\'');
        
        // Extract answers (remove quotes if present)
        for (int i = 0; i < 3 && i < parts.Length - 1; i++)
        {
            triviaAnswers[i] = parts[i + 1].Trim().Trim('"', '\'');
        }
        
        // Extract correct answer index
        if (int.TryParse(parts[4].Trim(), out int correctIndex))
        {
            // Convert from 1-based (user input) to 0-based (array index)
            if (correctIndex >= 1 && correctIndex <= 3)
            {
                correctAnswerIndex = correctIndex - 1;
            }
            else
            {
                Debug.LogWarning($"Invalid correct answer index: {correctIndex}. Must be 1, 2, or 3. Defaulting to 1.");
                correctAnswerIndex = 0;
            }
        }
        else
        {
            Debug.LogWarning($"Failed to parse correct answer index: {parts[4]}. Defaulting to 1.");
            correctAnswerIndex = 0;
        }
    }
    
    private string ProcessResourcePath(string path)
    {
        // Process {this.file.route} placeholder if present
        // For now, we'll just remove it or replace it
        // You can customize this based on your needs
        path = path.Replace("{this.file.route}/", "");
        path = path.Replace("{this.file.route}", "");
        
        // Remove file extension for Resources.Load (Unity doesn't need extension)
        int lastDot = path.LastIndexOf('.');
        if (lastDot != -1)
        {
            path = path.Substring(0, lastDot);
        }
        
        // Normalize path separators
        path = path.Replace('\\', '/');
        
        return path;
    }
    
    public override string ToString()
    {
        return $"[{type}] {content}";
    }
}

