# MP3 to WAV Converter

A Node.js script to convert all MP3 files in a directory to WAV format using FFmpeg.

## Prerequisites

1. **Node.js** (version 14 or higher)
2. **FFmpeg** must be installed on your system:
   - **Windows**: Download from [https://ffmpeg.org/download.html](https://ffmpeg.org/download.html) or install via chocolatey: `choco install ffmpeg`
   - **macOS**: Install via Homebrew: `brew install ffmpeg`
   - **Linux**: Install via package manager: `sudo apt install ffmpeg` (Ubuntu/Debian) or `sudo yum install ffmpeg` (CentOS/RHEL)

## Installation

Install the required dependencies:

```bash
npm install
```

## Usage

### Command Line

```bash
node convert-mp3-to-wav.js <input-directory> [output-directory] [--flat] [--delete]
```

### Using npm script

```bash
npm run convert <input-directory> [output-directory] [--flat] [--delete]
```

### Arguments

- `input-directory`: Directory containing MP3 files to convert (required)
- `output-directory`: Directory to save WAV files (optional, defaults to input directory)
- `--flat`: Don't preserve directory structure (optional)
- `--delete`: Delete original MP3 files after successful conversion (optional)

### Examples

1. **Convert all MP3s in a directory (output to same directory):**
   ```bash
   node convert-mp3-to-wav.js ./audio-files
   ```

2. **Convert to a different output directory:**
   ```bash
   node convert-mp3-to-wav.js ./mp3-files ./wav-files
   ```

3. **Convert without preserving directory structure:**
   ```bash
   node convert-mp3-to-wav.js ./audio ./output --flat
   ```

4. **Convert and delete original MP3 files:**
   ```bash
   node convert-mp3-to-wav.js ./audio --delete
   ```

5. **Convert to different directory and delete originals:**
   ```bash
   node convert-mp3-to-wav.js ./mp3-files ./wav-files --delete
   ```

6. **Convert with all options (flat structure, delete originals):**
   ```bash
   node convert-mp3-to-wav.js ./audio ./output --flat --delete
   ```

7. **Convert the generated multiplication audio files:**
   ```bash
   node convert-mp3-to-wav.js "./game_skeletons/SubwaySurfers/Assets/ReusablePatterns/FluencySDK/Sounds/fluency"
   ```

## Features

- **Recursive directory scanning**: Finds all MP3 files in subdirectories
- **Preserve directory structure**: Maintains folder hierarchy in output
- **Skip existing files**: Won't overwrite existing WAV files
- **Progress tracking**: Shows conversion progress for each file
- **Error handling**: Continues processing even if individual files fail
- **High-quality output**: Converts to 16-bit PCM, 44.1kHz, stereo WAV format
- **Optional file deletion**: Can delete original MP3 files after successful conversion

## Output Format

The script converts MP3 files to WAV with the following specifications:
- **Format**: WAV
- **Codec**: PCM 16-bit signed little-endian
- **Sample Rate**: 44.1 kHz
- **Channels**: 2 (Stereo)

## Error Handling

- The script will continue processing even if individual files fail to convert
- Error messages are displayed for failed conversions
- A summary is shown at the end with success/error counts

## Programmatic Usage

You can also import and use the functions in your own Node.js code:

```javascript
import { convertAllMp3ToWav, convertMp3ToWav, getMp3Files } from './convert-mp3-to-wav.js';

// Convert all MP3s in a directory
await convertAllMp3ToWav('./input-dir', './output-dir');

// Convert all MP3s and delete originals
await convertAllMp3ToWav('./input-dir', './output-dir', true, true);

// Convert a single file
await convertMp3ToWav('./input.mp3', './output.wav');

// Convert a single file and delete original
await convertMp3ToWav('./input.mp3', './output.wav', true);

// Get list of MP3 files in a directory
const mp3Files = await getMp3Files('./audio-dir');
```

## Troubleshooting

### FFmpeg not found
If you get an error about FFmpeg not being found:
1. Make sure FFmpeg is installed on your system
2. Ensure FFmpeg is in your system PATH
3. Try running `ffmpeg -version` in your terminal to verify installation

### Permission errors
If you encounter permission errors:
1. Make sure you have read access to the input directory
2. Make sure you have write access to the output directory
3. On Unix systems, you might need to use `sudo` or change file permissions

### Memory issues with large files
For very large MP3 files, you might encounter memory issues. The script processes files one at a time to minimize memory usage, but extremely large files might still cause problems 