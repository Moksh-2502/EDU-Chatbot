# Fluency Generator Interface

All implementations of the Multiplication Fluency Generator follow this common interface to allow for easy replacement.

## Core Interfaces

```typescript
interface Question {
  id: string;
  factors: number[];          // For multiplication: [3, 4] represents 3×4
  answer: number;             // The correct answer (12 in example above)
  timeStarted?: number;       // When question was presented (timestamp)
  timeEnded?: number;         // When answer was submitted (timestamp)
  userAnswer?: number;        // What the student answered
  isCorrect?: boolean;        // Whether their answer was correct
}

interface StudentState {
  currentPosition: number;    // Current position in sequence
  learnedFacts: Record<string, {
    lastSeen: number;         // Timestamp when last practiced
    timesCorrect: number;     // Count of correct answers
    timesIncorrect: number;   // Count of incorrect answers
    averageResponseTime: number; // Average response time in ms
  }>;
  mode: 'placement' | 'learning' | 'reinforcement';
}

interface FluencyGeneratorConfig {
  sequence?: number[];        // Ordered sequence of multiplicands to teach
  maxFactor?: number;         // Upper limit for factors (typically 12)
  questionsPerBlock?: number; // How many questions to give at once
  spacingIntervals?: number[]; // Intervals for spaced repetition (in ms)
  randomizeWindow?: number;   // Randomization amount for fact selection
}

interface FluencyGenerator {
  // Initialize or reset with configuration
  initialize(config?: FluencyGeneratorConfig): Promise<void>;
  
  // Get the next block of questions
  getNextQuestionBlock(): Promise<Question[]>;
  
  // Submit an answer and get feedback
  submitAnswer(questionId: string, answer: number, responseTimeMs: number): Promise<{
    isCorrect: boolean;
    correctAnswer?: number;
  }>;
  
  // Get current student state
  getStudentState(): Promise<StudentState>;
  
  // Set the mode (placement, learning, reinforcement)
  setMode(mode: 'placement' | 'learning' | 'reinforcement'): Promise<void>;
  
  // Check if student has mastered all facts
  checkMastery(): Promise<{
    isMastered: boolean;
    masteredFacts: number;
    totalFacts: number;
    averageSpeed: number;
  }>;
}
```

## Factory Pattern

A factory pattern is used to create the appropriate implementation:

```typescript
class FluencyGeneratorFactory {
  static create(type: 'simple' | 'intermediate' | 'advanced'): FluencyGenerator {
    switch(type) {
      case 'simple':
        return new SimpleGenerator();
      case 'intermediate':
        return new IntermediateGenerator();
      case 'advanced':
        return new AdvancedGenerator();
      default:
        return new SimpleGenerator();
    }
  }
}

// Usage
const generator = FluencyGeneratorFactory.create('simple');
```

## Usage Example

```typescript
// Initialize generator
const generator = FluencyGeneratorFactory.create('simple');
generator.initialize({
  sequence: [0, 1, 10, 2, 5, 3, 4, 6, 7, 8, 9, 11, 12], 
  questionsPerBlock: 5
});

// Set mode (placement, learning, reinforcement)
await generator.setMode('learning');

// Get a block of questions
const questions = await generator.getNextQuestionBlock();

// Submit an answer
const result = await generator.submitAnswer(
  questions[0].id, 
  42, 
  1500 // response time in ms
);

// Check if correct
if (result.isCorrect) {
  console.log("Correct!");
} else {
  console.log(`Wrong! The answer is ${result.correctAnswer}`);
}

// Check mastery
const masteryInfo = await generator.checkMastery();
if (masteryInfo.isMastered) {
  console.log("All facts mastered!");
} else {
  console.log(`Progress: ${masteryInfo.masteredFacts}/${masteryInfo.totalFacts}`);
}
``` 