# Fluency SDK

The Fluency SDK provides a framework for implementing math fluency practice in educational games. It's designed to be platform-agnostic and can be integrated into any JavaScript or TypeScript project.

## Key Features

- **Pedagogical Sequence:** Built-in sequence for teaching multiplication facts in a research-based order
- **Adaptive Learning:** Automatically adjusts to student progress
- **Learning Modes:** Supports learning, placement, and reinforcement modes
- **Progress Tracking:** Tracks correct/incorrect answers and response times
- **Persistent State:** Saves learning progress for continued practice

## Installation

```bash
# Coming soon
npm install @ai-edu-chatbot/fluency-sdk
```

## Basic Usage

```javascript
import { SimpleGenerator } from '@ai-edu-chatbot/fluency-sdk';

// Create a generator with default settings
const generator = new SimpleGenerator();

// Get a block of questions
const questions = generator.getNextQuestionBlock();

// Display a question to the user
const question = questions[0];
console.log(`${question.factors[0]} × ${question.factors[1]} = ?`);

// When the user answers
const userAnswer = 42; // This would come from user input
const responseTime = 1500; // Response time in milliseconds

// Submit the answer
const result = generator.submitAnswer(question.id, userAnswer, responseTime);

// Show feedback
if (result.isCorrect) {
  console.log("Correct!");
} else {
  console.log(`Incorrect. The answer is ${result.correctAnswer}.`);
}
```

## Configuration Options

The SimpleGenerator can be configured with various options:

```javascript
const generator = new SimpleGenerator({
  // Custom sequence of multiplication facts to learn
  sequence: [0, 1, 10, 2, 5, 3, 4, 6, 7, 8, 9, 11, 12],
  // Maximum factor (up to 12 times tables)
  maxFactor: 12,
  // Number of questions per practice block
  questionsPerBlock: 5
}, 
// Storage key - useful if you want multiple generators
'myGame_fluencyState',
// Optional custom storage adapter (see Storage section below)
customStorageAdapter
);
```

## Integrating with Games

### Web/JavaScript Games

For web-based games, you can directly use the SimpleGenerator class. See the [vanillaJs.js](./examples/vanillaJs.js) example for a complete implementation.

## Custom Storage

By default, the SimpleGenerator uses `localStorage` for persistence. If you're using a platform without localStorage or need a different storage solution, you can provide a custom storage adapter:

```javascript
import { SimpleGenerator, createStorageAdapter } from '@ai-edu-chatbot/fluency-sdk';

// Create a custom storage adapter
const customStorage = createStorageAdapter(
  // getItem function
  (key) => {
    // Your custom implementation
    return myCustomStorage.get(key);
  },
  // setItem function
  (key, value) => {
    // Your custom implementation
    myCustomStorage.set(key, value);
  },
  // removeItem function
  (key) => {
    // Your custom implementation
    myCustomStorage.remove(key);
  }
);

// Use the custom storage adapter
const generator = new SimpleGenerator(
  { /* config options */ },
  'myStorageKey',
  customStorage
);
```

## API Reference

### SimpleGenerator

The main class for generating and tracking fluency practice.

#### Methods

- `getNextQuestionBlock()`: Get the next set of questions
- `submitAnswer(questionId, answer, responseTimeMs)`: Submit an answer and get feedback
- `getState()`: Get the current student state
- `setState(state)`: Set the student state
- `reset()`: Reset progress to initial state
- `setMode(mode)`: Set the learning mode ('learning', 'placement', or 'reinforcement')

### Utilities

The SDK provides several utility functions:

- `calculateResponseTime(startTime)`: Calculate response time from a start time
- `formatQuestion(question)`: Get a formatted display string for a question
- `calculateAccuracy(state)`: Calculate accuracy statistics from a student state
- `getCurrentFocus(state, sequence)`: Get the current learning focus
- `createStorageAdapter(getItemFn, setItemFn, removeItemFn)`: Create a custom storage adapter

## Asynchronous Interface

All methods in the `FluencyGenerator` interface are asynchronous and return Promises. This design decision allows for:

1. **API-based implementations**: The SDK can be implemented using remote APIs without changing the interface.
2. **Database storage**: State can be persisted to databases that require async operations.
3. **Future extensibility**: Supports more complex implementations that may require asynchronous operations.

Example usage:

```typescript
// Create a generator
const generator = FluencyGeneratorFactory.create('simple');

// Get questions
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
}
```

## Available Implementations

The SDK includes the following implementations:

1. **SimpleGenerator**: A basic in-memory implementation with localStorage persistence.
2. **ApiGenerator**: An implementation that uses a remote API for all operations.

You can create instances of these using the factory:

```typescript
// Create a simple generator
const simpleGenerator = FluencyGeneratorFactory.create('simple', {
  config: {
    sequence: [0, 1, 10, 2, 5, 3, 4, 6, 7, 8, 9, 11, 12],
    maxFactor: 12,
    questionsPerBlock: 5
  },
  storageKey: 'myApp_fluencyState'
});

// Create an API-based generator
const apiGenerator = FluencyGeneratorFactory.create('api', {
  apiOptions: {
    apiBaseUrl: 'https://api.example.com/fluency',
    apiKey: 'YOUR_API_KEY'
  }
});
```

## License

MIT 