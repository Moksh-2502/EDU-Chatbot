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
const BASE_DIR = "E:\\Crossover\\AIProjects\\ai-edu-chatbot\\game_skeletons\\SubwaySurfers\\Assets\\ReusablePatterns\\FluencySDK\\Sounds\\fluency\\wrong_no_corrections";
const WRONG_ANSWER_DIR = join(BASE_DIR, "wrong_no_correction");

// Ensure output directory exists
if (!existsSync(WRONG_ANSWER_DIR)) {
  mkdirSync(WRONG_ANSWER_DIR, { recursive: true });
}

// Wrong answer phrases
const wrongAnswerPhrases = [
  { text: "Oops!", filename: "wrong-oops.mp3" },
  { text: "Not quite yet!", filename: "wrong-not-quite-yet.mp3" },
  { text: "So close!", filename: "wrong-so-close.mp3" },
  { text: "Nope, try again!", filename: "wrong-nope-try-again.mp3" },
  { text: "Give it another shot!", filename: "wrong-give-it-another-shot.mp3" },
  { text: "Missed that one!", filename: "wrong-missed-that-one.mp3" },
  { text: "Almost there!", filename: "wrong-almost-there.mp3" },
  { text: "That's off!", filename: "wrong-thats-off.mp3" },
  { text: "Don't give up!", filename: "wrong-dont-give-up.mp3" },
  { text: "Try once more!", filename: "wrong-try-once-more.mp3" },
  { text: "Keep at it!", filename: "wrong-keep-at-it.mp3" },
  { text: "Better luck next time!", filename: "wrong-better-luck-next-time.mp3" }
];

async function generateAudioFile(text, filename) {
  const filepath = join(WRONG_ANSWER_DIR, filename);
  
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

async function generateWrongAnswerFiles() {
  console.log("Generating wrong answer audio files...");
  let generatedCount = 0;
  
  for (const phrase of wrongAnswerPhrases) {
    const wasGenerated = await generateAudioFile(phrase.text, phrase.filename);
    if (wasGenerated) generatedCount++;
  }
  
  console.log(`Generated ${generatedCount} new wrong answer files out of ${wrongAnswerPhrases.length} total.`);
  return generatedCount;
}

async function generateAllWrongAnswerFiles() {
  console.log("Starting wrong answer audio generation...");
  console.log(`Output directory: ${WRONG_ANSWER_DIR}`);
  
  if (!process.env.ELEVENLABS_API_KEY) {
    console.error("Error: ELEVENLABS_API_KEY not found in environment variables");
    console.error("Please create a .env file with your API key:");
    console.error("ELEVENLABS_API_KEY=your_api_key_here");
    process.exit(1);
  }
  
  const wrongAnswerCount = await generateWrongAnswerFiles();
  
  console.log(`\n🎉 Completed! Generated ${wrongAnswerCount} new wrong answer audio files.`);
  console.log(`- Wrong answer phrases: ${wrongAnswerCount}/${wrongAnswerPhrases.length}`);
}

// Run the generator
generateAllWrongAnswerFiles().catch(console.error); 