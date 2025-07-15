import { PROGRESSION_STAGES } from './constants';

// Default configuration - will be overridden by loaded config
const DEFAULT_CONFIG = {
  // Features Toggle
  features: {
    enableBirds: false,
    enableRockets: false,
    enableCoinDrops: false,
    enableAnswerObstacles: true // Enable the new answer obstacles feature
  },
  
  // Player Progression
  progression: {
    levelNames: [
      'Beginner',
      'Novice Navigator',   // Level 1: MANUAL_CONTROL
      'Steady Strider',     // Level 2: AUTO_RUN_INITIAL
      'Velocity Voyager',   // Level 3: AUTO_RUN_VARIABLE_SPEED
      'Expert Explorer',    // Level 4: EXPERT_RUNNER
      'Master Marathoner'   // Level 5: MASTER_RUNNER
    ],
    requiredAnswersForNextStage: [8, 13, 21, 34, 55], // Number of correct answers needed to progress
    speedMultipliers: [1.0, 1.2, 1.5, 1.8, 2.0] // Speed multiplier for each level
  },
  
  // Game Speed
  speed: {
    baseAutoRunSpeed: 200,
    manualModeSpeed: 150,
    minSpeed: 100,
    maxSpeed: 400,
    accelerationFactor: 50,
    decelerationFactor: 30,
    initialTransitionDuration: 2000 // Duration for initial speed ramp-up (ms)
  },
  
  // Questions
  questions: {
    intervalByStage: {
      0: 10000, // MANUAL_CONTROL: 10 seconds between questions
      1: 8000,  // AUTO_RUN_INITIAL: 8 seconds
      2: 7000,  // AUTO_RUN_VARIABLE_SPEED: 7 seconds
      3: 6000,  // EXPERT_RUNNER: 6 seconds
      4: 5000   // MASTER_RUNNER: 5 seconds
    }
  },
  
  // Answer Obstacles
  answers: {
    hpGainOnCorrect: 10,
    hpLossOnWrong: 15,
    spacing: 300, // Base horizontal spacing (used as minimum spacing)
    spacingMin: 450, // Minimum distance between answers
    spacingMax: 700, // Maximum distance between answers
    spawnDelay: 500, // Small delay before spawning new answers after collision (ms)
    yOffset: 220 // Y-axis offset from player's position
  },
  
  // Missiles
  missiles: {
    speedByStage: {
      0: 2, // MANUAL_CONTROL
      1: 3, // AUTO_RUN_INITIAL
      2: 4, // AUTO_RUN_VARIABLE_SPEED
      3: 5, // EXPERT_RUNNER
      4: 6  // MASTER_RUNNER
    },
    scrollInfluence: 0.03,
    damage: {
      highMissile: 15,
      lowMissile: 10
    },
    creationFrequency: {
      baseMissile: {
        baseTime: 5000,
        levelMultiplier: 1000
      },
      secondMissile: {
        baseTime: 7000,
        levelMultiplier: 2000
      }
    }
  },
  
  // Environment
  environment: {
    parallaxFactors: {
      mountains: 0.25,
      plateau: 0.5,
      ground: 1.0
    }
  },
  
  // Health
  health: {
    initial: 120,
    maxHealth: 120,
    gainFromCoin: 1,
    gainFromCorrectAnswer: 5,
    penaltyFromWrongAnswer: 10,
    boostFromMissileScore: 10
  },
  
  // Items
  items: {
    spikeDamage: 15,
    coinCreationDelay: 1000,
    spikeCreationDelay: 5000
  },
  
  // Birds
  birds: {
    speed: -100,
    creationDelayMin: 2500,
    creationDelayMax: 5000
  },
  
  // Player Physics
  playerPhysics: {
    gravity: 800,
    fastFallGravity: 3500,
    jumpStrength: -400,
    maxJumps: 2,
    fallSpeed: 2500  // New value: controls how quickly player falls after jump apex
  }
};

class GameConfig {
  constructor() {
    this.config = DEFAULT_CONFIG;
    this.loaded = false;
  }

  // Load config from a JSON file
  async loadConfig(configPath = 'config/gameConfig.json') {
    try {
      const response = await fetch(configPath);
      if (!response.ok) {
        console.warn(`Could not load config from ${configPath}. Using default config.`);
        return;
      }
      
      const loadedConfig = await response.json();
      
      // Deep merge loaded config with default config
      this.mergeConfig(this.config, loadedConfig);
      
      console.log('Game configuration loaded successfully');
      this.loaded = true;
    } catch (error) {
      console.error('Error loading game config:', error);
      console.warn('Using default configuration values');
    }
  }
  
  // Deep merge objects helper
  mergeConfig(target, source) {
    for (const key in source) {
      if (source[key] && typeof source[key] === 'object' && !Array.isArray(source[key])) {
        if (!target[key]) target[key] = {};
        this.mergeConfig(target[key], source[key]);
      } else {
        target[key] = source[key];
      }
    }
  }
  
  // Get a specific config value with dot notation path
  get(path) {
    const parts = path.split('.');
    let current = this.config;
    
    for (const part of parts) {
      if (current[part] === undefined) {
        console.warn(`Config path ${path} not found. Using default value.`);
        return undefined;
      }
      current = current[part];
    }
    
    return current;
  }
  
  // Get entire config or section
  getSection(section) {
    if (!section) return this.config;
    return this.config[section];
  }
}

// Create a singleton instance
const gameConfig = new GameConfig();
export default gameConfig; 