import { ElevenLabsClient } from "@elevenlabs/elevenlabs-js";
import { writeFileSync, existsSync, mkdirSync } from "node:fs";
import { join } from "node:path";
import "dotenv/config";

const elevenlabs = new ElevenLabsClient({
  apiKey: process.env.ELEVENLABS_API_KEY
});

// Configuration
const VOICE_ID = "XrExE9yKIg1WjnnlVkGX"; // Same voice as other generators
const MODEL_ID = "eleven_multilingual_v2";
const OUTPUT_FORMAT = "mp3_44100_128";
const OUTPUT_DIR = "E:/Crossover/AIProjects/ai-edu-chatbot/game_skeletons/SubwaySurfers/Assets/Sounds/tutorial";

// Ensure output directory exists
if (!existsSync(OUTPUT_DIR)) {
  mkdirSync(OUTPUT_DIR, { recursive: true });
}

// All tutorial step texts
const tutorialSteps = [
  // Step 1: Tutorial Start (same for both platforms)
  {
    text: "Let's learn how to play",
    filename: "start.mp3"
  },
  
  // Step 2: Move Left
  {
    text: "Press A or left arrow to move left",
    filename: "move-left-desktop.mp3"
  },
  {
    text: "Swipe left to move left",
    filename: "move-left-mobile.mp3"
  },
  
  // Step 3: Move Right
  {
    text: "Press D or right arrow to move right",
    filename: "move-right-desktop.mp3"
  },
  {
    text: "Swipe right to move right",
    filename: "move-right-mobile.mp3"
  },
  
  // Step 4: Jump
  {
    text: "Press W or up arrow to jump",
    filename: "jump-desktop.mp3"
  },
  {
    text: "Swipe up to jump",
    filename: "jump-mobile.mp3"
  },
  
  // Step 5: Slide
  {
    text: "Press S or down arrow to slide",
    filename: "slide-desktop.mp3"
  },
  {
    text: "Swipe down to slide",
    filename: "slide-mobile.mp3"
  },
  
  // Step 6: Answer Selection
  {
    text: "Click a button or use arrow keys and Enter to select your answer",
    filename: "answer-selection-desktop.mp3"
  },
  {
    text: "Tap a button to select your answer",
    filename: "answer-selection-mobile.mp3"
  },
  
  // Step 7: Tutorial Complete (same for both platforms)
  {
    text: "Congratulations! You've finished the tutorial",
    filename: "complete.mp3"
  }
];

async function generateAudioFile(text, filename) {
  const filepath = join(OUTPUT_DIR, filename);
  
  // Skip if file already exists
  if (existsSync(filepath)) {
    console.log(`Skipping ${filename} - already exists`);
    return;
  }
  
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

async function generateTutorialAudio() {
  console.log("Starting consolidated tutorial audio generation...");
  console.log(`Output directory: ${OUTPUT_DIR}`);
  
  if (!process.env.ELEVENLABS_API_KEY) {
    console.error("Error: ELEVENLABS_API_KEY not found in environment variables");
    console.error("Please create a .env file with your API key:");
    console.error("ELEVENLABS_API_KEY=your_api_key_here");
    process.exit(1);
  }
  
  const totalFiles = tutorialSteps.length;
  let generatedFiles = 0;
  
  console.log(`\nGenerating ${totalFiles} tutorial audio files...\n`);
  
  // Generate all tutorial step files
  for (const step of tutorialSteps) {
    const filepath = join(OUTPUT_DIR, step.filename);
    
    if (!existsSync(filepath)) {
      await generateAudioFile(step.text, step.filename);
      generatedFiles++;
    } else {
      console.log(`Skipping ${step.filename} - already exists`);
    }
  }
  
  console.log(`\n✅ Completed tutorial audio generation!`);
  console.log(`📁 Generated ${generatedFiles} new files out of ${totalFiles} total files.`);
  console.log(`📂 Files saved to: ${OUTPUT_DIR}`);
  
  // Summary of generated files
  console.log(`\n📋 Tutorial Steps Generated:`);
  console.log(`   • Tutorial Start: start.mp3`);
  console.log(`   • Move Left: move-left-desktop.mp3, move-left-mobile.mp3`);
  console.log(`   • Move Right: move-right-desktop.mp3, move-right-mobile.mp3`);
  console.log(`   • Jump: jump-desktop.mp3, jump-mobile.mp3`);
  console.log(`   • Slide: slide-desktop.mp3, slide-mobile.mp3`);
  console.log(`   • Answer Selection: answer-selection-desktop.mp3, answer-selection-mobile.mp3`);
  console.log(`   • Tutorial Complete: complete.mp3`);
}

// Run the generator
generateTutorialAudio().catch(console.error); 