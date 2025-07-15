# Simple Implementation

The Simple Implementation provides basic multiplication fluency practice with minimal complexity. It's designed for quick implementation and testing of the core concepts.

## Features

- Hardcoded pedagogical sequence for multiplication facts (0, 1, 10, 2, 5, ...)
- Linear progression through the sequence
- Basic tracking of correct/incorrect answers
- Simple storage using localStorage
- Minimal UI requirements

## Implementation Details

### Data Structures

```typescript
class SimpleGenerator implements FluencyGenerator {
  private config: FluencyGeneratorConfig;
  private state: StudentState;
  private defaultSequence = [0, 1, 10, 2, 5, 3, 4, 6, 7, 8, 9, 11, 12];
  private questions: Record<string, Question> = {};
  
  constructor() {
    this.config = {
      sequence: this.defaultSequence,
      maxFactor: 12,
      questionsPerBlock: 5
    };
    this.resetState();
  }
  
  private resetState() {
    this.state = {
      currentPosition: 0,
      learnedFacts: {},
      mode: 'learning'
    };
  }
  
  // Other implementation methods follow...
}
```

### Key Methods

#### Question Generation

```typescript
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
```

#### Getting Questions

```typescript
public getNextQuestionBlock(): Question[] {
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
```

#### Submitting Answers

```typescript
public submitAnswer(
  questionId: string, 
  answer: number, 
  responseTimeMs: number
): { isCorrect: boolean; correctAnswer?: number } {
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
    const factKeys = [];
    for (let i = 0; i <= (this.config.maxFactor || 12); i++) {
      factKeys.push(`${currentFocus}x${i}`);
      factKeys.push(`${i}x${currentFocus}`);
    }
    
    // Count correct answers for current focus
    let correctCount = 0;
    let requiredCount = 5; // Arbitrary threshold
    
    factKeys.forEach(key => {
      if (this.state.learnedFacts[key]?.timesCorrect > 0) {
        correctCount++;
      }
    });
    
    if (correctCount >= requiredCount) {
      this.state.currentPosition++;
    }
  }
  
  this.saveState();
  
  return { 
    isCorrect: question.isCorrect, 
    correctAnswer: question.answer 
  };
}
```

#### Persistence

```typescript
private saveState() {
  if (typeof localStorage !== 'undefined') {
    localStorage.setItem('fluencyState', JSON.stringify(this.state));
  }
}

private loadState() {
  if (typeof localStorage !== 'undefined') {
    const savedState = localStorage.getItem('fluencyState');
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
  } else {
    this.resetState();
  }
}
```

## Implementation Steps

1. **Day 1**: Set up project structure and define core interfaces
   - Create the FluencyGenerator interface
   - Define Question and StudentState types
   - Set up basic test environment

2. **Day 2**: Implement core logic
   - Implement question generation
   - Create answer validation
   - Build basic state management

3. **Day 3**: Add storage and complete implementation
   - Add localStorage persistence
   - Implement remaining interface methods
   - Simple progress tracking

4. **Day 4**: Testing and integration
   - Unit tests for core functions
   - Basic UI integration
   - Manual testing of fact progression

5. **Day 5**: Polish and documentation
   - Fix bugs from testing
   - Add documentation
   - Prepare for next phase

## Limitations

The Simple Implementation has several limitations:

- No true spaced repetition
- Minimal adaptive learning
- Limited analytics
- Basic persistence (localStorage only)
- Simple progression criteria
- No support for error pattern detection

These limitations will be addressed in the Intermediate and Advanced implementations. 