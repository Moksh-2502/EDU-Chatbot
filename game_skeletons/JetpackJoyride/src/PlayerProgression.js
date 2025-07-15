import gameConfig from './GameConfig';
import { PROGRESSION_STAGES } from './constants';

// Export for backward compatibility with existing imports
export { PROGRESSION_STAGES };

class PlayerProgression {
  constructor(scene) {
    this.scene = scene;
    this.progressionStage = PROGRESSION_STAGES.MANUAL_CONTROL;
    this.levelNames = gameConfig.get('progression.levelNames') || 
                      ['Novice Navigator', 'Steady Strider', 'Velocity Voyager', 'Expert Explorer', 'Master Marathoner'];
    // Default to higher values, will be overridden by config in init()
    this.requiredAnswers = [2, 3, 4, 5]; 
    this.currentLevelName = this.levelNames[this.progressionStage];
    this.correctAnswerCountInStage = 0;
    this.incorrectAnswerCountInStage = 0; // Track incorrect answers for level down
    this.totalCorrectAnswers = 0;
    this.totalIncorrectAnswers = 0;
    
    // Threshold for incorrect answers before level down
    this.maxIncorrectForLevelDown = 3; 
  }

  init() {
    // Refresh config in case it was loaded after constructor
    this.levelNames = gameConfig.get('progression.levelNames') || this.levelNames;
    
    // Make sure we get the correct required answers from config
    const configAnswers = gameConfig.get('progression.requiredAnswersForNextStage');
    if (configAnswers && Array.isArray(configAnswers) && configAnswers.length >= this.levelNames.length - 1) {
      this.requiredAnswers = configAnswers;
    }
    
    this.progressionStage = PROGRESSION_STAGES.MANUAL_CONTROL;
    this.currentLevelName = this.levelNames[this.progressionStage];
    this.correctAnswerCountInStage = 0;
    this.incorrectAnswerCountInStage = 0;
    this.totalCorrectAnswers = 0;
    this.totalIncorrectAnswers = 0;
    console.log('PlayerProgression initialized:', this.getCurrentStageName());
    console.log('Required answers for progression:', this.requiredAnswers);
  }

  processAnswer(isCorrect) {
    let stageChanged = false;
    
    if (isCorrect) {
      this.correctAnswerCountInStage++;
      this.totalCorrectAnswers++;
      // Reset incorrect counter when we get a correct answer
      this.incorrectAnswerCountInStage = 0;

      const requiredAnswers = this.requiredAnswers[this.progressionStage];
      console.log(`Correct answers in stage: ${this.correctAnswerCountInStage}/${requiredAnswers} required`);
      
      // Check if we should advance to next stage (except the final stage)
      if (this.progressionStage < Object.keys(PROGRESSION_STAGES).length - 1 && 
          this.correctAnswerCountInStage >= requiredAnswers) {
        console.log(`Advancing from stage ${this.progressionStage} with ${this.correctAnswerCountInStage} correct answers`);
        this.advanceStage();
        this.refillHealth();
        stageChanged = true;
      }
    } else {
      // Handle incorrect answers
      this.incorrectAnswerCountInStage++;
      this.totalIncorrectAnswers++;
      
      // Check if we should go down a level
      if (this.progressionStage > PROGRESSION_STAGES.MANUAL_CONTROL && 
          this.incorrectAnswerCountInStage >= this.maxIncorrectForLevelDown) {
        console.log(`Going down from stage ${this.progressionStage} due to ${this.incorrectAnswerCountInStage} incorrect answers`);
        this.decreaseStage();
        this.refillHealth();
        stageChanged = true;
      }
    }
    
    return stageChanged; // Returns true if progression stage changed
  }

  advanceStage() {
    if (this.progressionStage < Object.keys(PROGRESSION_STAGES).length - 1) {
      this.progressionStage++;
      this.currentLevelName = this.levelNames[this.progressionStage];
      this.correctAnswerCountInStage = 0; // Reset for the new stage
      this.incorrectAnswerCountInStage = 0; // Reset incorrect count
      console.log('Advanced to stage:', this.getCurrentStageName());
      
      // Make sure to notify game.js that stage was advanced
      if (this.scene && this.scene.gameSpeedManager) {
        // This ensures speed is updated for the new progression stage
        this.scene.gameSpeedManager.setProgressionStage(this.progressionStage);
      }
    }
  }

  decreaseStage() {
    if (this.progressionStage > PROGRESSION_STAGES.MANUAL_CONTROL) {
      this.progressionStage--;
      this.currentLevelName = this.levelNames[this.progressionStage];
      this.correctAnswerCountInStage = 0; // Reset for the new stage
      this.incorrectAnswerCountInStage = 0; // Reset incorrect count
      console.log('Decreased to stage:', this.getCurrentStageName());
      
      // Make sure to notify game.js that stage was changed
      if (this.scene && this.scene.gameSpeedManager) {
        // This ensures speed is updated for the new progression stage
        this.scene.gameSpeedManager.setProgressionStage(this.progressionStage);
      }
    }
  }

  // Refill player health when changing levels
  refillHealth() {
    if (this.scene) {
      const maxHealth = gameConfig.get('health.maxHealth') || 120;
      this.scene.health = maxHealth;
      this.scene.updateHealthBar();
      console.log(`Health refilled to ${maxHealth} after level change`);
    }
  }

  getCurrentStage() {
    return this.progressionStage;
  }

  getCurrentStageName() {
    return Object.keys(PROGRESSION_STAGES).find(key => PROGRESSION_STAGES[key] === this.progressionStage);
  }

  getCurrentLevelName() {
    return this.currentLevelName;
  }

  isManualMode() {
    return this.progressionStage === PROGRESSION_STAGES.MANUAL_CONTROL;
  }

  isAutoRunMode() {
    return this.progressionStage === PROGRESSION_STAGES.AUTO_RUN_INITIAL ||
           this.progressionStage === PROGRESSION_STAGES.AUTO_RUN_VARIABLE_SPEED;
  }
}

export default PlayerProgression; 