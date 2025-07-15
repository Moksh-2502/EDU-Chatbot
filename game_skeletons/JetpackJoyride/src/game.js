import Phaser from 'phaser';
import { gameState, playStopAudio } from './boot';
import * as fetchScoreData from './support_script/fetchData';
import 'regenerator-runtime/runtime';
import FluencyProvider from './FluencyProvider';
// import QuestionUI from './QuestionUI'; // Now managed by GameUIManager

import PlayerProgression from './PlayerProgression';
import { PROGRESSION_STAGES } from './constants';
import GameSpeedManager from './GameSpeedManager';
import PlayerController from './PlayerController';
import GameUIManager from './GameUIManager';
import gameConfig from './GameConfig';
import AnswerManager from './AnswerManager';

// Question frequency by stage - more frequent in lower levels, less in higher
const QUESTION_INTERVAL_BY_STAGE = {
  0: 8000,       // MANUAL_CONTROL: 8 seconds between questions at novice stage
  1: 12000,      // AUTO_RUN_INITIAL: 12 seconds at intermediate stage
  2: 15000,      // AUTO_RUN_VARIABLE_SPEED: 15 seconds at advanced stage
};

// Missile speed factors by stage - slower in lower levels, faster in higher
const MISSILE_SPEED_BY_STAGE = {
  0: 2,       // MANUAL_CONTROL: Slow missiles for beginners
  1: 3,       // AUTO_RUN_INITIAL: Medium speed missiles
  2: 4,       // AUTO_RUN_VARIABLE_SPEED: Faster missiles for advanced
};

// Missile damage by type and position - fallback values if config unavailable
const MISSILE_DAMAGE = {
  HIGH_MISSILE: 15,   // Will use config values when available
  LOW_MISSILE: 10,    // Will use config values when available
};

// Spike damage - fallback value if config unavailable
const SPIKE_DAMAGE = 15; 

// Factor by which missile speed is affected by scroll speed (currentScrollSpeed)
const MISSILE_SCROLL_INFLUENCE = 0.03; // Reduced from 0.1 to make scroll speed have less impact

const createPlatform = (group, spriteWidth, myTexture, dist = 0) => {
  const platform = group.create(spriteWidth + dist, gameState.sceneHeight, myTexture)
    .setOrigin(0, 1)
    .setScale(0.5);
  if (myTexture === 'ground') {
    platform.setImmovable(true);
    platform.setSize(platform.displayWidth * 2, platform.displayHeight - 50);
  }

  switch (myTexture) {
    case 'ground':
      platform.setDepth(2);
      break;
    case 'plateau':
      platform.setDepth(1);
      break;
    default:
  }
};

const updatePlatform = (group, spriteWidth, myTexture, dist = 0) => {
  const child = group.get(spriteWidth - dist, gameState.sceneHeight, myTexture);
  child.setVisible(true);
  child.setActive(true);
  switch (myTexture) {
    case 'ground':
      child.setDepth(2);
      break;
    case 'plateau':
      child.setDepth(1);
      break;
    default:
  }
};

// Modified to use delta-compensated speed for smooth movement regardless of frame rate
const moveBackgroundPlatform = (group, platformWidth, myTexture, effectiveSpeed) => {
  group.children.iterate((child) => {
    child.x -= effectiveSpeed;
    if (child.x < -(child.displayWidth)) {
      group.killAndHide(child);
      // Pass 0 for dist if speed already accounts for direction and magnitude
      updatePlatform(group, platformWidth, myTexture, 0); 
    }
  });
};


class Game extends Phaser.Scene {
  constructor() {
    super({ key: 'Game' });
    this.timer = 0; // For missile creation
    this.secondTimer = 0; // For second missile type
    this.missileScore = 0;

    this.isGamePaused = false;
    this.fluencyProvider = null;
    this.currentFluencyQuestion = null;
    this.questionDisplayTime = null;
    this.questionTriggerTimer = 0;
    this.questionInterval = 8000; // Default value, will be overridden by config

    // New class instances
    this.playerProgression = new PlayerProgression(this);
    this.gameSpeedManager = new GameSpeedManager(this);
    this.playerController = null; // Initialized in create, needs player sprite
    this.gameUIManager = new GameUIManager(this);
    this.answerManager = null; // Will be initialized in create
    
    // Config loaded flag
    this.configLoaded = false;
  }

