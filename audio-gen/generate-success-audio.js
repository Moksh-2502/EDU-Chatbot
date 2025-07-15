import { ElevenLabsClient } from "@elevenlabs/elevenlabs-js";
import { writeFileSync, existsSync, mkdirSync } from "node:fs";
import { join } from "node:path";
import "dotenv/config";

const elevenlabs = new ElevenLabsClient({
  apiKey: process.env.ELEVENLABS_API_KEY
});

// Configuration
const VOICE_ID = "XrExE9yKIg1WjnnlVkGX"; // Same voice as main generator
const MODEL_ID = "eleven_multilingual_v2";
const OUTPUT_FORMAT = "mp3_44100_128";
const BASE_DIR = "E://Crossover//AIProjects//ai-edu-chatbot//game_skeletons//SubwaySurfers//Assets//ReusablePatterns//FluencySDK//Sounds//fluency";
const SUCCESS_DIR = join(BASE_DIR, "success");
const STREAK_DIR = join(BASE_DIR, "streak");

// Ensure output directories exist
if (!existsSync(SUCCESS_DIR)) {
  mkdirSync(SUCCESS_DIR, { recursive: true });
}
if (!existsSync(STREAK_DIR)) {
  mkdirSync(STREAK_DIR, { recursive: true });
}

// Regular success phrases for correct answers
const successPhrases = [
  { text: "You got it!", filename: "success-you-got-it.mp3" },
  { text: "Right on!", filename: "success-right-on.mp3" },
  { text: "Spot on!", filename: "success-spot-on.mp3" },
  { text: "That's it!", filename: "success-thats-it.mp3" },
  { text: "Bullseye!", filename: "success-bullseye.mp3" },
  { text: "Perfect!", filename: "success-perfect.mp3" },
  { text: "Nailed that one!", filename: "success-nailed-that-one.mp3" },
  { text: "Bravo!", filename: "success-bravo.mp3" },
  { text: "Excellent!", filename: "success-excellent.mp3" },
  { text: "Yes indeed!", filename: "success-yes-indeed.mp3" },
  { text: "Absolutely!", filename: "success-absolutely.mp3" },
  { text: "Gotcha!", filename: "success-gotcha.mp3" }
];

// Streak bonus phrases for success streaks
const streakPhrases = [
  { text: "You're on fire!", filename: "streak-on-fire.mp3" },
  { text: "Unstoppable!", filename: "streak-unstoppable.mp3" },
  { text: "Legendary!", filename: "streak-legendary.mp3" },
  { text: "Keep it rolling!", filename: "streak-keep-it-rolling.mp3" },
  { text: "Crushing it!", filename: "streak-crushing-it.mp3" },
  { text: "Hot streak!", filename: "streak-hot-streak.mp3" },
  { text: "Masterclass!", filename: "streak-masterclass.mp3" },
  { text: "Epic run!", filename: "streak-epic-run.mp3" },
  { text: "Next level!", filename: "streak-next-level.mp3" },
  { text: "Unbelievable!", filename: "streak-unbelievable.mp3" },
  { text: "Stellar streak!", filename: "streak-stellar-streak.mp3" },
  { text: "Dominating!", filename: "streak-dominating.mp3" }
];

async function generateAudioFile(text, filename, outputDir) {
  const filepath = join(outputDir, filename);
  
  // Skip if file already exists
  if (existsSync(filepath)) {
    console.log(`Skipping ${filename} - already exists`);
    return false;
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
    await new Promise(resolve => setTimeout(resolve, 700));
    
    return true;
  } catch (error) {
    console.error(`Error generating ${filename}:`, error.message);
    return false;
  }
}

async function generateSuccessFiles() {
  console.log("Generating regular success audio files...");
  let generatedCount = 0;
  
  for (const phrase of successPhrases) {
    const wasGenerated = await generateAudioFile(phrase.text, phrase.filename, SUCCESS_DIR);
    if (wasGenerated) generatedCount++;
  }
  
  console.log(`Generated ${generatedCount} new success files out of ${successPhrases.length} total.`);
  return generatedCount;
}

async function generateStreakFiles() {
  console.log("\nGenerating streak bonus audio files...");
  let generatedCount = 0;
  
  for (const phrase of streakPhrases) {
    const wasGenerated = await generateAudioFile(phrase.text, phrase.filename, STREAK_DIR);
    if (wasGenerated) generatedCount++;
  }
  
  console.log(`Generated ${generatedCount} new streak files out of ${streakPhrases.length} total.`);
  return generatedCount;
}

async function generateAllSuccessFiles() {
  console.log("Starting success audio generation...");
  console.log(`Success files directory: ${SUCCESS_DIR}`);
  console.log(`Streak files directory: ${STREAK_DIR}`);
  
  if (!process.env.ELEVENLABS_API_KEY) {
    console.error("Error: ELEVENLABS_API_KEY not found in environment variables");
    console.error("Please create a .env file with your API key:");
    console.error("ELEVENLABS_API_KEY=your_api_key_here");
    process.exit(1);
  }
  
  const successCount = await generateSuccessFiles();
  const streakCount = await generateStreakFiles();
  
  console.log(`\n🎉 Completed! Generated ${successCount + streakCount} new audio files total.`);
  console.log(`- Success phrases: ${successCount}/${successPhrases.length}`);
  console.log(`- Streak phrases: ${streakCount}/${streakPhrases.length}`);
}

// Run the generator
generateAllSuccessFiles().catch(console.error); 