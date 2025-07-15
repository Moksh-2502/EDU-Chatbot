import AnswerObstacle from './AnswerObstacle';
import gameConfig from './GameConfig';

class AnswerManager {
  constructor(scene, fluencyProvider) {
    this.scene = scene;
    this.fluencyProvider = fluencyProvider;
    this.currentQuestion = null;
    this.obstacles = [];
    this.spacing = gameConfig.get('answers.spacing') || 300; // Horizontal spacing between obstacles
    this.spawnPosition = { x: 0, y: 0 }; // Will be set during initialization
    
    // Config values
    this.hpGainOnCorrect = gameConfig.get('answers.hpGainOnCorrect') || 10;
    this.hpLossOnWrong = gameConfig.get('answers.hpLossOnWrong') || 15;
  }
  
  init(spawnX, spawnY) {
    // Calculate optimal spawn position based on screen width
    const screenWidth = this.scene.sys.game.config.width;
    this.spawnPosition.x = screenWidth + 100; // Just off-screen to the right
    
    // Get Y-offset from config, defaulting to 120 if not specified
    const yOffset = gameConfig.get('answers.yOffset') || 120;
    this.spawnPosition.y = spawnY + yOffset; // Use the configured Y-offset
    
    // Load config values for HP and spacing
    this.hpGainOnCorrect = gameConfig.get('answers.hpGainOnCorrect') || this.hpGainOnCorrect;
    this.hpLossOnWrong = gameConfig.get('answers.hpLossOnWrong') || this.hpLossOnWrong;
    
    // Get min and max spacing from config
    this.baseSpacing = gameConfig.get('answers.spacing') || 300;
    this.minSpacing = gameConfig.get('answers.spacingMin') || this.baseSpacing * 0.4;
    this.maxSpacing = gameConfig.get('answers.spacingMax') || this.baseSpacing;
    
    // Adjust for smaller boxes
    this.minSpacing *= 0.4;
    this.maxSpacing *= 0.4;
  }
  
  // Helper method to get a random spacing value
  getRandomSpacing() {
    return Phaser.Math.Between(this.minSpacing, this.maxSpacing);
  }
  
  async spawnNewQuestion() {
    try {
      // Get a new question from fluency provider
      const questions = await this.fluencyProvider.getNextQuestionBlock();
      
      // If no questions available, create a fallback question
      if (!questions || !questions.length) {
        console.warn("No valid questions received from FluencyProvider, using fallback question");
        this.currentQuestion = {
          question: "What is 2 + 2?",
          options: ["3", "4", "5", "6"],
          correctAnswer: "4"
        };
      } else {
        // Use the first question from the block
        this.currentQuestion = questions[0];
        
        // Get the formatted question text
        const questionText = this.fluencyProvider.formatQuestionForDisplay(this.currentQuestion);
        
        // Get multiple choice options if not provided
        if (!this.currentQuestion.options) {
          this.currentQuestion.options = this.fluencyProvider.generateMultipleChoiceOptions(this.currentQuestion, 4);
          this.currentQuestion.correctAnswer = this.currentQuestion.answer.toString();
        } else {
          // Make sure we have the correctAnswer set
          this.currentQuestion.correctAnswer = this.currentQuestion.answer.toString();
        }
        
        // Display the question in the UI
        if (this.scene.gameUIManager && this.scene.gameUIManager.updateQuestionText) {
          const formattedQuestion = this.fluencyProvider.formatQuestionForDisplay(this.currentQuestion);
          this.scene.gameUIManager.updateQuestionText(formattedQuestion);
        }
      }
      
      const answers = [...this.currentQuestion.options];
      
      // Adjust initial X position based on current speed
      // Faster speeds = spawn farther to maintain reaction time
      const currentSpeed = this.scene.gameSpeedManager.getCurrentSpeed();
      const baseSpawnX = this.spawnPosition.x;
      const speedFactor = Math.max(1, currentSpeed / 200); // Normalize with base speed
      
      let posX = baseSpawnX + (speedFactor * 100); // Add distance for faster speeds
      
      // Create obstacles for each answer
      answers.forEach(answer => {
        const answerStr = answer.toString();
        const correctStr = this.currentQuestion.correctAnswer.toString();
        const isCorrect = (answerStr === correctStr);
        
        const obstacle = new AnswerObstacle(
          this.scene, 
          posX, 
          this.spawnPosition.y,
          answerStr,
          isCorrect
        );
        
        this.obstacles.push(obstacle);
        
        // Setup collision with player
        this.scene.physics.add.collider(
          this.scene.player,
          obstacle.sprite,
          () => this.handleAnswerCollision(obstacle),
          null,
          this
        );
        
        // Increment X position for next obstacle
        posX += this.getRandomSpacing();
      });
    } catch (error) {
      console.error("Error spawning new question:", error);
      // Create a fallback when there's an error
      this.spawnFallbackQuestion();
    }
  }
  
