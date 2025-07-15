# Intermediate Implementation

The Intermediate Implementation builds on the Simple Implementation by adding spaced repetition, better tracking, and improved analytics. It provides a more personalized learning experience while still maintaining reasonable complexity.

## Features

- Spaced repetition algorithm for optimal review scheduling
- Confidence scoring based on response time and accuracy
- Session-based learning with state persistence
- Interleaving of new and review questions
- Basic analytics on speed and accuracy
- JSON/database persistence rather than just localStorage
- Support for multiple mode-specific question selection strategies

## Implementation Details

### Data Structures

```typescript
interface FactHistory {
  lastSeen: number;
  timesCorrect: number;
  timesIncorrect: number;
  averageResponseTime: number;
  scheduledReview: number;  // Timestamp when it should be reviewed next
  confidence: number;       // 0-1 scale
}

class IntermediateGenerator implements FluencyGenerator {
  private config: FluencyGeneratorConfig;
  private state: StudentState;
  private factHistory: Record<string, FactHistory> = {};
  private questions: Record<string, Question> = {};
  private sessionData: {
    startTime: number;
    questionsAnswered: number;
    correctAnswers: number;
    sessionId: string;
  };
  
  constructor() {
    this.config = {
      sequence: [0, 1, 10, 2, 5, 3, 4, 6, 7, 8, 9, 11, 12],
      maxFactor: 12,
      questionsPerBlock: 5,
      spacingIntervals: [1, 3, 7, 14, 30], // Days before review
      randomizeWindow: 1
    };
    this.resetState();
    this.sessionData = {
      startTime: Date.now(),
      questionsAnswered: 0,
      correctAnswers: 0,
      sessionId: `session_${Date.now()}`
    };
  }
  
  // Implementation methods follow...
}
```

### Key Components

#### Spaced Repetition Algorithm

```typescript
private calculateNextReviewDate(factKey: string): number {
  const fact = this.factHistory[factKey];
  if (!fact) {
    // New fact, review tomorrow
    return Date.now() + 24 * 60 * 60 * 1000;
  }
  
  // Calculate how many times this fact has been reviewed
  const reviewCount = fact.timesCorrect + fact.timesIncorrect;
  
  // Get the appropriate spacing interval
  const intervalIndex = Math.min(
    reviewCount, 
    (this.config.spacingIntervals?.length || 1) - 1
  );
  
  const intervalDays = this.config.spacingIntervals?.[intervalIndex] || 1;
  
  // If fact has been seen a lot and confidence is high, extend interval
  let multiplier = 1.0;
  if (fact.confidence > 0.8 && reviewCount > 5) {
    multiplier = 1.5;
  } else if (fact.confidence < 0.3) {
    multiplier = 0.5; // Review sooner if low confidence
  }
  
  // Convert days to milliseconds
  const intervalMs = intervalDays * 24 * 60 * 60 * 1000 * multiplier;
  
  return Date.now() + intervalMs;
}
```

#### Confidence Scoring

```typescript
private calculateConfidence(
  fact: FactHistory, 
  isCorrect: boolean, 
  responseTimeMs: number
): number {
  // Start with existing confidence and adjust
  let confidence = fact.confidence || 0.5;
  
  // Adjust based on correctness
  if (isCorrect) {
    confidence += 0.1;
  } else {
    confidence -= 0.2;
  }
  
  // Adjust based on response time
  // Faster than 2 seconds is good, slower than 5 seconds indicates hesitation
  if (isCorrect) {
    if (responseTimeMs < 2000) {
      confidence += 0.1;
    } else if (responseTimeMs > 5000) {
      confidence -= 0.05;
    }
  }
  
  // Ensure confidence stays between 0 and 1
  return Math.max(0, Math.min(1, confidence));
}
```

#### Question Selection Strategy

```typescript
public getNextQuestionBlock(): Question[] {
  const questions: Question[] = [];
  const questionsPerBlock = this.config.questionsPerBlock || 5;
  
  if (this.state.mode === 'learning') {
    // Mix of new facts and due reviews
    
    // First, get due reviews
    const dueReviews = this.getDueReviews();
    const reviewCount = Math.min(
      Math.floor(questionsPerBlock * 0.6), // 60% reviews
      dueReviews.length
    );
    
    // Add review questions
    for (let i = 0; i < reviewCount; i++) {
      questions.push(this.generateQuestionForFact(dueReviews[i]));
    }
    
    // Then, add new questions focused on current position
    const currentFocus = this.config.sequence?.[this.state.currentPosition] || 0;
    for (let i = questions.length; i < questionsPerBlock; i++) {
      questions.push(this.generateQuestion(currentFocus));
    }
  } else if (this.state.mode === 'placement') {
    // For placement, sample across the full sequence
    const sequenceLength = this.config.sequence?.length || 12;
    
    // Ensure we test facts with a good distribution
    const step = Math.max(1, Math.floor(sequenceLength / questionsPerBlock));
    for (let i = 0; i < questionsPerBlock; i++) {
      const index = (i * step) % sequenceLength;
      const focus = this.config.sequence?.[index] || index;
      questions.push(this.generateQuestion(focus));
    }
  } else {
    // Reinforcement mode - only use due reviews
    const dueReviews = this.getDueReviews();
    
    if (dueReviews.length >= questionsPerBlock) {
      // If we have enough reviews, use those
      for (let i = 0; i < questionsPerBlock; i++) {
        questions.push(this.generateQuestionForFact(dueReviews[i]));
      }
    } else {
      // Otherwise, add some random questions from learned facts
      const learnedFactKeys = Object.keys(this.factHistory);
      
      // Add due reviews first
      dueReviews.forEach(factKey => {
        questions.push(this.generateQuestionForFact(factKey));
      });
      
      // Then add random reviews until we reach the quota
      while (questions.length < questionsPerBlock && learnedFactKeys.length > 0) {
        const randomIndex = Math.floor(Math.random() * learnedFactKeys.length);
        const factKey = learnedFactKeys[randomIndex];
        
        // Remove from list to avoid duplicates
        learnedFactKeys.splice(randomIndex, 1);
        
        questions.push(this.generateQuestionForFact(factKey));
      }
    }
  }
  
  // Set timestamp when questions are presented
  questions.forEach(q => {
    q.timeStarted = Date.now();
    this.questions[q.id] = q;
  });
  
  return questions;
}

private getDueReviews(): string[] {
  const now = Date.now();
  return Object.keys(this.factHistory)
    .filter(factKey => {
      const fact = this.factHistory[factKey];
      return fact.scheduledReview <= now;
    })
    .sort((a, b) => {
      // Prioritize by how overdue they are
      return (
        this.factHistory[a].scheduledReview - 
        this.factHistory[b].scheduledReview
      );
    });
}
```

