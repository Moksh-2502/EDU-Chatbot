# Popup Tutorial Audio Generation

This system generates audio files for instructing players how to interact with tutorial question popups.

## Overview

The popup tutorial audio system provides platform-specific instructions for answering questions in tutorial mode:

### Generated Files (2 files)
- `popup-desktop-instructions.mp3` - "Click a button or use arrow keys and Enter to select your answer"
- `popup-mobile-instructions.mp3` - "Tap a button to select your answer"

## Usage

When showing tutorial question popups, the game can:

1. **Detect platform** (desktop/keyboard vs mobile/touch)
2. **Play appropriate instruction audio** to guide the player
3. **Help players understand interaction methods** before they attempt to answer

## File Location

Files are generated in:
`E:\Crossover\AIProjects\ai-edu-chatbot\game_skeletons\SubwaySurfers\Assets\Sounds\tutorial\`

## Generation

To generate or regenerate the popup tutorial audio files:

```bash
npm run generate-popup
```

## Implementation Notes

- **Platform-specific instructions**: Clear guidance for different input methods
- **Voice consistency**: Uses the same ElevenLabs voice as other game audio
- **Concise messaging**: Short, direct instructions to avoid overwhelming players
- **Accessibility**: Covers both mouse/keyboard and touch interaction methods
- **Rate limiting**: Includes 500ms delays between API calls

## Total Files Generated

- 1 desktop instruction file
- 1 mobile instruction file
- **Total: 2 files**

## Example Usage in Game Code

```javascript
// Pseudocode for popup tutorial integration
function showQuestionPopup(question, answers) {
  // Show the popup UI
  displayPopup(question, answers);
  
  // Play platform-appropriate instruction
  const isMobile = detectMobile();
  const instructionFile = isMobile ? 
    "popup-mobile-instructions.mp3" : 
    "popup-desktop-instructions.mp3";
    
  playAudio(instructionFile);
  
  // Wait for player interaction
  waitForAnswer();
}

// Alternative: Play instructions only on first popup
let hasPlayedPopupInstructions = false;

function showQuestionPopupWithInstructions(question, answers) {
  displayPopup(question, answers);
  
  if (!hasPlayedPopupInstructions) {
    const isMobile = detectMobile();
    const instructionFile = isMobile ? 
      "popup-mobile-instructions.mp3" : 
      "popup-desktop-instructions.mp3";
      
    playAudio(instructionFile);
    hasPlayedPopupInstructions = true;
  }
  
  waitForAnswer();
}
```

## Interaction Methods

| Platform | Interaction Methods |
|----------|-------------------|
| Desktop | • Click buttons with mouse<br>• Use arrow keys to highlight + Enter to select |
| Mobile | • Tap buttons on screen |

## Integration Notes

- Consider playing instructions only on the first popup to avoid repetition
- Instructions can be triggered manually if player seems confused
- Audio provides accessibility for players who might miss visual cues
- Short duration keeps tutorial flow smooth 