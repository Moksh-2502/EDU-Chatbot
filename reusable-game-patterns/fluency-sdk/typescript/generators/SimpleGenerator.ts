import type { 
  FluencyGenerator, 
  FluencyGeneratorConfig, 
  Question, 
  StudentState 
} from '../types';
import type { StorageAdapter } from '../utils';

export class SimpleGenerator implements FluencyGenerator {
  private config: FluencyGeneratorConfig;
  private state: StudentState = {
    currentPosition: 0,
    learnedFacts: {},
    mode: 'learning'
  };
  private defaultSequence = [0, 1, 10, 2, 5, 3, 4, 6, 7, 8, 9, 11, 12];
  private questions: Record<string, Question> = {};
  private storage: StorageAdapter;
  
  /**
   * Creates a new SimpleGenerator with optional configuration
   * @param config Optional configuration for the generator
   * @param storageKey Key to use for storage (defaults to 'fluencyState')
   * @param storageAdapter Custom storage adapter for platforms without localStorage
   */
  constructor(
    config?: FluencyGeneratorConfig, 
    private storageKey = 'fluencyState',
    storageAdapter?: StorageAdapter
  ) {
    this.config = {
      sequence: config?.sequence || this.defaultSequence,
      maxFactor: config?.maxFactor || 12,
      questionsPerBlock: config?.questionsPerBlock || 5
    };
    
    // Use provided storage adapter or default to localStorage
    this.storage = storageAdapter || {
      getItem: (key: string) => {
        return typeof localStorage !== 'undefined' ? localStorage.getItem(key) : null;
      },
      setItem: (key: string, value: string) => {
        if (typeof localStorage !== 'undefined') {
          localStorage.setItem(key, value);
        }
      },
      removeItem: (key: string) => {
        if (typeof localStorage !== 'undefined') {
          localStorage.removeItem(key);
        }
      }
    };
    
    this.loadState();
  }
  
  private resetState() {
    this.state = {
      currentPosition: 0,
      learnedFacts: {},
      mode: 'learning'
    };
  }
  
  private generateQuestion(focus: number): Question {
    const id = `q_${Date.now()}_${Math.random().toString(36).substring(2, 9)}`;
    let factors: number[];
    
    if (this.state.mode === 'learning') {
      // In learning mode, one factor is always the current focus
      const otherFactor = Math.floor(Math.random() * (this.config.maxFactor || 12) + 1);
      factors = [focus, otherFactor];
    } else {
      // In placement or reinforcement, choose random factors
      const factor1 = Math.floor(Math.random() * (this.config.maxFactor || 12) + 1);
      const factor2 = Math.floor(Math.random() * (this.config.maxFactor || 12) + 1);
      factors = [factor1, factor2];
    }
    
    const question: Question = {
      id,
      factors,
      answer: factors[0] * factors[1]
    };
    
    this.questions[id] = question;
    return question;
  }
  
  /**
   * Gets the next block of questions based on the current state
   * @returns Promise that resolves to an array of Question objects
   */
  public async getNextQuestionBlock(): Promise<Question[]> {
    const questions: Question[] = [];
    const questionsPerBlock = this.config.questionsPerBlock || 5;
    
    if (this.state.mode === 'learning') {
      // In learning mode, focus on current position in sequence
      const currentFocus = this.config.sequence?.[this.state.currentPosition] || 0;
      
      for (let i = 0; i < questionsPerBlock; i++) {
        questions.push(this.generateQuestion(currentFocus));
      }
    } else if (this.state.mode === 'placement') {
      // In placement mode, sample across the entire range
      for (let i = 0; i < questionsPerBlock; i++) {
        const randomIndex = Math.floor(Math.random() * (this.config.sequence?.length || 12));
        const focus = this.config.sequence?.[randomIndex] || randomIndex;
        questions.push(this.generateQuestion(focus));
      }
    } else {
      // In reinforcement mode, use previously learned facts
      // Simple implementation just uses random facts for now
      for (let i = 0; i < questionsPerBlock; i++) {
        const maxPosition = Math.min(
          this.state.currentPosition, 
          (this.config.sequence?.length || 12) - 1
        );
        const randomIndex = Math.floor(Math.random() * (maxPosition + 1));
        const focus = this.config.sequence?.[randomIndex] || randomIndex;
        questions.push(this.generateQuestion(focus));
      }
    }
    
    // Set timestamp when questions are presented
    questions.forEach(q => {
      q.timeStarted = Date.now();
      this.questions[q.id] = q;
    });
    
    return questions;
  }
  
