# Correction Audio Generation

This system generates audio files for providing feedback when students get multiplication problems wrong.

## Overview

The correction audio system consists of two types of files:

### 1. Prefix Audio Files (2 files)
- `correction-oops.mp3` - "Oops!"
- `correction-not-quite.mp3` - "Not quite!"

### 2. Correction Answer Audio Files (169 files)
- Format: `correction-{first}x{second}.mp3`
- Content: "{first number} times {second number} is {result}"
- Examples:
  - `correction-2x3.mp3` - "two times three is six"
  - `correction-7x8.mp3` - "seven times eight is fifty six"
  - `correction-0x5.mp3` - "zero times five is zero"

## Usage

When a student gets a multiplication problem wrong:

1. **Randomly select a prefix** from the two available options
2. **Play the correction sequence**:
   - First: Play the random prefix (e.g., "Oops!")
   - Then: Play the specific correction (e.g., "two times three is six")

This provides variety while ensuring students hear the correct answer.

## File Location

All files are generated in:
`./game_skeletons/SubwaySurfers/Assets/ReusablePatterns/FluencySDK/Sounds/fluency/corrections/`

## Generation

To generate or regenerate the correction audio files:

```bash
npm run generate-corrections
```

## Implementation Notes

- **Voice consistency**: Uses the same ElevenLabs voice as the main multiplication audio
- **Rate limiting**: Includes 500ms delays between API calls to avoid rate limits
- **Smart skipping**: Won't regenerate files that already exist
- **Complete coverage**: Covers all multiplication problems from 0x0 to 12x12
- **Natural pronunciation**: Uses word-based number pronunciation for better clarity

## Total Files Generated

- 2 prefix files
- 169 correction answer files (13 x 13 combinations from 0-12)
- **Total: 171 files**

## Example Usage in Game Code

```javascript
// Pseudocode for game integration
function playCorrection(firstNum, secondNum) {
  // Randomly select prefix
  const randomPrefix = Math.random() < 0.5 ? 
    "correction-oops.mp3" : 
    "correction-not-quite.mp3";
    
  // Get correction audio
  const correctionAudio = `correction-${firstNum}x${secondNum}.mp3`;
  
  // Play sequence with slight delay
  playAudio(randomPrefix);
  setTimeout(() => playAudio(correctionAudio), 1000);
}
``` 