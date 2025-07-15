export default class QuestionUI {
    constructor(scene) {
        this.scene = scene;
        this.domElement = null;
        this.container = null;
        this.questionText = null;
        this.answersContainer = null;
        this._createDOM();
    }

    _createDOM() {
        const questionContainer = document.createElement('div');
        questionContainer.id = 'question-container';
        // Styles from original createQuestionUI
        questionContainer.style.position = 'absolute';
        questionContainer.style.top = '50%';
        questionContainer.style.left = '50%';
        questionContainer.style.transform = 'translate(-50%, -50%)';
        questionContainer.style.backgroundImage = "url('assets/question_popup.png')";
        questionContainer.style.backgroundSize = '100% 100%';
        questionContainer.style.backgroundRepeat = 'no-repeat';
        questionContainer.style.backgroundColor = 'transparent';
        questionContainer.style.padding = '20px';
        questionContainer.style.borderRadius = '10px';
        questionContainer.style.color = 'white';
        questionContainer.style.fontFamily = 'Arial, sans-serif';
        questionContainer.style.zIndex = '100';
        questionContainer.style.display = 'none'; // Initially hidden
        questionContainer.style.textAlign = 'center';
        questionContainer.style.minWidth = '400px';

        const questionTextElement = document.createElement('p');
        questionTextElement.id = 'question-text';
        questionTextElement.textContent = 'Question will appear here.';
        // Styles for questionText from original createQuestionUI
        questionTextElement.style.color = 'black';
        questionTextElement.style.fontWeight = 'bold';
        questionTextElement.style.fontSize = '1.2em';
        questionTextElement.style.textShadow = '0 0 2px white, 0 0 2px white, 0 0 2px white, 0 0 2px white';
        questionContainer.appendChild(questionTextElement);

        const answersDiv = document.createElement('div');
        answersDiv.id = 'answers-container';
        answersDiv.style.marginTop = '15px';
        questionContainer.appendChild(answersDiv);

        // Add to Phaser DOM
        this.domElement = this.scene.add.dom(0, 0, questionContainer).setOrigin(0,0);
        this.domElement.setVisible(false); // Keep it hidden initially

        this.container = questionContainer;
        this.questionText = questionTextElement;
        this.answersContainer = answersDiv;
    }

    displayQuestion(questionTextContent, options, answerSubmissionCallback) {
        this.questionText.textContent = questionTextContent;
        this.answersContainer.innerHTML = ''; // Clear previous options

        options.forEach(option => {
            const button = document.createElement('button');
            button.textContent = option;
            // Button styles from original displayFluencyQuestion
            button.style.margin = '5px';
            button.style.padding = '10px 15px';
            button.style.cursor = 'pointer';
            button.style.backgroundImage = "url('assets/answer_idle_bg.png')";
            button.style.backgroundSize = '100% 100%';
            button.style.backgroundRepeat = 'no-repeat';
            button.style.backgroundColor = 'transparent';
            button.style.border = 'none';
            button.style.color = 'white';
            button.style.fontWeight = 'bold';
            button.style.fontSize = '1.1em';
            button.style.textShadow = '0 0 2px black, 0 0 2px black, 0 0 2px black, 0 0 2px black';
            button.style.textAlign = 'center';

            button.onclick = () => answerSubmissionCallback(option, button);
            this.answersContainer.appendChild(button);
        });
        this.show();
    }

    updateButtonStyles(clickedButton, isCorrect, correctAnswer) {
        const allButtons = Array.from(this.answersContainer.querySelectorAll('button'));
        allButtons.forEach(btn => { btn.disabled = true; });

        const idleButtonBg = "url('assets/answer_idle_bg.png')";
        const correctButtonBg = "url('assets/answer_right_bg.png')";
        const wrongButtonBg = "url('assets/answer_wrong_bg.png')";

        if (isCorrect) {
            clickedButton.style.backgroundImage = correctButtonBg;
        } else {
            clickedButton.style.backgroundImage = wrongButtonBg;
            const correctAnswerString = String(correctAnswer);
            allButtons.forEach(btn => {
                if (btn.textContent === correctAnswerString) {
                    btn.style.backgroundImage = correctButtonBg;
                }
            });
        }
    }

    resetButtonStyles() {
        const allButtons = Array.from(this.answersContainer.querySelectorAll('button'));
        const idleButtonBg = "url('assets/answer_idle_bg.png')";
        allButtons.forEach(btn => {
            btn.disabled = false;
            btn.style.backgroundImage = idleButtonBg;
        });
    }
    
    clearAnswers() {
        if (this.answersContainer) {
            this.answersContainer.innerHTML = '';
        }
    }

    show() {
        if (this.domElement) {
            this.domElement.setVisible(true);
        }
    }

    hide() {
        if (this.domElement) {
            this.domElement.setVisible(false);
        }
        this.clearAnswers();
    }
} 