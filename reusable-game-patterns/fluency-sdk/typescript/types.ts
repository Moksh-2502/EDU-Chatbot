export interface Question {
  id: string;
  factors: number[];
  answer: number;
  timeStarted?: number;
  timeEnded?: number;
  userAnswer?: number;
  isCorrect?: boolean;
}

export interface LearnedFact {
  lastSeen: number;
  timesCorrect: number;
  timesIncorrect: number;
  averageResponseTime: number;
}

export interface StudentState {
  currentPosition: number;
  learnedFacts: Record<string, LearnedFact>;
  mode: 'learning' | 'placement' | 'reinforcement';
}

export interface FluencyGeneratorConfig {
  sequence?: number[];
  maxFactor?: number;
  questionsPerBlock?: number;
}

export interface FluencyGenerator {
  getNextQuestionBlock(): Promise<Question[]>;
  submitAnswer(questionId: string, answer: number, responseTimeMs: number): Promise<{ 
    isCorrect: boolean; 
    correctAnswer?: number 
  }>;
  getState(): Promise<StudentState>;
  setState(state: StudentState): Promise<void>;
  reset(): Promise<void>;
} 