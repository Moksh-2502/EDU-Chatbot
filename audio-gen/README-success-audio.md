# Success Audio Generation

This script generates audio files for correct answer feedback and streak bonuses in the game.

## Features

### Regular Success Phrases (12 variations)
Used when a player answers a question correctly:
- "You got it!"
- "Right on!"
- "Spot on!"
- "That's it!"
- "Bullseye!"
- "Perfect!"
- "Nailed that one!"
- "Bravo!"
- "Excellent!"
- "Yes indeed!"
- "Absolutely!"
- "Gotcha!"

### Streak Bonus Phrases (12 variations)
Used when a player is on a successful streak:
- "You're on fire!"
- "Unstoppable!"
- "Legendary!"
- "Keep it rolling!"
- "Crushing it!"
- "Hot streak!"
- "Masterclass!"
- "Epic run!"
- "Next level!"
- "Unbelievable!"
- "Stellar streak!"
- "Dominating!"

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

### Generate Success Audio Files
```bash
npm run generate-success
```

## Output

Audio files are generated in separate directories:

**Success files:**
```
./game_skeletons/SubwaySurfers/Assets/ReusablePatterns/FluencySDK/Sounds/fluency/success/
```

**Streak files:**
```
./game_skeletons/SubwaySurfers/Assets/ReusablePatterns/FluencySDK/Sounds/fluency/streak/
```

### File Naming Convention

**Regular Success Files:**
- `success-you-got-it.mp3`
- `success-right-on.mp3`
- `success-spot-on.mp3`
- `success-thats-it.mp3`
- `success-bullseye.mp3`
- `success-perfect.mp3`
- `success-nailed-that-one.mp3`
- `success-bravo.mp3`
- `success-excellent.mp3`
- `success-yes-indeed.mp3`
- `success-absolutely.mp3`
- `success-gotcha.mp3`

**Streak Bonus Files:**
- `streak-on-fire.mp3`
- `streak-unstoppable.mp3`
- `streak-legendary.mp3`
- `streak-keep-it-rolling.mp3`
- `streak-crushing-it.mp3`
- `streak-hot-streak.mp3`
- `streak-masterclass.mp3`
- `streak-epic-run.mp3`
- `streak-next-level.mp3`
- `streak-unbelievable.mp3`
- `streak-stellar-streak.mp3`
- `streak-dominating.mp3`

## Implementation in Game

### Basic Usage
```javascript
// For regular correct answers - randomly pick one
const successPhrases = [
  'success-you-got-it.mp3',
  'success-right-on.mp3',
  'success-spot-on.mp3',
  // ... all success files
];

const randomSuccess = successPhrases[Math.floor(Math.random() * successPhrases.length)];
playAudio(randomSuccess);
```

### Streak Usage
```javascript
// For streak bonuses - randomly pick one
const streakPhrases = [
  'streak-on-fire.mp3',
  'streak-unstoppable.mp3',
  'streak-legendary.mp3',
  // ... all streak files
];

const randomStreak = streakPhrases[Math.floor(Math.random() * streakPhrases.length)];
playAudio(randomStreak);
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