  // Preload function to load the configuration before scene starts
  preload() {
    // Start loading config in preload
    gameConfig.loadConfig().then(() => {
      this.configLoaded = true;
      this.applyConfigValues();
      console.log("Game configuration loaded successfully");
    }).catch(error => {
      console.error("Failed to load game configuration:", error);
      console.log("Using default values");
    });
  }

  applyConfigValues() {
    // Set question interval based on current progression stage
    const currentStage = this.playerProgression ? this.playerProgression.getCurrentStage() : PROGRESSION_STAGES.MANUAL_CONTROL;
    const intervals = gameConfig.get('questions.intervalByStage');
    if (intervals && intervals[currentStage] !== undefined) {
      this.questionInterval = intervals[currentStage];
    }
  }

  // Start of create function
  create() {
    this.gameTheme = this.sound.add('theme2', { loop: true });
    this.gameTheme.volume = 0.1;

    playStopAudio(gameState.music, this.gameTheme);

    this.addSoundEffects(); // jumpSound is used by PlayerController, ensure it's created

    gameState.score = 0;
    this.health = gameConfig.get('health.initial') || 120;
    const maxHealth = gameConfig.get('health.maxHealth') || 120;

    // Score and Health UI (remains in Game.js for now)
    this.scoreText = this.add.text(50, 25, 'Coins: ', {
      fontSize: '40px',
      fill: '#ffffff',
      fontFamily: '"Akaya Telivigala"',
      strokeThickness: 10,
      stroke: '#FFD700',
    }).setDepth(8);

    this.scoreValue = this.add.text(170, 25, `${gameState.score}`, {
      fontSize: '40px',
      fill: '#ffffff',
      fontFamily: '"Akaya Telivigala"',
      strokeThickness: 5,
      stroke: '#000',
    }).setDepth(8);

    this.healthText = this.add.text(50, 75, 'Health: ', {
      fontSize: '30px',
      fill: '#ffffff',
      strokeThickness: 8,
      fontFamily: '"Akaya Telivigala"',
      stroke: '#FF69B4',
    }).setDepth(8);

    this.progressBox = this.add.graphics();
    this.progressBar = this.add.graphics();
    this.progressBox.setDepth(8);
    this.progressBar.setDepth(8);
    this.progressBox.lineStyle(3, 0x0275d8, 1);
    this.progressBox.strokeRect(170, 95, maxHealth, 10); // Full width for health bar background
    this.progressBar.fillStyle(0xFFD700, 1);
    this.progressBar.fillRect(170, 95, this.health, 10);

    this.addGameBackground();

    this.player = this.physics.add.sprite(200, gameState.sceneHeight - 300, 'player').setScale(0.2);
    this.physics.add.collider(this.player, this.groundGroup);
    const playerGravity = gameConfig.get('playerPhysics.gravity') || 800;
    this.player.setGravityY(playerGravity); // Default gravity from config
    this.player.setDepth(6);
    this.player.body.setCollideWorldBounds();
    this.player.setSize(this.player.width / 2, this.player.height - 30);
    this.player.setOffset(this.player.width / 2 - 20, 30);

    this.createAnimations('run', 'player', 0, 5, -1, 12);
    this.createAnimations('jump', 'player', 0, 0, -1, 1);
    // It's good practice to have an 'idle' animation if the player can be static
    this.createAnimations('idle', 'player', 0, 0, -1, 1); // Example: using jump frame as idle or a specific idle anim

    // Initialize Managers that depend on scene elements or other managers
    this.playerProgression.init();
    this.gameSpeedManager.setProgressionStage(this.playerProgression.getCurrentStage());
    this.playerController = new PlayerController(this, this.player, this.gameSpeedManager);
    this.playerController.init(); 
    this.gameUIManager.init();
    this.gameUIManager.updateLevelName(this.playerProgression.getCurrentLevelName());

    // Initialize fluency provider for questions
    this.fluencyProvider = new FluencyProvider(window.gameConfig || {});

    // Initialize Answer Manager
    if (gameConfig.get('features.enableAnswerObstacles')) {
      this.answerManager = new AnswerManager(this, this.fluencyProvider);
      this.answerManager.init(
        this.sys.game.config.width + 200, // Start slightly off-screen
        gameState.sceneHeight - 300       // Same height as player
      );
      
      // Start with initial question set
      this.answerManager.spawnNewQuestion();
    }

    // Birds SECTION
    this.birdGroup = this.physics.add.group();
    const createBird = () => {
      const myY = Phaser.Math.Between(100, 300);
      // Birds move independently of game scroll speed for now, or adjust their velocity
      const bird = this.birdGroup.create(gameState.sceneWidth + 100, myY, 'bird').setScale(0.3);
      const birdSpeed = gameConfig.get('birds.speed') || -100;
      bird.setVelocityX(birdSpeed); // This speed is absolute, not relative to scroll
      bird.flipX = true;
      bird.setDepth(6);
      bird.setSize(bird.displayWidth - 10, bird.displayHeight - 10);
    };
    this.createAnimations('fly', 'bird', 0, 8, -1, 7);
    
    const birdDelayMin = gameConfig.get('birds.creationDelayMin') || 2500;
    const birdDelayMax = gameConfig.get('birds.creationDelayMax') || 5000;
    
    // Only create birds timer if enabled in config
    const enableBirds = gameConfig.get('features.enableBirds') || false;
    if (enableBirds) {
      this.birdCreationTime = this.time.addEvent({
        callback: createBird,
        delay: Phaser.Math.Between(birdDelayMin, birdDelayMax),
        callbackScope: this,
        loop: true,
      });
    }

    // Coins SECTION
    this.coinGroup = this.physics.add.group();
    const createCoin = () => {
      this.createBirdDrop(this.coinGroup, 'coin');
    };
    this.physics.add.collider(this.coinGroup, this.groundGroup, (singleCoin) => {
      // Coins on ground should move with the ground speed
      if (singleCoin.body.touching.down) {
          // Set a flag or make them kinematic
      }
    });
    this.physics.add.overlap(this.player, this.coinGroup, (player, singleCoin) => {
      this.pickCoin.play();
      singleCoin.destroy();
      gameState.score += 1;
      const coinHealthGain = gameConfig.get('health.gainFromCoin') || 1;
      this.health = Phaser.Math.Clamp(this.health + coinHealthGain, 0, maxHealth);
      this.updateHealthBar();
      this.scoreValue.setText(`${gameState.score}`);
      this.hoveringTextScore(player, `+${coinHealthGain}`, '#0000ff');
    });
    
    const coinDelay = gameConfig.get('items.coinCreationDelay') || 1000;
    
    // Only create coin timer if enabled in config
    const enableCoinDrops = gameConfig.get('features.enableCoinDrops') || false;
    if (enableCoinDrops) {
      this.coinCreationTime = this.time.addEvent({
        callback: createCoin,
        delay: coinDelay,
        callbackScope: this,
        loop: true,
      });
    }

    // Spikes SECTION
    this.spikeGroup = this.physics.add.group();
    function createSpike() {
      this.createBirdDrop(this.spikeGroup, 'spike');
    }
    
    const spikeDelay = gameConfig.get('items.spikeCreationDelay') || 5000;
    this.spikeCreationTime = this.time.addEvent({
      callback: createSpike,
      delay: spikeDelay,
      callbackScope: this,
      loop: true,
    });
    
    this.physics.add.collider(this.spikeGroup, this.groundGroup, (singleSpike) => {
       // Similar to coins, spikes on ground should move with ground
    });
    
    this.physics.add.overlap(this.player, this.spikeGroup, (player, singleSpike) => {
      this.spikeSound.play();
      singleSpike.destroy();
      
      // Apply damage from spike
      const spikeDamage = gameConfig.get('items.spikeDamage') || 15;
      this.health = Phaser.Math.Clamp(this.health - spikeDamage, 0, maxHealth);
      this.updateHealthBar();
      this.hoveringTextScore(player, `-${spikeDamage} Health!`, '#FF0000', '#800080');
    });

    // Missiles SECTION
    this.missileGroup = this.physics.add.group();
    this.explosion = this.add.sprite(-100, -100, 'explosion').setScale(0.5).setDepth(8);
    this.createAnimations('explode', 'explosion', 0, 15, 0, 20);
    this.createAnimations('idle', 'explosion', 15, 15, -1, 1);
    this.explosion.play('idle', true);
    this.physics.add.collider(this.player, this.missileGroup, (player, missile) => {
      if (player.body.touching.down && missile.body.touching.up) {
        this.killMissile.play();
        player.setVelocityY(-300);
        missile.setVelocityY(300); // Make missile fall after being jumped on
        missile.body.enable = false; // Disable further collisions for this missile
        let message = '';
        if (missile.y < 350) { message += '+0.5'; this.missileScore += 0.5; }
        else { message += '+0.25'; this.missileScore += 0.25; }
        this.hoveringTextScore(player, message, '#00ff00');
      } else {
        this.explodeSound.play();
        
        // Apply damage based on missile height
        const highMissileDamage = gameConfig.get('missiles.damage.highMissile') || 15;
        const lowMissileDamage = gameConfig.get('missiles.damage.lowMissile') || 10;
        const damage = missile.isHighMissile ? highMissileDamage : lowMissileDamage;
        this.health = Phaser.Math.Clamp(this.health - damage, 0, maxHealth);
        this.updateHealthBar();
        
        missile.destroy();
        this.hoveringTextScore(player, `-${damage} Health!`, '#FF0000', '#FF0000');
        this.explosion.x = player.x;
        this.explosion.y = player.y;
        this.explosion.play('explode', true);
      }
    });

    // Bounds for despawning objects
    this.leftBound = this.add.rectangle(-50, 0, 10, gameState.sceneHeight, 0x000000).setOrigin(0);
    this.bottomBound = this.add.rectangle(0, gameState.sceneHeight, gameState.sceneWidth, 10, 0x000000).setOrigin(0);
    this.boundGroup = this.physics.add.staticGroup();
    this.boundGroup.add(this.leftBound);
    this.boundGroup.add(this.bottomBound);
    this.physics.add.collider(this.birdGroup, this.boundGroup, (singleBird) => singleBird.destroy());
    this.physics.add.collider(this.coinGroup, this.boundGroup, (singleCoin) => singleCoin.destroy());
    this.physics.add.collider(this.spikeGroup, this.boundGroup, (singleSpike) => singleSpike.destroy());
    this.physics.add.collider(this.missileGroup, this.boundGroup, (singleMissile) => singleMissile.destroy());

    // If config was already loaded in preload, apply it again to ensure all values are updated
    if (this.configLoaded) {
      this.applyConfigValues();
    }
  }

