import ffmpeg from 'fluent-ffmpeg';
import { readdir, stat, mkdir, unlink } from 'node:fs/promises';
import { existsSync } from 'node:fs';
import { join, extname, basename } from 'node:path';
import { fileURLToPath } from 'node:url';

/**
 * Convert a single MP3 file to WAV format
 * @param {string} inputPath - Path to the input MP3 file
 * @param {string} outputPath - Path for the output WAV file
 * @param {boolean} deleteOriginal - Whether to delete the original MP3 file after conversion
 * @returns {Promise<void>}
 */
async function convertMp3ToWav(inputPath, outputPath, deleteOriginal = false) {
  return new Promise((resolve, reject) => {
    ffmpeg(inputPath)
      .toFormat('wav')
      .audioCodec('pcm_s16le') // 16-bit PCM encoding
      .audioFrequency(44100) // 44.1kHz sample rate
      .audioChannels(2) // Stereo
      .on('start', (commandLine) => {
        console.log(`Converting: ${basename(inputPath)}`);
        console.log(`Command: ${commandLine}`);
      })
      .on('progress', (progress) => {
        if (progress.percent) {
          process.stdout.write(`\rProgress: ${Math.round(progress.percent)}%`);
        }
      })
      .on('end', async () => {
        console.log(
          `\n✓ Converted: ${basename(inputPath)} -> ${basename(outputPath)}`,
        );

        // Delete original MP3 file if requested
        if (deleteOriginal) {
          try {
            await unlink(inputPath);
            console.log(`🗑️  Deleted original: ${basename(inputPath)}`);
          } catch (deleteError) {
            console.error(
              `⚠️  Warning: Could not delete ${basename(inputPath)}:`,
              deleteError.message,
            );
          }
        }

        resolve();
      })
      .on('error', (err) => {
        console.error(
          `\n✗ Error converting ${basename(inputPath)}:`,
          err.message,
        );
        reject(err);
      })
      .save(outputPath);
  });
}

/**
 * Get all MP3 files in a directory recursively
 * @param {string} dirPath - Directory path to search
 * @returns {Promise<string[]>} Array of MP3 file paths
 */
async function getMp3Files(dirPath) {
  const mp3Files = [];

  try {
    const items = await readdir(dirPath);

    for (const item of items) {
      const fullPath = join(dirPath, item);
      const stats = await stat(fullPath);

      if (stats.isDirectory()) {
        // Recursively search subdirectories
        const subDirMp3s = await getMp3Files(fullPath);
        mp3Files.push(...subDirMp3s);
      } else if (stats.isFile() && extname(item).toLowerCase() === '.mp3') {
        mp3Files.push(fullPath);
      }
    }
  } catch (error) {
    console.error(`Error reading directory ${dirPath}:`, error.message);
  }

  return mp3Files;
}

/**
 * Convert all MP3 files in a directory to WAV format
 * @param {string} inputDir - Input directory containing MP3 files
 * @param {string} outputDir - Output directory for WAV files (optional, defaults to input directory)
 * @param {boolean} preserveStructure - Whether to preserve directory structure (default: true)
 * @param {boolean} deleteOriginal - Whether to delete original MP3 files after conversion (default: false)
 */
