# Wrong Answer Audio Generation

This script generates audio files for wrong answer feedback in the game.

## Features

### Wrong Answer Phrases (12 variations)
Used when a player answers a question incorrectly:
- "Oops!"
- "Not quite yet!"
- "So close!"
- "Nope, try again!"
- "Give it another shot!"
- "Missed that one!"
- "Almost there!"
- "That's off!"
- "Don't give up!"
- "Try once more!"
- "Keep at it!"
- "Better luck next time!"

## Usage

### Prerequisites
1. Make sure you have a `.env` file with your ElevenLabs API key:
   ```
   ELEVENLABS_API_KEY=your_api_key_here
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

### Generate Wrong Answer Audio Files
```bash
npm run generate-wrong-answers
```

## Output

Audio files are generated in:
```
./game_skeletons/SubwaySurfers/Assets/ReusablePatterns/FluencySDK/Sounds/fluency/wrong_no_correction/
```

### File Naming Convention

- `wrong-oops.mp3`
- `wrong-not-quite-yet.mp3`
- `wrong-so-close.mp3`
- `wrong-nope-try-again.mp3`
- `wrong-give-it-another-shot.mp3`
- `wrong-missed-that-one.mp3`
- `wrong-almost-there.mp3`
- `wrong-thats-off.mp3`
- `wrong-dont-give-up.mp3`
- `wrong-try-once-more.mp3`
- `wrong-keep-at-it.mp3`
- `wrong-better-luck-next-time.mp3`

## Implementation in Game

### Basic Usage
```javascript
// For incorrect answers - randomly pick one
const wrongAnswerPhrases = [
  'wrong-oops.mp3',
  'wrong-not-quite-yet.mp3',
  'wrong-so-close.mp3',
  // ... all wrong answer files
];

const randomWrongAnswer = wrongAnswerPhrases[Math.floor(Math.random() * wrongAnswerPhrases.length)];
playAudio(randomWrongAnswer);
```

## Voice Configuration

- **Voice ID**: `XrExE9yKIg1WjnnlVkGX` (same as other audio files)
- **Model**: `eleven_multilingual_v2`
- **Format**: `mp3_44100_128`
- **Rate limiting**: 700ms delay between generations to avoid API limits

## Notes

- The script automatically skips files that already exist
- Files are generated with a 700ms delay between requests to respect API rate limits
- The same voice is used across all audio files for consistency
- These phrases are intended for wrong answers without correction audio (hence "wrong_no_correction" directory) 