  // END of create function above

  // Helper method to update health bar
  updateHealthBar() {
    const maxHealth = gameConfig.get('health.maxHealth') || 120;
    // Ensure health doesn't exceed maxHealth
    this.health = Math.min(this.health, maxHealth);
    
    this.progressBar.clear();
    this.progressBar.fillStyle(0xFFD700, 1);
    this.progressBar.fillRect(170, 95, this.health, 10);
  }

  createAnimations(animKey, spriteKey, startFrame, endFrame, loopTimes, frameRate) {
    return (this.anims.create({
      key: animKey,
      frames: this.anims.generateFrameNumbers(spriteKey, { start: startFrame, end: endFrame }),
      frameRate,
      repeat: loopTimes,
    }));
  }

  addGameBackground() {
    this.add.image(gameState.sceneWidth / 2, gameState.sceneHeight / 2, 'sky').setScale(0.5);
    this.mountainGroup = this.add.group();
    this.firstMountain = this.mountainGroup.create(0, gameState.sceneHeight, 'mountains').setScale(0.5).setOrigin(0, 1);
    this.mountainWidth = this.firstMountain.displayWidth;
    createPlatform(this.mountainGroup, this.mountainWidth, 'mountains');
    this.plateauGroup = this.add.group();
    this.firstPlateau = this.plateauGroup.create(0, gameState.sceneHeight, 'plateau').setScale(0.5).setOrigin(0, 1);
    this.plateauWidth = this.firstPlateau.displayWidth;
    createPlatform(this.plateauGroup, this.plateauWidth, 'plateau');
    this.groundGroup = this.physics.add.group();
    this.first = this.groundGroup.create(0, this.scale.height, 'ground').setOrigin(0, 1).setScale(0.5);
    this.first.setImmovable(true);
    this.groundWidth = this.first.displayWidth;
    this.groundHeight = this.first.displayHeight;
    this.first.setSize(this.groundWidth * 2, this.groundHeight - 50);
    createPlatform(this.groundGroup, this.groundWidth, 'ground');
  }

