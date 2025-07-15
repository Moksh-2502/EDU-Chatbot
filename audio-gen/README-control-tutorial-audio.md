# Control Tutorial Audio Generation

This system generates audio files for teaching players game controls in tutorial mode.

## Overview

The control tutorial audio system provides instruction audio for both keyboard and touch controls:

### Keyboard Controls (4 files)
- `keyboard-move-right.mp3` - "Press right arrow or D to move right"
- `keyboard-jump.mp3` - "Press up arrow or W to jump"
- `keyboard-duck.mp3` - "Press down arrow or S to duck"
- `keyboard-move-left.mp3` - "Press left arrow or A to move left"

### Touch/Mobile Controls (4 files)
- `touch-move-right.mp3` - "Swipe right to move right"
- `touch-jump.mp3` - "Swipe up to jump"
- `touch-duck.mp3` - "Swipe down to duck"
- `touch-move-left.mp3` - "Swipe left to move left"

## Usage

During the tutorial phase, the game can:

1. **Detect platform** (desktop/keyboard vs mobile/touch)
2. **Play appropriate control instructions** based on the control scheme
3. **Guide players through each action** with clear audio cues

## File Location

All files are generated in:
`./game_skeletons/SubwaySurfers/Assets/ReusablePatterns/FluencySDK/Sounds/fluency/tutorial/`

## Generation

To generate or regenerate the control tutorial audio files:

```bash
npm run generate-controls
```

## Implementation Notes

- **Platform awareness**: Separate files for keyboard and touch controls
- **Voice consistency**: Uses the same ElevenLabs voice as other game audio
- **Clear instructions**: Simple, direct language for each control action
- **Action-specific**: Each file focuses on one specific control/action
- **Rate limiting**: Includes 500ms delays between API calls

## Total Files Generated

- 4 keyboard control files
- 4 touch control files
- **Total: 8 files**

## Example Usage in Game Code

```javascript
// Pseudocode for tutorial integration
function playControlInstruction(action, isMobile = false) {
  const platform = isMobile ? "touch" : "keyboard";
  const audioFile = `${platform}-${action}.mp3`;
  
  // Examples:
  // playControlInstruction("move-right", false) → "keyboard-move-right.mp3"
  // playControlInstruction("jump", true) → "touch-jump.mp3"
  
  playAudio(audioFile);
}

// Tutorial sequence example
async function runMovementTutorial() {
  const isMobile = detectMobile();
  
  playControlInstruction("move-right", isMobile);
  await waitForPlayerAction("move-right");
  
  playControlInstruction("jump", isMobile);
  await waitForPlayerAction("jump");
  
  playControlInstruction("duck", isMobile);
  await waitForPlayerAction("duck");
  
  playControlInstruction("move-left", isMobile);
  await waitForPlayerAction("move-left");
}
```

## Control Mapping

| Action | Keyboard | Touch |
|--------|----------|-------|
| Move Right | Right Arrow / D | Swipe Right |
| Jump | Up Arrow / W | Swipe Up |
| Duck | Down Arrow / S | Swipe Down |
| Move Left | Left Arrow / A | Swipe Left | 