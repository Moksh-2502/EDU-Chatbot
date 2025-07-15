import { PROGRESSION_STAGES } from './constants';
import gameConfig from './GameConfig';

class PlayerController {
  constructor(scene, playerSprite, gameSpeedManager) {
    this.scene = scene;
    this.player = playerSprite;
    this.gameSpeedManager = gameSpeedManager; // To signal manual movement speed
    this.cursors = this.scene.input.keyboard.createCursorKeys();
    
    // Load physics values from config
    this.jumpTimes = gameConfig.get('playerPhysics.maxJumps') || 2; // Max jumps allowed
    this.jumpStrength = gameConfig.get('playerPhysics.jumpStrength') || -400;
    this.defaultGravity = gameConfig.get('playerPhysics.gravity') || 800;
    this.fastFallGravity = gameConfig.get('playerPhysics.fastFallGravity') || 1300;
    this.fallSpeed = gameConfig.get('playerPhysics.fallSpeed') || 1000; // New falling gravity
    
    this.jumpCount = 0; // Current jumps made since last touch down
    this.isJumping = false; // Track if player is currently in a jump
  }

  // Call this method in Game scene's create, after player sprite and animations are set up
  init() {
    if (!this.player) {
        console.error('Player sprite not provided to PlayerController');
        return;
    }
    
    // Re-fetch config values in case they were loaded asynchronously
    this.jumpTimes = gameConfig.get('playerPhysics.maxJumps') || this.jumpTimes;
    this.jumpStrength = gameConfig.get('playerPhysics.jumpStrength') || this.jumpStrength;
    this.defaultGravity = gameConfig.get('playerPhysics.gravity') || this.defaultGravity;
    this.fastFallGravity = gameConfig.get('playerPhysics.fastFallGravity') || this.fastFallGravity;
    
    // Ensure fallSpeed is not too extreme (cap at 3000)
    this.fallSpeed = Math.min(
      gameConfig.get('playerPhysics.fallSpeed') || this.fallSpeed,
      3000
    );
    
    console.log(`Player physics: jumpTimes=${this.jumpTimes}, fallSpeed=${this.fallSpeed}`);
    
    // Set initial gravity
    if (this.player.body) {
      this.player.setGravityY(this.defaultGravity);
    }
    
    // Initial animation state
    this.updateAnimations(PROGRESSION_STAGES.MANUAL_CONTROL, 0);
  }

  // Debug method to log jump state
  logJumpState() {
    if (!this.player || !this.player.body) return;
    
    const onGround = this.player.body.touching.down;
    const velocity = this.player.body.velocity.y;
    const gravity = this.player.body.gravity.y;
    
    console.log(
      `Jump Debug - Count: ${this.jumpCount}/${this.jumpTimes}, ` +
      `OnGround: ${onGround}, Velocity: ${velocity.toFixed(1)}, ` +
      `Gravity: ${gravity}, isJumping: ${this.isJumping}`
    );
  }

  handleInput(progressionStage) {
    if (!this.player || !this.player.body) return;

    const onGround = this.player.body.touching.down;
    const playerVelocityY = this.player.body.velocity.y;

    // Reset jump count when landing on ground
    if (onGround) {
      this.jumpCount = 0;
      this.isJumping = false;
      this.player.setGravityY(this.defaultGravity); // Reset to default gravity when on ground
    } else {
      // Apply different gravity based on jump state
      if (this.cursors.down.isDown) {
        // Fast fall when down key is pressed
        this.player.setGravityY(this.fastFallGravity);
      } else if (playerVelocityY >= 0) {
        // Apply fall speed immediately when velocity becomes zero or positive
        // (at the apex of the jump and during descent)
        this.player.setGravityY(this.fallSpeed);
      } else {
        // During upward motion of jump, use default gravity
        this.player.setGravityY(this.defaultGravity);
      }
    }

    // Occasionally log jump state for debugging
    if (Math.random() < 0.01) {
      this.logJumpState();
    }

    // Handle Jump
    if (Phaser.Input.Keyboard.JustDown(this.cursors.up)) {
      // Allow jump if on ground OR if within the allowed multi-jump limit
      if (onGround || this.jumpCount < this.jumpTimes) {
        this.player.setVelocityY(this.jumpStrength); // Use configured jump strength
        this.scene.jumpSound.play(); // Assuming jumpSound is on the scene
        this.isJumping = true;
        this.jumpCount++; // Always increment jump count when a jump occurs
        
        console.log(`Jump performed. Jump count: ${this.jumpCount}`);
      }
    }

    // Handle Manual Forward Movement
    if (progressionStage === PROGRESSION_STAGES.MANUAL_CONTROL) {
      if (this.cursors.right.isDown) {
        this.gameSpeedManager.setManualForward(true);
      } else {
        this.gameSpeedManager.setManualForward(false);
      }
    } else if (progressionStage === PROGRESSION_STAGES.AUTO_RUN_INITIAL) {
      // Ensure auto-run mode is always active by resetting to stage speed
      this.gameSpeedManager.resetToStageSpeed();
      // In auto-run initial mode, ensure manual forward is off
      this.gameSpeedManager.setManualForward(false);
    } else if (progressionStage === PROGRESSION_STAGES.AUTO_RUN_VARIABLE_SPEED) {
      // Speed-up control for advanced stage
      if (Phaser.Input.Keyboard.JustDown(this.cursors.right)) {
        this.gameSpeedManager.speedUp();
      }
      // In auto-run variable speed mode, ensure we have speed
      this.gameSpeedManager.resetToStageSpeed();
    }
  }

  updateAnimations(progressionStage, currentSpeed) {
    if (!this.player || !this.player.anims) return;

    const onGround = this.player.body.touching.down;

    if (!onGround) {
      this.player.anims.play('jump', true);
    } else {
      if (progressionStage === PROGRESSION_STAGES.MANUAL_CONTROL) {
        if (currentSpeed > 0) {
          this.player.anims.play('run', true);
        } else {
          // Check if an 'idle' animation exists, otherwise stop or play a default frame
          if (this.player.anims.exists('idle')) {
            this.player.anims.play('idle', true);
          } else {
            this.player.anims.stop();
            // Optionally set a specific frame for idle, e.g., player.setFrame(0);
          }
        }
      } else { // Auto-run stages
        if (currentSpeed > 0) { // Should always be > 0 in auto-run unless paused
            this.player.anims.play('run', true);
        } else {
             // If speed is 0 in auto-run (e.g. game just started/resumed before speed kicks in, or paused)
            if (this.player.anims.exists('idle')) {
                this.player.anims.play('idle', true);
            } else {
                this.player.anims.stop();
            }
        }
      }
    }
  }

  pause() {
    if (this.player && this.player.anims) {
      this.player.anims.pause();
      // If you have an idle frame, you might want to set it explicitly
      // this.player.setFrame(IDLE_FRAME_INDEX);
    }
  }

  resume(progressionStage, currentSpeed) {
    if (this.player && this.player.anims) {
      this.player.anims.resume();
      // Immediately update animation based on current state after resume
      this.updateAnimations(progressionStage, currentSpeed);
    }
  }
}

export default PlayerController; 