  createBirdDrop(group, texture) {
    if (this.birdGroup.getLength() >= 2) {
      const birds = this.birdGroup.getChildren();
      const randomBirdIndex = Phaser.Math.Between(0, birds.length - 1);
      const child = birds[randomBirdIndex];
      if (child && child.active) { // Ensure bird is active
          const drop = group.create(child.x, child.y, texture).setScale(texture === 'spike' ? 0.1 : 0.05);
          drop.setGravityY(700);
          drop.setDepth(6);
          drop.setBounce(1);
          drop.setSize(drop.width - 200, drop.height - 200);
      }
    }
  }

  createMissile(height, texture) {
    const missile = this.missileGroup.create(gameState.sceneWidth + 100, height, texture);
    missile.setScale(0.1);
    missile.setDepth(6);
    missile.setSize(missile.width, missile.height - 300);
    missile.setOffset(0, 150);
    // Store missile type for damage calculation
    missile.isHighMissile = height < 350;
  }

  hoveringTextScore(player, message, strokeColor, fillColor = '#ffffff') {
    const singleScoreText = this.add.text(player.x, player.y - 20, message, { // Position slightly above player
      fontSize: '30px',
      fill: fillColor,
      fontFamily: '"Akaya Telivigala"',
      strokeThickness: 2,
      stroke: strokeColor,
    }).setDepth(7).setOrigin(0.5, 1); // Origin at bottom-center for better positioning
    this.tweens.add({
      targets: singleScoreText,
      alpha: 0,
      y: singleScoreText.y - 100,
      duration: 1000,
      ease: 'Linear',
      onComplete: () => singleScoreText.destroy(),
    });
  }