#### Persistence

```typescript
async saveState(): Promise<void> {
  // First save to localStorage as a backup
  if (typeof localStorage !== 'undefined') {
    localStorage.setItem('fluencyState', JSON.stringify(this.state));
    localStorage.setItem('factHistory', JSON.stringify(this.factHistory));
  }
  
  // Then save to database/API
  try {
    await this.saveToDatabase();
  } catch (e) {
    console.error('Failed to save to database:', e);
  }
}

private async saveToDatabase(): Promise<void> {
  // Example implementation using fetch API
  try {
    const response = await fetch('/api/fluency/save', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        userId: 'current-user-id', // Would be provided in initialization
        state: this.state,
        factHistory: this.factHistory,
        sessionData: this.sessionData
      })
    });
    
    if (!response.ok) {
      throw new Error(`Failed to save: ${response.statusText}`);
    }
  } catch (e) {
    console.error('Error saving to database:', e);
    throw e;
  }
}

async loadState(): Promise<void> {
  // Try to load from database first
  try {
    await this.loadFromDatabase();
  } catch (e) {
    console.error('Failed to load from database:', e);
    
    // Fall back to localStorage
    this.loadFromLocalStorage();
  }
}
```

#### Analytics

```typescript
public getAnalytics(): {
  accuracy: number;
  averageSpeed: number;
  progressPercent: number;
  weakestFacts: string[];
  recommendedFocus: number[];
} {
  const totalAnswered = this.sessionData.questionsAnswered;
  const accuracy = totalAnswered > 0 
    ? this.sessionData.correctAnswers / totalAnswered 
    : 0;
  
  // Calculate average speed
  let totalTime = 0;
  let totalQuestions = 0;
  
  Object.values(this.factHistory).forEach(fact => {
    if (fact.averageResponseTime > 0) {
      totalTime += fact.averageResponseTime;
      totalQuestions++;
    }
  });
  
  const averageSpeed = totalQuestions > 0 
    ? totalTime / totalQuestions 
    : 0;
  
  // Calculate progress percentage
  const totalFacts = (this.config.sequence?.length || 12) * (this.config.maxFactor || 12);
  const learnedFacts = Object.keys(this.factHistory).length;
  const progressPercent = (learnedFacts / totalFacts) * 100;
  
  // Find weakest facts
  const weakestFacts = Object.entries(this.factHistory)
    .filter(([_, fact]) => fact.confidence < 0.5)
    .sort(([_, a], [__, b]) => a.confidence - b.confidence)
    .map(([factKey, _]) => factKey)
    .slice(0, 5);
  
  // Recommend what to focus on next
  const recommendedFocus = Array.from(
    new Set(
      weakestFacts
        .map(factKey => {
          const [a, b] = factKey.split('x').map(Number);
          return [a, b];
        })
        .flat()
    )
  )
  .filter(n => n !== 0) // Filter out zeros as they're trivial
  .slice(0, 3);
  
  return {
    accuracy,
    averageSpeed,
    progressPercent,
    weakestFacts,
    recommendedFocus
  };
}
```

## Implementation Steps

### Week 1: Foundation (Days 1-3)

1. **Day 1**: Set up enhanced data structures
   - Define FactHistory interface
   - Extend StudentState with additional tracking
   - Implement session data tracking

2. **Day 2**: Implement spaced repetition algorithm
   - Create confidence scoring function
   - Build next review date calculator
   - Design fact retrieval mechanism

3. **Day 3**: Question selection strategies
   - Implement due review finder
   - Create question generation for specific facts
   - Design interleaving strategy for new/review questions

### Week 1: Persistence and Analytics (Days 4-5)

4. **Day 4**: Persistence layer
   - Implement database schema for fact history
   - Create API endpoints for saving/loading
   - Build backup mechanism with localStorage

5. **Day 5**: Analytics module
   - Implement accuracy and speed calculations
   - Create weak fact identification
   - Build progress tracking

### Week 2: Testing and Refinement

6. **Day 6-7**: Integration and testing
   - Unit tests for spaced repetition
   - Integration tests for persistence
   - User flow testing

7. **Day 8-10**: Refinement and documentation
   - Tune spaced repetition parameters
   - Add logging and error handling
   - Complete documentation

## Limitations

While more advanced than the Simple Implementation, the Intermediate still has some limitations:

- No machine learning for optimal fact scheduling
- Limited error pattern detection
- Basic analytics without predictive modeling
- No adaptivity based on learning style
- Moderate complexity in deployment (requires a database)

These limitations will be addressed in the Advanced Implementation. 