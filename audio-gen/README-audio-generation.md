# Multiplication Table Audio Generation

This script generates audio files for multiplication tables (0x0 through 12x12) using the ElevenLabs Text-to-Speech API.

## Setup

1. **Install Node.js dependencies:**
   ```bash
   npm install
   ```

2. **Set up your ElevenLabs API key:**
   Create a `.env` file in the root directory with your API key:
   ```
   ELEVENLABS_API_KEY=your_actual_api_key_here
   ```

3. **Run the generation script:**
   ```bash
   npm run generate
   ```

## Output

The script will generate MP3 files in the format `{first_number}x{second_number}.mp3` and save them to:
`Assets/ReusablePatterns/FluencySDK/Sounds/fluency/`

For example:
- `0x0.mp3` - "zero times zero"
- `1x5.mp3` - "one times five"
- `12x12.mp3` - "twelve times twelve"

## Features

- **Smart skipping**: Won't regenerate files that already exist
- **Rate limiting**: Includes delays to avoid API rate limits
- **Error handling**: Continues generation even if individual files fail
- **Progress tracking**: Shows which files are being generated vs skipped

## Configuration

You can modify the following in `generate-audio.js`:
- `VOICE_ID`: Change the voice used for generation
- `MODEL_ID`: Change the TTS model
- `OUTPUT_FORMAT`: Change audio format/quality
- Delay between requests (currently 500ms)

## Total Files

The script will generate 169 audio files total (13 x 13 combinations from 0-12). 