  addSoundEffects() {
    this.pickCoin = this.createSoundEffect('pickCoin', 0.3, false);
    this.explodeSound = this.createSoundEffect('explosion', 0.4, false);
    this.killMissile = this.createSoundEffect('killMissile', 0.1, false);
    this.jumpSound = this.createSoundEffect('jumpSound', 0.05, false); // Used by PlayerController
    this.spikeSound = this.createSoundEffect('spikeSound', 0.2, false);
    
    // Add new sound effects for answer obstacles
    try {
      this.correctSound = this.createSoundEffect('correct_answer', 0.3);
      this.wrongSound = this.createSoundEffect('wrong_answer', 0.3);
      this.speedUpSound = this.createSoundEffect('speed_up', 0.3);
    } catch (error) {
      console.warn("Could not load some answer-related sound effects. Using fallbacks.");
      // Use existing sounds as fallbacks
      this.correctSound = this.pickCoin;
      this.wrongSound = this.explodeSound;
      this.speedUpSound = this.jumpSound;
    }
  }

  createSoundEffect(soundKey, volumeLevel, loopStatus = false) {
    try {
      const effect = this.sound.add(soundKey, { loop: loopStatus });
      effect.volume = volumeLevel;
      return effect;
    } catch (error) {
      console.warn(`Error creating sound effect '${soundKey}': ${error.message}`);
      // Return a dummy object that won't crash when methods are called
      return {
        play: () => {},
        pause: () => {},
        resume: () => {},
        stop: () => {},
        destroy: () => {},
        get volume() { return volumeLevel; },
        set volume(val) {},
        get isPlaying() { return false; },
        get isPaused() { return false; }
      };
    }
  }