async function convertAllMp3ToWav(
  inputDir,
  outputDir = inputDir,
  preserveStructure = true,
  deleteOriginal = false,
) {
  console.log(`Starting MP3 to WAV conversion...`);
  console.log(`Input directory: ${inputDir}`);
  console.log(`Output directory: ${outputDir}`);
  console.log(`Preserve directory structure: ${preserveStructure}`);
  console.log(`Delete original MP3 files: ${deleteOriginal}`);
  console.log('');

  // Ensure output directory exists
  if (!existsSync(outputDir)) {
    await mkdir(outputDir, { recursive: true });
    console.log(`Created output directory: ${outputDir}`);
  }

  // Get all MP3 files
  const mp3Files = await getMp3Files(inputDir);

  if (mp3Files.length === 0) {
    console.log('No MP3 files found in the specified directory.');
    return;
  }

  console.log(`Found ${mp3Files.length} MP3 file(s) to convert.\n`);

  let successCount = 0;
  let errorCount = 0;
  let deletedCount = 0;

  // Convert each MP3 file
  for (let i = 0; i < mp3Files.length; i++) {
    const inputPath = mp3Files[i];
    let outputPath;

    if (preserveStructure && outputDir !== inputDir) {
      // Preserve directory structure
      const relativePath = inputPath.replace(inputDir, '');
      const outputRelativePath = relativePath.replace(/\.mp3$/i, '.wav');
      outputPath = join(outputDir, outputRelativePath);

      // Ensure subdirectory exists
      const outputSubDir = join(outputPath, '..');
      if (!existsSync(outputSubDir)) {
        await mkdir(outputSubDir, { recursive: true });
      }
    } else {
      // Place in output directory with same filename
      const filename = `${basename(inputPath, '.mp3')}.wav`;
      outputPath = join(outputDir, filename);
    }

    // Skip if WAV file already exists
    if (existsSync(outputPath)) {
      console.log(`Skipping ${basename(inputPath)} - WAV file already exists`);
      continue;
    }

    try {
      console.log(`\n[${i + 1}/${mp3Files.length}]`);
      await convertMp3ToWav(inputPath, outputPath, deleteOriginal);
      successCount++;
      if (deleteOriginal) {
        deletedCount++;
      }
    } catch (error) {
      errorCount++;
    }

    // Add a small delay between conversions
    if (i < mp3Files.length - 1) {
      await new Promise((resolve) => setTimeout(resolve, 100));
    }
  }

  console.log(`\n${'='.repeat(50)}`);
  console.log(`Conversion completed!`);
  console.log(`Successfully converted: ${successCount} files`);
  console.log(`Errors: ${errorCount} files`);
  if (deleteOriginal) {
    console.log(`Original MP3 files deleted: ${deletedCount} files`);
  }
  console.log(`Total processed: ${mp3Files.length} files`);
}

/**
 * Main function to handle command line arguments
 */
async function main() {
  const args = process.argv.slice(2);

  if (args.length === 0) {
    console.log(
      'Usage: node convert-mp3-to-wav.js <input-directory> [output-directory] [--flat] [--delete]',
    );
    console.log('');
    console.log('Arguments:');
    console.log(
      '  input-directory   Directory containing MP3 files to convert',
    );
    console.log(
      '  output-directory  Directory to save WAV files (optional, defaults to input directory)',
    );
    console.log(
      "  --flat           Don't preserve directory structure (optional)",
    );
    console.log(
      '  --delete         Delete original MP3 files after successful conversion (optional)',
    );
    console.log('');
    console.log('Examples:');
    console.log('  node convert-mp3-to-wav.js ./audio');
    console.log('  node convert-mp3-to-wav.js ./mp3-files ./wav-files');
    console.log('  node convert-mp3-to-wav.js ./audio ./output --flat');
    console.log('  node convert-mp3-to-wav.js ./audio --delete');
    console.log(
      '  node convert-mp3-to-wav.js ./mp3-files ./wav-files --flat --delete',
    );
    process.exit(1);
  }

  // Filter out flags to get positional arguments
  const flags = args.filter((arg) => arg.startsWith('--'));
  const positionalArgs = args.filter((arg) => !arg.startsWith('--'));

  const inputDir = positionalArgs[0];
  const outputDir = positionalArgs[1];
  const preserveStructure = !flags.includes('--flat');
  const deleteOriginal = flags.includes('--delete');

  // Validate input directory
  if (!existsSync(inputDir)) {
    console.error(`Error: Input directory "${inputDir}" does not exist.`);
    process.exit(1);
  }

  try {
    const stats = await stat(inputDir);
    if (!stats.isDirectory()) {
      console.error(`Error: "${inputDir}" is not a directory.`);
      process.exit(1);
    }
  } catch (error) {
    console.error(`Error accessing input directory:`, error.message);
    process.exit(1);
  }

  try {
    await convertAllMp3ToWav(
      inputDir,
      outputDir,
      preserveStructure,
      deleteOriginal,
    );
  } catch (error) {
    console.error('Conversion failed:', error.message);
    process.exit(1);
  }
}

// Run the script if called directly
const __filename = fileURLToPath(import.meta.url);
if (process.argv[1] === __filename) {
  main().catch(console.error);
}

export { convertAllMp3ToWav, convertMp3ToWav, getMp3Files };