  // Helper to create a fallback question when there's an error
  spawnFallbackQuestion() {
    try {
      this.currentQuestion = {
        question: "What is 2 + 2?",
        options: ["3", "4", "5", "6"],
        correctAnswer: "4"
      };
      
      // Display the fallback question in the UI
      if (this.scene.gameUIManager && this.scene.gameUIManager.updateQuestionText) {
        this.scene.gameUIManager.updateQuestionText(this.currentQuestion.question);
      }
      
      const answers = [...this.currentQuestion.options];
      
      // Use same positioning logic as the main spawn method
      const screenWidth = this.scene.sys.game.config.width;
      const currentSpeed = this.scene.gameSpeedManager.getCurrentSpeed();
      const baseSpawnX = this.spawnPosition.x;
      const speedFactor = Math.max(1, currentSpeed / 200);
      
      let posX = baseSpawnX + (speedFactor * 100);
      
      // Create obstacles for each answer
      answers.forEach(answer => {
        const isCorrect = (answer === this.currentQuestion.correctAnswer);
        const obstacle = new AnswerObstacle(
          this.scene, 
          posX, 
          this.spawnPosition.y,
          answer,
          isCorrect
        );
        
        this.obstacles.push(obstacle);
        
        // Setup collision with player
        this.scene.physics.add.collider(
          this.scene.player,
          obstacle.sprite,
          () => this.handleAnswerCollision(obstacle),
          null,
          this
        );
        
        // Increment X position for next obstacle
        posX += this.getRandomSpacing();
      });
    } catch (error) {
      console.error("Critical error spawning fallback question:", error);
    }
  }
  
