import { FluencyGeneratorFactory } from './lib/fluency-sdk/index.esm.js';

export default class FluencyProvider {
  constructor(config = {}) {
    // Default configuration for the SDK
    const defaultConfig = {
      type: 'simple', // Or 'api' if you have an API backend
      options: {
        // SimpleGenerator options (if type is 'simple')
        config: {
          // Example: define question sequences or topics if your SDK supports it
          // sequence: [0, 1, 2, 3, 4],
          // questionsPerBlock: 1, // Get one question at a time
        },
        // storageKey: 'endlessRunnerFluencyState', // Optional: custom storage key
        // storageAdapter: null, // Optional: custom storage adapter

        // ApiGenerator options (if type is 'api')
        // apiOptions: {
        //   apiBaseUrl: 'YOUR_API_BASE_URL',
        //   apiKey: 'YOUR_API_KEY', // Optional
        // },
      },
    };

    const generatorType = config.type || defaultConfig.type;
    const generatorOptions = { ...defaultConfig.options, ...config.options };

    this.fluencyGenerator = FluencyGeneratorFactory.create(generatorType, generatorOptions);
    this.currentQuestions = [];
  }

  /**
   * Fetches the next block of questions from the fluency generator.
   * @returns {Promise<Array<Object>>} A promise that resolves to an array of question objects.
   */
  async getNextQuestionBlock() {
    try {
      this.currentQuestions = await this.fluencyGenerator.getNextQuestionBlock();
      return this.currentQuestions;
    } catch (error) {
      console.error('Error fetching next question block:', error);
      return []; // Return an empty array or handle error as appropriate
    }
  }

  /**
   * Submits an answer for a given question.
   * @param {string} questionId - The ID of the question being answered.
   * @param {any} answer - The user's answer.
   * @param {number} responseTimeMs - The time taken to answer in milliseconds.
   * @returns {Promise<Object>} A promise that resolves to an object indicating if the answer was correct,
   *                            the correct answer, and other relevant feedback.
   */
  async submitAnswer(questionId, answer, responseTimeMs) {
    try {
      const result = await this.fluencyGenerator.submitAnswer(questionId, answer, responseTimeMs);
      return result;
    } catch (error) {
      console.error('Error submitting answer:', error);
      // Handle error as appropriate, e.g., return a default error response
      return { isCorrect: false, error: 'Failed to submit answer' };
    }
  }

  /**
   * Retrieves the current state from the fluency generator.
   * @returns {Promise<Object>} A promise that resolves to the current state object.
   */
  async getState() {
    try {
      const state = await this.fluencyGenerator.getState();
      return state;
    } catch (error) {
      console.error('Error getting state:', error);
      return {}; // Return an empty object or handle error
    }
  }

  /**
   * Sets the state of the fluency generator.
   * @param {Object} newState - The new state to set.
   * @returns {Promise<void>} A promise that resolves when the state has been set.
   */
  async setState(newState) {
    try {
      await this.fluencyGenerator.setState(newState);
    } catch (error) {
      console.error('Error setting state:', error);
    }
  }

  /**
   * Resets the fluency generator's state.
   * @returns {Promise<void>} A promise that resolves when the state has been reset.
   */
  async reset() {
    try {
      await this.fluencyGenerator.reset();
      this.currentQuestions = [];
    } catch (error) {
      console.error('Error resetting state:', error);
    }
  }

  /**
   * Formats a question for display.
   * (This is a helper, you might need to adjust it based on your SDK's question structure)
   * @param {Object} question - The question object from the SDK.
   * @returns {string} A formatted string representation of the question.
   */
  formatQuestionForDisplay(question) {
    if (!question) return 'No question available.';
    // Example: Assumes question object has a 'text' property or 'factors' for math
    if (question.text) {
      return question.text;
    }
    if (question.factors && question.factors.length === 2) {
      return `What is ${question.factors[0]} × ${question.factors[1]}?`;
    }
    return 'Question format not recognized.';
  }

  /**
   * Generates multiple choice answer options for a given question.
   * (This is a helper, adapt based on your SDK and question type)
   * @param {Object} question - The question object.
   * @param {number} numOptions - The number of multiple-choice options to generate.
   * @returns {Array<any>} An array of answer options, including the correct answer.
   */
  generateMultipleChoiceOptions(question, numOptions = 4) {
    if (!question || typeof question.answer === 'undefined') {
      return [];
    }

    const correctAnswer = question.answer;
    const options = [correctAnswer];

    // Simple dummy wrong answer generation. Replace with more sophisticated logic.
    for (let i = 0; options.length < numOptions; i++) {
      let wrongAnswer;
      if (typeof correctAnswer === 'number') {
        // Generate random numbers around the correct answer
        const offset = (Math.random() < 0.5 ? -1 : 1) * (Math.floor(Math.random() * 5) + 1);
        wrongAnswer = correctAnswer + offset;
        if (wrongAnswer < 0 && correctAnswer >=0) wrongAnswer = correctAnswer + (Math.floor(Math.random() * 5) + 1); // avoid negative if ans is positive
      } else if (typeof correctAnswer === 'string') {
        // For strings, you might pick from a predefined list of distractors or manipulate the string
        wrongAnswer = `Wrong Option ${options.length}`; // Placeholder
      } else {
        wrongAnswer = `Option ${options.length + 1}`; // Generic placeholder
      }

      if (!options.includes(wrongAnswer)) {
        options.push(wrongAnswer);
      }
       // safety break for difficult to generate options
      if (i > numOptions * 2 && options.length < numOptions) break;
    }
    
    // If not enough unique options generated, fill with placeholders
    while(options.length < numOptions) {
        options.push(`Option ${options.length + Math.random()}`);
    }


    // Shuffle options
    return options.sort(() => Math.random() - 0.5);
  }
} 