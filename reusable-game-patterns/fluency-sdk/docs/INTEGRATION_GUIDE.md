# Fluency SDK Integration Guide

This guide explains how to integrate the Fluency SDK into your educational game to add math fluency practice features. The SDK is designed to be platform-agnostic and can be used with any game engine or framework.

## Table of Contents

- [Overview](#overview)
- [Integration Methods](#integration-methods)
  - [Method 1: Direct Import (JavaScript/TypeScript)](#method-1-direct-import-javascripttypescript)
  - [Method 2: Module Bundling (JavaScript/TypeScript)](#method-2-module-bundling-javascripttypescript)
- [Implementing Game UI](#implementing-game-ui)
- [Advanced Usage](#advanced-usage)
  - [Custom Storage](#custom-storage)
  - [Multiple Generators](#multiple-generators)
  - [Progress Tracking](#progress-tracking)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Overview

The Fluency SDK provides a learning system for teaching multiplication facts in a structured, pedagogically sound way. It includes:

- A sequence-based learning system
- Adaptive difficulty
- Progress tracking
- Persistent state management

The core of the SDK is the `SimpleGenerator` class, which creates math questions, evaluates answers, and tracks student progress.

## Integration Methods

### Method 1: Direct Import (JavaScript/TypeScript)

For web-based games or games built with JavaScript/TypeScript, you can directly import the SDK from its location in your project:

```javascript
// Import the SDK directly from its location
import { SimpleGenerator, calculateResponseTime } from '../reusable-game-patterns/fluency-sdk';

// Create a generator
const generator = new SimpleGenerator();

// Use it in your game
const questions = generator.getNextQuestionBlock();
```

### Method 2: Module Bundling (JavaScript/TypeScript)

For more complex projects, you might want to bundle the SDK as a separate module:

1. Copy the `fluency-sdk` folder to your project
2. Create a build script to generate the SDK as a standalone module
3. Import it in your game:

```javascript
// If bundled as a UMD module
const { SimpleGenerator } = window.FluencySDK;

// Or if bundled as an ES module
import { SimpleGenerator } from 'fluency-sdk';
```

## Implementing Game UI

The SDK is UI-agnostic, so you'll need to create your own game UI. Here's a basic implementation pattern:

1. Create question UI elements (question display, input field, submit button)
2. Get questions from the generator
3. Display each question to the player
4. Collect the player's answer and submit it to the generator
5. Show feedback based on the result
6. Move to the next question

Example flow:

```javascript
function startGame() {
  // Get first block of questions
  currentQuestions = generator.getNextQuestionBlock();
  currentIndex = 0;
  displayCurrentQuestion();
}

function displayCurrentQuestion() {
  const question = currentQuestions[currentIndex];
  questionText.text = `${question.factors[0]} × ${question.factors[1]} = ?`;
  startTime = Date.now(); // For tracking response time
}

function submitAnswer(answer) {
  const question = currentQuestions[currentIndex];
  const responseTime = Date.now() - startTime;
  
  const result = generator.submitAnswer(question.id, answer, responseTime);
  
  if (result.isCorrect) {
    showSuccessFeedback();
  } else {
    showErrorFeedback(result.correctAnswer);
  }
  
  // Move to next question
  currentIndex++;
  if (currentIndex >= currentQuestions.length) {
    currentQuestions = generator.getNextQuestionBlock();
    currentIndex = 0;
  }
  
  displayCurrentQuestion();
}
```

## Advanced Usage

### Custom Storage

By default, the SimpleGenerator uses `localStorage` for web games. For platforms without localStorage, provide a custom storage adapter:

```javascript
// For a game with its own save system
const gameStorageAdapter = {
  getItem: (key) => {
    return mySaveSystem.loadData(key);
  },
  setItem: (key, value) => {
    mySaveSystem.saveData(key, value);
  },
  removeItem: (key) => {
    mySaveSystem.deleteData(key);
  }
};

const generator = new SimpleGenerator(
  { /* config */ },
  'fluencyState',
  gameStorageAdapter
);
```

### Multiple Generators

You can use multiple generators for different multiplication skills or different players:

```javascript
// One generator for each player
const player1Generator = new SimpleGenerator({}, 'player1_fluency');
const player2Generator = new SimpleGenerator({}, 'player2_fluency');

// Or different generators for different skills
const multiplicationGenerator = new SimpleGenerator({}, 'multiplication');
const divisionGenerator = new SimpleGenerator({ /* division config */ }, 'division');
```

### Progress Tracking

Track the player's progress to show achievements or unlock new content:

```javascript
function updatePlayerProgress() {
  const state = generator.getState();
  const { totalCorrect, totalIncorrect, accuracyPercentage } = calculateAccuracy(state);
  
  // Update UI to show player progress
  progressBar.value = accuracyPercentage / 100;
  statsText.text = `Correct: ${totalCorrect}, Incorrect: ${totalIncorrect}`;
  
  // Unlock new content based on progress
  if (state.currentPosition >= 5) {
    unlockNewGameMode();
  }
}
```

## Best Practices

1. **Integrate seamlessly with your game**: Make the math practice feel like a natural part of your game, not an add-on.

2. **Add game elements**: Turn the questions into challenges, add rewards, or embed them in the game narrative.

3. **Use appropriate feedback**: Make success and failure feedback match your game's style.

4. **Adapt difficulty**: Use the different modes (learning, placement, reinforcement) to match the player's skill level.

5. **Track meaningful metrics**: Use the SDK's tracking to show the player their improvement over time.

## Troubleshooting

**Q: Questions aren't advancing to the next factor in the sequence.**  
A: The player needs to get enough correct answers for each factor. Check that the `submitAnswer` method is being called with the correct parameters.

**Q: State isn't being saved between game sessions.**  
A: Make sure your storage adapter is correctly implementing all three methods: `getItem`, `setItem`, and `removeItem`.

**Q: The same questions keep appearing.**  
A: Make sure you're calling `getNextQuestionBlock()` to get new questions after completing a block.

---

For more examples and detailed API documentation, see the [README.md](./README.md) file. 