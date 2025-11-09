using UnityEngine;

[System.Serializable]
public enum InstructionType
{
    Audio,      // Audio: {path}/audioName.mp3
    Image,      // Image: {path}/imageName.png
    Wait,       // Wait: 2.5
    Text,       // Text: "Display this text"
    Action,     // Action: actionName
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
        
        // Determine instruction type
        switch (typeString.ToLower())
        {
            case "audio":
                type = InstructionType.Audio;
                resourcePath = ProcessResourcePath(content);
                break;
                
            case "image":
                type = InstructionType.Image;
                resourcePath = ProcessResourcePath(content);
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
                displayText = content.Trim('"', '\''); // Remove quotes if present
                break;
                
            case "action":
                type = InstructionType.Action;
                resourcePath = content;
                break;
                
            default:
                Debug.LogWarning($"Unknown instruction type: {typeString}");
                type = InstructionType.Unknown;
                break;
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

