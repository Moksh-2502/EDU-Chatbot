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
const OUTPUT_DIR = "./game_skeletons/SubwaySurfers/Assets/ReusablePatterns/FluencySDK/Sounds/fluency/corrections";

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
  12: "twelve",
  13: "thirteen",
  14: "fourteen",
  15: "fifteen",
  16: "sixteen",
  17: "seventeen",
  18: "eighteen",
  19: "nineteen",
  20: "twenty",
  21: "twenty one",
  22: "twenty two",
  23: "twenty three",
  24: "twenty four",
  25: "twenty five",
  26: "twenty six",
  27: "twenty seven",
  28: "twenty eight",
  29: "twenty nine",
  30: "thirty",
  31: "thirty one",
  32: "thirty two",
  33: "thirty three",
  34: "thirty four",
  35: "thirty five",
  36: "thirty six",
  37: "thirty seven",
  38: "thirty eight",
  39: "thirty nine",
  40: "forty",
  41: "forty one",
  42: "forty two",
  43: "forty three",
  44: "forty four",
  45: "forty five",
  46: "forty six",
  47: "forty seven",
  48: "forty eight",
  49: "forty nine",
  50: "fifty",
  51: "fifty one",
  52: "fifty two",
  53: "fifty three",
  54: "fifty four",
  55: "fifty five",
  56: "fifty six",
  57: "fifty seven",
  58: "fifty eight",
  59: "fifty nine",
  60: "sixty",
  61: "sixty one",
  62: "sixty two",
  63: "sixty three",
  64: "sixty four",
  65: "sixty five",
  66: "sixty six",
  67: "sixty seven",
  68: "sixty eight",
  69: "sixty nine",
  70: "seventy",
  71: "seventy one",
  72: "seventy two",
  73: "seventy three",
  74: "seventy four",
  75: "seventy five",
  76: "seventy six",
  77: "seventy seven",
  78: "seventy eight",
  79: "seventy nine",
  80: "eighty",
  81: "eighty one",
  82: "eighty two",
  83: "eighty three",
  84: "eighty four",
  85: "eighty five",
  86: "eighty six",
  87: "eighty seven",
  88: "eighty eight",
  89: "eighty nine",
  90: "ninety",
  91: "ninety one",
  92: "ninety two",
  93: "ninety three",
  94: "ninety four",
  95: "ninety five",
  96: "ninety six",
  97: "ninety seven",
  98: "ninety eight",
  99: "ninety nine",
  100: "one hundred",
  101: "one hundred one",
  102: "one hundred two",
  103: "one hundred three",
  104: "one hundred four",
  105: "one hundred five",
  106: "one hundred six",
  107: "one hundred seven",
  108: "one hundred eight",
  109: "one hundred nine",
  110: "one hundred ten",
  111: "one hundred eleven",
  112: "one hundred twelve",
  113: "one hundred thirteen",
  114: "one hundred fourteen",
  115: "one hundred fifteen",
  116: "one hundred sixteen",
  117: "one hundred seventeen",
  118: "one hundred eighteen",
  119: "one hundred nineteen",
  120: "one hundred twenty",
  121: "one hundred twenty one",
  122: "one hundred twenty two",
  123: "one hundred twenty three",
  124: "one hundred twenty four",
  125: "one hundred twenty five",
  126: "one hundred twenty six",
  127: "one hundred twenty seven",
  128: "one hundred twenty eight",
  129: "one hundred twenty nine",
  130: "one hundred thirty",
  131: "one hundred thirty one",
  132: "one hundred thirty two",
  133: "one hundred thirty three",
  134: "one hundred thirty four",
  135: "one hundred thirty five",
  136: "one hundred thirty six",
  137: "one hundred thirty seven",
  138: "one hundred thirty eight",
  139: "one hundred thirty nine",
  140: "one hundred forty",
  141: "one hundred forty one",
  142: "one hundred forty two",
  143: "one hundred forty three",
  144: "one hundred forty four"
};

// Prefix words for corrections
const prefixes = [
  { text: "Oops!", filename: "correction-oops.mp3" },
  { text: "Not quite!", filename: "correction-not-quite.mp3" }
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

async function generatePrefixFiles() {
  console.log("Generating prefix files...");
  
  for (const prefix of prefixes) {
    await generateAudioFile(prefix.text, prefix.filename);
  }
}

async function generateCorrectionFiles() {
  console.log("Generating correction answer files...");
  
  // Generate all combinations from 0x0 to 12x12
  for (let i = 0; i <= 12; i++) {
    for (let j = 0; j <= 12; j++) {
      const result = i * j;
      const text = `${numberWords[i]} times ${numberWords[j]} is ${numberWords[result]}`;
      const filename = `correction-${i}x${j}.mp3`;
      
      await generateAudioFile(text, filename);
    }
  }
}

async function generateAllCorrectionFiles() {
  console.log("Starting correction audio generation...");
  console.log(`Output directory: ${OUTPUT_DIR}`);
  
  if (!process.env.ELEVENLABS_API_KEY) {
    console.error("Error: ELEVENLABS_API_KEY not found in environment variables");
    console.error("Please create a .env file with your API key:");
    console.error("ELEVENLABS_API_KEY=your_api_key_here");
    process.exit(1);
  }
  
  // Generate prefix files first
  await generatePrefixFiles();
  
  // Then generate correction answer files
  await generateCorrectionFiles();
  
  console.log(`\nCompleted correction audio generation!`);
  console.log(`Generated files in: ${OUTPUT_DIR}`);
}

// Run the generator
generateAllCorrectionFiles().catch(console.error); 