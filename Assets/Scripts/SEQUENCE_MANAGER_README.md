# SequenceManager Documentation

## Overview
The `SequenceManager` is a system that reads a `.txt` file and executes a sequence of instructions to create a sequential narrative. It can play audio, display images, show text, wait for durations, and execute custom actions.

## Setup

### 1. Folder Structure
- Create a `StreamingAssets` folder in your `Assets` directory (if it doesn't exist)
- Place your sequence `.txt` files in the `StreamingAssets` folder
- Place audio files in `Assets/Resources/` or `StreamingAssets/`
- Place image files in `Assets/Resources/` or `StreamingAssets/`

### 2. Unity Setup
1. Add the `SequenceManager` script to a GameObject in your scene
2. Assign the required components:
   - **AudioSource**: For playing audio clips
   - **RawImage** or **Image**: For displaying images
   - **TextMeshProUGUI**: For displaying text (optional)

### 3. Configuration
- **Sequence File Name**: Name of your `.txt` file (without extension)
- **Play On Start**: Automatically start the sequence when the scene loads
- **Loop Sequence**: Automatically restart the sequence when it completes

## Sequence File Format

Each line in the sequence file represents one instruction. The format is:

```
InstructionType: content
```

### Supported Instruction Types

#### 1. Audio
Plays an audio file.
```
Audio: {this.file.route}/audio/narration1
```
- The path should be relative to `Resources/` folder (without file extension)
- Or place in `StreamingAssets/` and use full path
- Supported formats: `.wav`, `.mp3`, `.ogg`

#### 2. Image
Displays an image.
```
Image: {this.file.route}/images/welcome
```
- The path should be relative to `Resources/` folder (without file extension)
- Or place in `StreamingAssets/` and it will be loaded directly
- Supported formats: `.png`, `.jpg`

#### 3. Wait
Waits for a specified duration in seconds.
```
Wait: 2.5
```

#### 4. Text
Displays text on screen.
```
Text: "Welcome to the Museum"
```
- Text can be wrapped in quotes (optional)

#### 5. Action
Executes a custom action (extensible).
```
Action: showNextExhibit
```
- This is a placeholder for custom actions you can implement

### Special Notes

- **Comments**: Lines starting with `#` are ignored
- **Empty Lines**: Empty lines are ignored
- **Path Processing**: The `{this.file.route}/` placeholder is automatically removed
- **File Extensions**: Don't include file extensions in the path for Resources folder

## Example Sequence File

```
# Welcome sequence
Image: images/welcome
Wait: 2
Text: "Welcome to the Digital Museum"
Wait: 1.5
Audio: audio/narration1
Image: images/exhibit1
Wait: 3
Audio: audio/narration2
Text: "This artifact dates back to 500 BC"
Wait: 2
Action: showNextExhibit
Image: images/thankYou
Wait: 2
```

## Usage in Code

### Basic Usage
```csharp
SequenceManager sequenceManager = GetComponent<SequenceManager>();

// Start the sequence
sequenceManager.StartSequence();

// Stop the sequence
sequenceManager.StopSequence();

// Pause/Resume
sequenceManager.PauseSequence();
sequenceManager.ResumeSequence();

// Jump to specific instruction
sequenceManager.JumpToInstruction(5);

// Check status
bool isPlaying = sequenceManager.IsPlaying();
int currentIndex = sequenceManager.GetCurrentInstructionIndex();
```

### Events
You can subscribe to events to track sequence progress:
```csharp
sequenceManager.OnInstructionStart += (instruction) => {
    Debug.Log($"Starting: {instruction.type}");
};

sequenceManager.OnInstructionComplete += (instruction) => {
    Debug.Log($"Completed: {instruction.type}");
};

sequenceManager.OnSequenceComplete += () => {
    Debug.Log("Sequence finished!");
};
```

## File Organization

### Option 1: Using Resources Folder (Recommended for small files)
```
Assets/
  Resources/
    audio/
      narration1.mp3
      narration2.mp3
    images/
      welcome.png
      exhibit1.png
  StreamingAssets/
    sequence.txt
```

### Option 2: Using StreamingAssets (Recommended for large files)
```
Assets/
  StreamingAssets/
    sequence.txt
    audio/
      narration1.mp3
      narration2.mp3
    images/
      welcome.png
      exhibit1.png
```

## Extending the System

### Adding Custom Actions
Modify the `ExecuteAction` method in `SequenceManager.cs`:
```csharp
private void ExecuteAction(SequenceInstruction instruction)
{
    switch (instruction.resourcePath)
    {
        case "showNextExhibit":
            // Your custom logic here
            break;
        case "changeScene":
            // Scene change logic
            break;
    }
}
```

### Adding New Instruction Types
1. Add a new type to the `InstructionType` enum in `SequenceInstruction.cs`
2. Add parsing logic in the `ParseLine` method
3. Add execution logic in `ExecuteInstruction` method in `SequenceManager.cs`

## Troubleshooting

### File Not Found
- Make sure the `.txt` file is in the `StreamingAssets` folder
- Check that the file name (without extension) matches the `sequenceFileName` in the inspector
- Build and run the game - `StreamingAssets` folder is copied to the build

### Audio/Image Not Loading
- For Resources: Make sure files are in `Assets/Resources/` and paths don't include file extensions
- For StreamingAssets: Check file paths are correct and files exist
- Check Unity console for error messages

### Sequence Not Playing
- Make sure `Play On Start` is enabled or call `StartSequence()` manually
- Verify that the sequence file was loaded (check console for "Loaded X instructions")
- Check that at least one valid instruction exists in the file