  async displayFluencyQuestion() {
    if (!this.fluencyProvider || !this.gameUIManager) return;

    const questions = await this.fluencyProvider.getNextQuestionBlock();
    if (questions && questions.length > 0) {
      this.currentFluencyQuestion = questions[0];
      this.questionDisplayTime = Date.now();

      const questionTextContent = this.fluencyProvider.formatQuestionForDisplay(this.currentFluencyQuestion);
      const options = this.fluencyProvider.generateMultipleChoiceOptions(this.currentFluencyQuestion, 4);
      
      this.gameUIManager.displayQuestion(questionTextContent, options, (option, buttonElement) => {
        this.handleAnswerSubmission(option, buttonElement);
      });

    } else {
      console.log('No questions available from FluencyProvider.');
      this.resumeGameAfterQuestion();
    }
  }

  async handleAnswerSubmission(selectedAnswer, clickedButtonElement) {
    if (!this.currentFluencyQuestion || !this.gameUIManager || !this.playerProgression || !this.gameSpeedManager) return;

    const responseTimeMs = Date.now() - this.questionDisplayTime;
    let processedSelectedAnswer = selectedAnswer;
    if (typeof this.currentFluencyQuestion.answer === 'number') {
      const numericAttempt = parseFloat(selectedAnswer);
      if (!isNaN(numericAttempt)) {
        processedSelectedAnswer = numericAttempt;
      } else {
        console.warn('Could not parse selected answer to a number:', selectedAnswer);
      }
    }

    const result = await this.fluencyProvider.submitAnswer(
      this.currentFluencyQuestion.id,
      processedSelectedAnswer,
      responseTimeMs
    );

    console.log('Answer submitted:', processedSelectedAnswer, 'Result:', result);
    this.gameUIManager.updateButtonStyles(clickedButtonElement, result.isCorrect, this.currentFluencyQuestion.answer.toString());

    // Log current stage and correct count before processing
    console.log(`Current stage before processing: ${this.playerProgression.getCurrentStage()}, Correct count: ${this.playerProgression.correctAnswerCountInStage}`);
    
    const stageBeforeAnswer = this.playerProgression.getCurrentStage();
    const stageChanged = this.playerProgression.processAnswer(result.isCorrect);
    const currentStage = this.playerProgression.getCurrentStage();
    
    // Log if we advanced or not
    console.log(`Stage after processing: ${currentStage}, Stage changed: ${stageChanged}`);
    
    const maxHealth = gameConfig.get('health.maxHealth') || 120;

    if (result.isCorrect) {
      const healthGain = gameConfig.get('health.gainFromCorrectAnswer') || 5;
      this.health = Phaser.Math.Clamp(this.health + healthGain, 0, maxHealth);
      this.updateHealthBar();
      this.hoveringTextScore(this.player, `+${healthGain} Health!`, '#00FF00');
      if (currentStage === PROGRESSION_STAGES.AUTO_RUN_VARIABLE_SPEED) {
          this.gameUIManager.showFeedback('Correct! Speed Up!');
      } else {
          this.gameUIManager.showFeedback('Correct!');
      }
    } else {
      const healthPenalty = gameConfig.get('health.penaltyFromWrongAnswer') || 10;
      this.health = Phaser.Math.Clamp(this.health - healthPenalty, 0, maxHealth);
      this.updateHealthBar();
      this.hoveringTextScore(this.player, `-${healthPenalty} Health!`, '#FF0000');
      if (currentStage === PROGRESSION_STAGES.AUTO_RUN_VARIABLE_SPEED) {
        this.gameUIManager.showFeedback('Incorrect! Slowing Down!');
      } else {
        this.gameUIManager.showFeedback('Incorrect!');
      }
    }

    this.gameSpeedManager.processAnswer(result.isCorrect); // Adjust speed if in variable speed mode

    if (stageChanged) {
      this.gameUIManager.updateLevelName(this.playerProgression.getCurrentLevelName());
      
      if (currentStage > stageBeforeAnswer) {
        // We leveled up
        this.gameUIManager.showFeedback(`Level Up: ${this.playerProgression.getCurrentLevelName()}!`, 3000);
      } else {
        // We leveled down
        this.gameUIManager.showFeedback(`Level Down: ${this.playerProgression.getCurrentLevelName()}`, 3000);
      }
      
      this.gameSpeedManager.setProgressionStage(currentStage);
      
      // Update question interval based on new stage using config
      const intervals = gameConfig.get('questions.intervalByStage');
      if (intervals && intervals[currentStage] !== undefined) {
        this.questionInterval = intervals[currentStage];
        console.log(`Updated question interval to ${this.questionInterval}ms for stage ${currentStage}`);
      }
    }

    // Determine next action based on correctness
    if (result.isCorrect) {
      setTimeout(() => {
        this.resumeGameAfterQuestion();
      }, 1000); // Short delay for correct answer feedback
    } else {
      setTimeout(() => {
        // If wrong, resume after penalty feedback
        this.resumeGameAfterQuestion();
      }, 3000); // Longer delay for incorrect answer feedback
    }
  }

