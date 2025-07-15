import { ElevenLabsClient } from "@elevenlabs/elevenlabs-js";
import { writeFileSync, existsSync, mkdirSync } from "node:fs";
import { join } from "node:path";
import "dotenv/config";

const elevenlabs = new ElevenLabsClient({
  apiKey: process.env.ELEVENLABS_API_KEY
});

// Configuration
const VOICE_ID = "XrExE9yKIg1WjnnlVkGX"; // You can change this to your preferred voice
const MODEL_ID = "eleven_multilingual_v2";
const OUTPUT_FORMAT = "mp3_44100_128";
const OUTPUT_DIR = "./game_skeletons/SubwaySurfers/Assets/ReusablePatterns/FluencySDK/Sounds/fluency";

// Ensure output directory exists
if (!existsSync(OUTPUT_DIR)) {
  mkdirSync(OUTPUT_DIR, { recursive: true });
}

// Number words for better pronunciation
const numberWords = {
  0: "zero",
  1: "one", 
  2: "two",
  3: "three",
  4: "four",
  5: "five",
  6: "six",
  7: "seven",
  8: "eight",
  9: "nine",
  10: "ten",
  11: "eleven",
  12: "twelve"
};

async function generateAudioFile(firstNumber, secondNumber) {
  const filename = `${firstNumber}x${secondNumber}.mp3`;
  const filepath = join(OUTPUT_DIR, filename);
  
  // Skip if file already exists
  if (existsSync(filepath)) {
    console.log(`Skipping ${filename} - already exists`);
    return;
  }
  
  const text = `${numberWords[firstNumber]} times ${numberWords[secondNumber]}`;
  
  try {
    console.log(`Generating: ${filename} - "${text}"`);
    
    const audio = await elevenlabs.textToSpeech.convert(VOICE_ID, {
      text: text,
      voiceId: VOICE_ID,
      modelId: MODEL_ID,
      outputFormat: OUTPUT_FORMAT,
    });
    
    // Convert the audio stream to buffer
    const chunks = [];
    for await (const chunk of audio) {
      chunks.push(chunk);
    }
    const audioBuffer = Buffer.concat(chunks);
    
    // Write to file
    writeFileSync(filepath, audioBuffer);
    console.log(`✓ Generated: ${filename}`);
    
    // Add a small delay to avoid rate limiting
    await new Promise(resolve => setTimeout(resolve, 500));
    
  } catch (error) {
    console.error(`Error generating ${filename}:`, error.message);
  }
}

async function generateAllFiles() {
  console.log("Starting multiplication table audio generation...");
  console.log(`Output directory: ${OUTPUT_DIR}`);
  
  if (!process.env.ELEVENLABS_API_KEY) {
    console.error("Error: ELEVENLABS_API_KEY not found in environment variables");
    console.error("Please create a .env file with your API key:");
    console.error("ELEVENLABS_API_KEY=your_api_key_here");
    process.exit(1);
  }
  
  let totalFiles = 0;
  let generatedFiles = 0;
  
  // Generate all combinations from 0x0 to 12x12
  for (let i = 0; i <= 12; i++) {
    for (let j = 0; j <= 12; j++) {
      totalFiles++;
      const filename = `${i}x${j}.mp3`;
      const filepath = join(OUTPUT_DIR, filename);
      
      if (!existsSync(filepath)) {
        await generateAudioFile(i, j);
        generatedFiles++;
      } else {
        console.log(`Skipping ${filename} - already exists`);
      }
    }
  }
  
  console.log(`\nCompleted! Generated ${generatedFiles} new files out of ${totalFiles} total files.`);
}

// Run the generator
generateAllFiles().catch(console.error); 