  /**
   * Submits an answer for a given question and updates the state accordingly
   * @param questionId The ID of the question being answered
   * @param answer The user's answer
   * @param responseTimeMs The response time in milliseconds
   * @returns Promise that resolves to an object indicating if the answer was correct and the correct answer
   */
  public async submitAnswer(
    questionId: string, 
    answer: number, 
    responseTimeMs: number
  ): Promise<{ isCorrect: boolean; correctAnswer?: number }> {
    const question = this.questions[questionId];
    if (!question) {
      return { isCorrect: false };
    }
    
    question.userAnswer = answer;
    question.timeEnded = Date.now();
    question.isCorrect = answer === question.answer;
    
    // Update learned facts
    const factKey = `${question.factors[0]}x${question.factors[1]}`;
    if (!this.state.learnedFacts[factKey]) {
      this.state.learnedFacts[factKey] = {
        lastSeen: Date.now(),
        timesCorrect: 0,
        timesIncorrect: 0,
        averageResponseTime: responseTimeMs
      };
    } else {
      const fact = this.state.learnedFacts[factKey];
      fact.lastSeen = Date.now();
      if (question.isCorrect) {
        fact.timesCorrect++;
      } else {
        fact.timesIncorrect++;
      }
      
      // Update average response time
      const totalResponses = fact.timesCorrect + fact.timesIncorrect;
      fact.averageResponseTime = (
        (fact.averageResponseTime * (totalResponses - 1)) + responseTimeMs
      ) / totalResponses;
    }
    
    // In learning mode, advance position if enough correct answers
    if (
      this.state.mode === 'learning' && 
      question.isCorrect &&
      question.factors.includes(this.config.sequence?.[this.state.currentPosition] || 0)
    ) {
      const currentFocus = this.config.sequence?.[this.state.currentPosition] || 0;
      const factKeys: string[] = [];
      for (let i = 0; i <= (this.config.maxFactor || 12); i++) {
        factKeys.push(`${currentFocus}x${i}`);
        factKeys.push(`${i}x${currentFocus}`);
      }
      
      // Count correct answers for current focus
      let correctCount = 0;
      const requiredCount = 5; // Arbitrary threshold
      
      factKeys.forEach(key => {
        if (this.state.learnedFacts[key]?.timesCorrect > 0) {
          correctCount++;
        }
      });
      
      if (correctCount >= requiredCount) {
        this.state.currentPosition++;
      }
    }
    
    await this.saveState();
    
    return { 
      isCorrect: question.isCorrect, 
      correctAnswer: question.answer 
    };
  }
  
  /**
   * Saves the current state to storage
   * @returns Promise that resolves when the state has been saved
   */
  private async saveState(): Promise<void> {
    this.storage.setItem(this.storageKey, JSON.stringify(this.state));
  }
  
  /**
   * Loads the state from storage
   * @returns Promise that resolves when the state has been loaded
   */
  private async loadState(): Promise<void> {
    const savedState = this.storage.getItem(this.storageKey);
    if (savedState) {
      try {
        this.state = JSON.parse(savedState);
      } catch (e) {
        console.error('Error loading saved state:', e);
        this.resetState();
      }
    } else {
      this.resetState();
    }
  }
  
  /**
   * Gets the current state
   * @returns Promise that resolves to a copy of the current state
   */
  public async getState(): Promise<StudentState> {
    return { ...this.state };
  }
  
  /**
   * Sets the state
   * @param state The new state
   * @returns Promise that resolves when the state has been set
   */
  public async setState(state: StudentState): Promise<void> {
    this.state = { ...state };
    await this.saveState();
  }
  
  /**
   * Resets the state to the initial values
   * @returns Promise that resolves when the state has been reset
   */
  public async reset(): Promise<void> {
    this.resetState();
    await this.saveState();
  }
  
  /**
   * Sets the learning mode
   * @param mode The mode to set
   * @returns Promise that resolves when the mode has been set
   */
  public async setMode(mode: 'learning' | 'placement' | 'reinforcement'): Promise<void> {
    this.state.mode = mode;
    await this.saveState();
  }
} 