  pauseGameForQuestion() {
    if (this.isGamePaused) return;
    this.isGamePaused = true;

    if (this.gameTheme && this.gameTheme.isPlaying) {
        this.gameTheme.pause();
    }
    this.physics.pause();
    if (this.playerController) this.playerController.pause();
    this.birdGroup.children.iterate(child => { if (child.anims) child.anims.pause(); });

    this.time.paused = true; // Pauses all phaser time events
    
    // Ensure player sprite itself has no velocity if it was moving manually
    if(this.player && this.player.body) {
        this.player.setVelocityX(0);
    }
    
    console.log('Game paused for question.');
    this.displayFluencyQuestion();
  }

  resumeGameAfterQuestion() {
    if (!this.isGamePaused) return;
    this.isGamePaused = false;

    if (this.gameUIManager) {
        this.gameUIManager.hideQuestionUI();
    }
    this.currentFluencyQuestion = null;

    if (this.gameTheme && this.gameTheme.isPaused) {
        this.gameTheme.resume();
    }
    this.physics.resume();
    const currentStage = this.playerProgression.getCurrentStage();
    const currentSpeed = this.gameSpeedManager.getCurrentSpeed();
    if (this.playerController) this.playerController.resume(currentStage, currentSpeed);
    this.birdGroup.children.iterate(child => { if (child.anims) child.anims.resume(); });

    this.time.paused = false; // Resumes all phaser time events

    this.questionTriggerTimer = 0; // Reset timer for next question
    console.log('Game resumed after question.');
  }