  handleAnswerCollision(obstacle) {
    // Process the collision with an answer
    if (obstacle.isCorrect) {
      // Correct answer
      this.scene.health += this.hpGainOnCorrect;
      this.scene.updateHealthBar();
      
      // Visual feedback - floating text, sound effect
      this.scene.hoveringTextScore(
        obstacle.sprite,
        `+${this.hpGainOnCorrect} HP`,
        '#00FF00',
        '#FFFFFF'
      );
      
      // Play sound if it exists
      if (this.scene.correctSound && typeof this.scene.correctSound.play === 'function') {
        this.scene.correctSound.play();
      }
      
      // Show feedback message
      if (this.scene.gameUIManager) {
        this.scene.gameUIManager.showFeedback("Correct! +HP");
      }
      
      // Handle level progression
      if (this.scene.playerProgression) {
        console.log("Processing correct answer for progression");
        const stageChanged = this.scene.playerProgression.processAnswer(true);
        if (stageChanged) {
          const newStage = this.scene.playerProgression.getCurrentStage();
          const newLevelName = this.scene.playerProgression.getCurrentLevelName();
          console.log(`Advanced to stage ${newStage}: ${newLevelName}`);
          
          // Update UI with new level
          if (this.scene.gameUIManager) {
            this.scene.gameUIManager.updateLevelName(newLevelName);
            this.scene.gameUIManager.showFeedback(`Level Up: ${newLevelName}!`, 3000);
          }
          
          // Update game speed for new stage
          if (this.scene.gameSpeedManager) {
            this.scene.gameSpeedManager.setProgressionStage(newStage);
          }
        }
      }
    } else {
      // Wrong answer
      this.scene.health -= this.hpLossOnWrong;
      this.scene.updateHealthBar();
      
      // Visual feedback
      this.scene.hoveringTextScore(
        obstacle.sprite,
        `-${this.hpLossOnWrong} HP`,
        '#FF0000',
        '#FFFFFF'
      );
      
      // Play sound if it exists
      if (this.scene.wrongSound && typeof this.scene.wrongSound.play === 'function') {
        this.scene.wrongSound.play();
      }
      
      // Show feedback message with the correct answer
      if (this.scene.gameUIManager && this.currentQuestion) {
        const correctAnswer = this.currentQuestion.correctAnswer;
        this.scene.gameUIManager.showFeedback(`Wrong! Answer: ${correctAnswer}`);
      }
      
      // Process incorrect answer for possible level down
      if (this.scene.playerProgression) {
        console.log("Processing incorrect answer for potential level down");
        const stageChanged = this.scene.playerProgression.processAnswer(false);
        if (stageChanged) {
          // Level down occurred
          const newStage = this.scene.playerProgression.getCurrentStage();
          const newLevelName = this.scene.playerProgression.getCurrentLevelName();
          console.log(`Decreased to stage ${newStage}: ${newLevelName}`);
          
          // Update UI with new level
          if (this.scene.gameUIManager) {
            this.scene.gameUIManager.updateLevelName(newLevelName);
            this.scene.gameUIManager.showFeedback(`Level Down: ${newLevelName}`, 3000);
          }
          
          // Update game speed for new stage
          if (this.scene.gameSpeedManager) {
            this.scene.gameSpeedManager.setProgressionStage(newStage);
          }
        }
      }
    }
    
    // Update the game speed based on correctness
    this.scene.gameSpeedManager.processAnswer(obstacle.isCorrect);
    
    // Remove the obstacle after collision
    obstacle.destroy();
    this.obstacles = this.obstacles.filter(o => o !== obstacle);
    
    // If there are remaining obstacles, destroy them with a visual effect
    if (this.obstacles.length > 0) {
      this.obstacles.forEach(o => {
        // Add fade out effect
        this.scene.tweens.add({
          targets: [o.sprite, o.textObject],
          alpha: 0,
          duration: 300,
          onComplete: () => o.destroy()
        });
      });
      this.obstacles = [];
      
      // Small delay before spawning new question
      const spawnDelay = gameConfig.get('answers.spawnDelay') || 500;
      this.scene.time.delayedCall(spawnDelay, () => {
        this.spawnNewQuestion();
      });
    } else {
      // If all obstacles were already gone, spawn new question immediately
      this.spawnNewQuestion();
    }
  }
  
  update(scrollSpeed) {
    // Update all obstacles position
    const destroyedObstacles = [];
    
    this.obstacles.forEach(obstacle => {
      if (obstacle.update(scrollSpeed)) {
        destroyedObstacles.push(obstacle);
      }
    });
    
    // Remove destroyed obstacles
    if (destroyedObstacles.length > 0) {
      this.obstacles = this.obstacles.filter(o => !destroyedObstacles.includes(o));
      
      // If all obstacles are gone, generate new ones
      if (this.obstacles.length === 0) {
        this.spawnNewQuestion();
      }
    }
  }
  
  reset() {
    // Clean up all obstacles
    this.obstacles.forEach(obstacle => obstacle.destroy());
    this.obstacles = [];
    this.currentQuestion = null;
    
    // Clear the question text in the UI
    if (this.scene.gameUIManager && this.scene.gameUIManager.updateQuestionText) {
      this.scene.gameUIManager.updateQuestionText("");
    }
  }
}

export default AnswerManager; 