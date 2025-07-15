import { PROGRESSION_STAGES } from './constants';
import gameConfig from './GameConfig';

class GameSpeedManager {
  constructor(scene, initialSpeed = 0) {
    this.scene = scene;
    this.currentScrollSpeed = initialSpeed;
    this.progressionStage = PROGRESSION_STAGES.MANUAL_CONTROL;
    this.isManuallyMovingForward = false;
    this.transitionTimer = null;

    // Configuration for speeds - initialize with defaults
    this.config = {
      baseAutoRun: gameConfig.get('speed.baseAutoRunSpeed') || 200,
      manualForward: gameConfig.get('speed.manualModeSpeed') || 150,
      min: gameConfig.get('speed.minSpeed') || 100,
      max: gameConfig.get('speed.maxSpeed') || 400,
      acceleration: gameConfig.get('speed.accelerationFactor') || 50,
      deceleration: gameConfig.get('speed.decelerationFactor') || 30,
      initialTransitionDuration: gameConfig.get('speed.initialTransitionDuration') || 2000,
    };
  }

  setProgressionStage(stage) {
    const prevStage = this.progressionStage;
    this.progressionStage = stage;
    
    if (prevStage === PROGRESSION_STAGES.MANUAL_CONTROL && 
        stage === PROGRESSION_STAGES.AUTO_RUN_INITIAL) {
      // If transitioning from manual to auto, start gradual transition
      this.startGradualSpeedIncrease();
    } else {
      this.resetToStageSpeed();
    }
    
    console.log(`GameSpeedManager stage set to: ${Object.keys(PROGRESSION_STAGES).find(key => PROGRESSION_STAGES[key] === stage)}, speed: ${this.currentScrollSpeed}`);
  }

  resetToStageSpeed() {
    // Get speed multiplier for current stage
    const speedMultipliers = gameConfig.get('progression.speedMultipliers') || 
      [1.0, 1.2, 1.5, 1.8, 2.0];
    const multiplier = speedMultipliers[this.progressionStage] || 1.0;
    
    switch (this.progressionStage) {
      case PROGRESSION_STAGES.MANUAL_CONTROL:
        this.currentScrollSpeed = this.isManuallyMovingForward ? this.config.manualForward : 0;
        break;
      case PROGRESSION_STAGES.AUTO_RUN_INITIAL:
        // Always ensure there's movement in auto-run mode
        if (this.currentScrollSpeed <= 0) {
          this.currentScrollSpeed = this.config.baseAutoRun * multiplier;
        }
        break;
      case PROGRESSION_STAGES.AUTO_RUN_VARIABLE_SPEED:
      case PROGRESSION_STAGES.EXPERT_RUNNER:
      case PROGRESSION_STAGES.MASTER_RUNNER:
        // Apply different base speeds for higher levels
        if (this.currentScrollSpeed <= 0 || this.currentScrollSpeed > this.config.max) {
            this.currentScrollSpeed = this.config.baseAutoRun * multiplier;
        }
        
        // Adjust min/max speeds based on level
        const minSpeed = this.config.min * multiplier;
        const maxSpeed = Math.min(this.config.max * multiplier, 800); // Cap at 800 to keep game playable
        
        this.currentScrollSpeed = Phaser.Math.Clamp(this.currentScrollSpeed, minSpeed, maxSpeed);
        break;
      default:
        this.currentScrollSpeed = 0;
    }
    
    console.log(`Reset speed for stage ${this.progressionStage} to ${this.currentScrollSpeed} (multiplier: ${multiplier})`);
  }

  // Refresh config values (called after async loading)
  refreshConfig() {
    this.config = {
      baseAutoRun: gameConfig.get('speed.baseAutoRunSpeed') || this.config.baseAutoRun,
      manualForward: gameConfig.get('speed.manualModeSpeed') || this.config.manualForward,
      min: gameConfig.get('speed.minSpeed') || this.config.min,
      max: gameConfig.get('speed.maxSpeed') || this.config.max,
      acceleration: gameConfig.get('speed.accelerationFactor') || this.config.acceleration,
      deceleration: gameConfig.get('speed.decelerationFactor') || this.config.deceleration,
      initialTransitionDuration: gameConfig.get('speed.initialTransitionDuration') || this.config.initialTransitionDuration,
    };
  }

  setManualForward(isPressed) {
    if (this.progressionStage === PROGRESSION_STAGES.MANUAL_CONTROL) {
      this.isManuallyMovingForward = isPressed;
      this.currentScrollSpeed = isPressed ? this.config.manualForward : 0;
    }
  }

  processAnswer(isCorrect) {
    if (this.progressionStage === PROGRESSION_STAGES.AUTO_RUN_VARIABLE_SPEED) {
      if (isCorrect) {
        this.currentScrollSpeed += this.config.acceleration;
      } else {
        this.currentScrollSpeed -= this.config.deceleration;
      }
      this.currentScrollSpeed = Phaser.Math.Clamp(this.currentScrollSpeed, this.config.min, this.config.max);
      console.log(`Speed updated due to answer: ${this.currentScrollSpeed}`);
    }
  }

  // New method for speed-up control
  speedUp() {
    if (this.progressionStage === PROGRESSION_STAGES.AUTO_RUN_VARIABLE_SPEED) {
      // Jump straight to max speed on right arrow press
      this.currentScrollSpeed = this.config.max;
      console.log(`Speed maxed out: ${this.currentScrollSpeed}`);
      
      // Play speed-up sound if available
      if (this.scene.speedUpSound && typeof this.scene.speedUpSound.play === 'function') {
        this.scene.speedUpSound.play();
      }
    }
  }

  // Gradual speed transition when changing modes
  startGradualSpeedIncrease() {
    const startSpeed = 0;
    const targetSpeed = this.config.baseAutoRun;
    const duration = this.config.initialTransitionDuration;
    const steps = duration / 100; // Update every 100ms
    const speedIncrement = (targetSpeed - startSpeed) / steps;
    
    // Clear any existing transition
    if (this.transitionTimer) {
      this.transitionTimer.remove();
    }
    
    this.currentScrollSpeed = startSpeed;
    
    // Create a timer to gradually increase speed
    this.transitionTimer = this.scene.time.addEvent({
      delay: 100,
      callback: () => {
        this.currentScrollSpeed += speedIncrement;
        
        // If we've reached the target, stop the timer
        if (this.currentScrollSpeed >= targetSpeed) {
          this.currentScrollSpeed = targetSpeed;
          this.transitionTimer.remove();
          this.transitionTimer = null;
        }
      },
      callbackScope: this,
      repeat: steps - 1
    });
  }

  getCurrentSpeed() {
    return this.currentScrollSpeed;
  }
  
  // The update method will adjust the speed over time (per frame)
  // rather than instantly setting it. This is important for background movement.
  getEffectiveSpeed(delta) {
    // delta is typically in milliseconds, so convert speed to per-millisecond
    // or ensure delta is treated as fraction of a second if speed is per second.
    // For Phaser, delta is usually ms. If currentScrollSpeed is pixels/second:
    return this.currentScrollSpeed * (delta / 1000);
  }
}

export default GameSpeedManager; 