  update(time, delta) {
    if (this.isGamePaused) return;

    // Player input handling
    this.playerController.handleInput(this.playerProgression.getCurrentStage());
    
    // Get current progression stage
    const currentStage = this.playerProgression.getCurrentStage();
    
    // Ensure speed is maintained in auto-run modes
    if (currentStage === PROGRESSION_STAGES.AUTO_RUN_INITIAL || 
        currentStage === PROGRESSION_STAGES.AUTO_RUN_VARIABLE_SPEED) {
      // Force speed reset if we're in auto-run mode and speed is 0
      if (this.gameSpeedManager.getCurrentSpeed() <= 0) {
        this.gameSpeedManager.resetToStageSpeed();
      }
    }
    
    // Get effective scroll speed and current speed
    const effectiveSpeed = this.gameSpeedManager.getEffectiveSpeed(delta);
    const currentSpeed = this.gameSpeedManager.getCurrentSpeed();
    
    // Update player animations
    this.playerController.updateAnimations(currentStage, currentSpeed);

    // Move background elements
    this.moveEnvironment(effectiveSpeed);
    
    // Update answer obstacles if enabled
    if (this.answerManager && gameConfig.get('features.enableAnswerObstacles')) {
      this.answerManager.update(effectiveSpeed);
    }

    // Question timer for traditional popup questions (might be deprecated if using obstacles)
    if (!gameConfig.get('features.enableAnswerObstacles')) {
      this.questionTriggerTimer += delta;
      if (this.questionTriggerTimer >= this.questionInterval) {
        this.displayFluencyQuestion();
        this.questionTriggerTimer = 0;
      }
    }

    if (this.health <= 0) {
      const myUrl = `${fetchScoreData.apiUrl + fetchScoreData.apiKey}/scores`;
      fetchScoreData.postScores(myUrl, { user: gameState.playerName, score: gameState.score });
      this.gameTheme.stop();
      this.scene.stop('Game'); // Ensure correct key is used
      this.scene.start('GameOver');
    }

    if (this.missileScore >= 1) {
      const healthBoost = gameConfig.get('health.boostFromMissileScore') || 10;
      const maxHealth = gameConfig.get('health.maxHealth') || 120;
      this.health = Phaser.Math.Clamp(this.health + healthBoost, 0, maxHealth);
      this.updateHealthBar();
      this.missileScore -= 1;
      this.hoveringTextScore(this.player, `+${healthBoost} Health Boost!`, '#00FFFF');
    }

    // Bird animations are independent of scroll speed (they fly on their own)
    this.birdGroup.children.iterate((child) => {
      if (child.active && child.anims) child.anims.play('fly', true);
    });

    // Missile movement (horizontal) - get speeds from config
    const missileScrollInfluence = gameConfig.get('missiles.scrollInfluence') || 0.03;
    const missileSpeedByStage = gameConfig.getSection('missiles.speedByStage') || {
      [PROGRESSION_STAGES.MANUAL_CONTROL]: 2,
      [PROGRESSION_STAGES.AUTO_RUN_INITIAL]: 3,
      [PROGRESSION_STAGES.AUTO_RUN_VARIABLE_SPEED]: 4
    };
    
    this.missileGroup.children.iterate((child) => {
      if (child.active) {
        // Get base missile speed for current stage and add small influence from scroll speed
        const baseMissileSpeed = missileSpeedByStage[currentStage] || missileSpeedByStage[PROGRESSION_STAGES.MANUAL_CONTROL];
        const missileSpeed = baseMissileSpeed + (currentSpeed * missileScrollInfluence);
        child.x -= missileSpeed;
      }
    });

    // Missile creation timers - adjust frequency based on level and config
    const missileConfig = gameConfig.getSection('missiles.creationFrequency') || {
      baseMissile: { baseTime: 5000, levelMultiplier: 1000 },
      secondMissile: { baseTime: 7000, levelMultiplier: 2000 }
    };
    
    // Only create missiles if enabled in config
    const enableRockets = gameConfig.get('features.enableRockets') || false;
    if (enableRockets) {
      this.timer += delta;
      const missileFrequency = missileConfig.baseMissile.baseTime + 
                             (missileConfig.baseMissile.levelMultiplier * (2 - currentStage));
      if (this.timer >= missileFrequency) {
        this.createMissile(415, 'missile');
        this.timer = 0;
      }
      
      this.secondTimer += delta;
      const missile2Frequency = missileConfig.secondMissile.baseTime + 
                              (missileConfig.secondMissile.levelMultiplier * (2 - currentStage));
      if (this.secondTimer >= missile2Frequency) {
        this.createMissile(300, 'missile2');
        this.secondTimer = 0;
      }
    }
  }

  // Add a method to handle environment movement with parallax
  moveEnvironment(effectiveSpeed) {
    // Get parallax factors from config or use defaults
    const parallaxFactors = gameConfig.get('environment.parallaxFactors') || {
      mountains: 0.25,
      plateau: 0.5,
      ground: 1.0
    };
    
    // Background movement with parallax, using effective speed for frame-rate independence
    moveBackgroundPlatform(this.mountainGroup, this.mountainWidth, 'mountains', effectiveSpeed * parallaxFactors.mountains);
    moveBackgroundPlatform(this.plateauGroup, this.plateauWidth, 'plateau', effectiveSpeed * parallaxFactors.plateau);
    moveBackgroundPlatform(this.groundGroup, this.groundWidth, 'ground', effectiveSpeed * parallaxFactors.ground);
    
    // Update items that should scroll with the ground if they are on it
    this.coinGroup.getChildren().forEach(coin => {
      if (coin.body.touching.down || coin.y + coin.displayHeight/2 >= gameState.sceneHeight - (this.groundHeight*0.5)/2) { // Approx on ground
        coin.x -= effectiveSpeed;
        if (coin.body && !coin.body.immovable) coin.setVelocityX(0); // Stop its own physics-driven X movement if on ground
      }
    });
    
    this.spikeGroup.getChildren().forEach(spike => {
      if (spike.body.touching.down || spike.y + spike.displayHeight/2 >= gameState.sceneHeight - (this.groundHeight*0.5)/2) {
        spike.x -= effectiveSpeed;
        if (spike.body && !spike.body.immovable) spike.setVelocityX(0);
      }
    });
  }
}

export default Game;