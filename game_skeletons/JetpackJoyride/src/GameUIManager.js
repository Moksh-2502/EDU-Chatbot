import QuestionUI from './QuestionUI';

const FEEDBACK_DURATION = 2000; // ms
const LEVEL_NAME_Y_OFFSET = 30; // y-position for level name, now at top right
const FEEDBACK_TEXT_Y_OFFSET = 160; // y-position for feedback text
const QUESTION_TEXT_Y_OFFSET = 125; // y-position for the current question

class GameUIManager {
  constructor(scene) {
    this.scene = scene;
    this.levelNameText = null;
    this.feedbackText = null;
    this.questionUI = null;
    this.questionTextDisplay = null; // New text display for current question
    this.currentQuestionText = ""; // Store the current question text
  }

  init() {
    // Level Name Display - moved to top right
    this.levelNameText = this.scene.add.text(
      this.scene.scale.width - 20, 
      LEVEL_NAME_Y_OFFSET, 
      '', 
      {
        fontSize: '22px',
        fill: '#00BFFF', // Deep sky blue color for level name
        fontFamily: '"Akaya Telivigala"',
        strokeThickness: 3,
        stroke: '#000000',
      }
    ).setOrigin(1, 0.5).setDepth(10); // Right-aligned at top right

    // Question Text Display - in the position where level name was
    this.questionTextDisplay = this.scene.add.text(
      this.scene.scale.width / 2, 
      QUESTION_TEXT_Y_OFFSET, 
      '', 
      {
        fontSize: '28px',
        fill: '#FFFF00', // Bright yellow (same as old level name)
        fontFamily: '"Akaya Telivigala"',
        strokeThickness: 4,
        stroke: '#000000',
      }
    ).setOrigin(0.5, 0.5).setDepth(10);

    // Feedback Text Display (e.g., for "Speed Up", "Level Up")
    this.feedbackText = this.scene.add.text(
      this.scene.scale.width / 2, 
      FEEDBACK_TEXT_Y_OFFSET, 
      '', 
      {
        fontSize: '24px',
        fill: '#FF69B4', // Hot pink for feedback
        fontFamily: '"Akaya Telivigala"',
        strokeThickness: 3,
        stroke: '#FFFFFF',
      }
    ).setOrigin(0.5, 0.5).setDepth(10).setAlpha(0); // Initially hidden

    // Initialize QuestionUI
    this.questionUI = new QuestionUI(this.scene);
    // QuestionUI likely creates its own DOM elements and handles its visibility.
    // We don't need to add it to the scene display list if it manages its own DOM elements.

    console.log('GameUIManager initialized.');
  }

  updateLevelName(name) {
    if (this.levelNameText) {
      this.levelNameText.setText(name);
      console.log(`UI Level Name Updated: ${name}`);
    }
  }

  // New method to update the displayed question text
  updateQuestionText(questionText) {
    if (this.questionTextDisplay) {
      this.currentQuestionText = questionText;
      this.questionTextDisplay.setText(questionText);
      console.log(`UI Question Updated: ${questionText}`);
    }
  }

  showFeedback(message, duration = FEEDBACK_DURATION) {
    if (this.feedbackText) {
      this.feedbackText.setText(message);
      this.feedbackText.setAlpha(1);
      console.log(`UI Feedback: ${message}`);

      // Clear existing tweens on feedbackText to prevent conflicts if called rapidly
      this.scene.tweens.killTweensOf(this.feedbackText);

      this.scene.tweens.add({
        targets: this.feedbackText,
        alpha: 0,
        delay: duration - 500, // Start fading a bit before duration ends
        duration: 500, // Fade out duration
        ease: 'Power2'
      });
    }
  }

  // Methods to interact with QuestionUI
  displayQuestion(questionText, options, answerCallback) {
    if (this.questionUI) {
      // Update the question display at the top
      this.updateQuestionText(questionText);
      
      // Display the answer options via the QuestionUI
      this.questionUI.displayQuestion(questionText, options, answerCallback);
    } else {
      console.error('QuestionUI not initialized in GameUIManager.');
    }
  }

  updateButtonStyles(clickedButtonElement, isCorrect, correctAnswer) {
    if (this.questionUI) {
      this.questionUI.updateButtonStyles(clickedButtonElement, isCorrect, correctAnswer);
    }
  }

  hideQuestionUI() {
    if (this.questionUI) {
      this.questionUI.hide();
      
      // Clear the question text when hiding the UI
      this.updateQuestionText("");
    }
  }
}

export default GameUIManager; 