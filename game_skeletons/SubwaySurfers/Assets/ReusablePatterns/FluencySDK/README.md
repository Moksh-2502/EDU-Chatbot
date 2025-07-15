# FluencySDK Question System

A question system built using the FluencySDK for generating timed math questions with proper separation of concerns.

## Features

- Generates math questions every 5 seconds using the FluencySDK
- Clear separation between logic and UI layers using interfaces and events
- Visual feedback for correct and incorrect answers
- Tracks student progress through the FluencySDK

## Setup Instructions

1. Add the `FluencySDK.prefab` from `Prefabs/UI` to your scene
2. Configure the `FluencyConfig` component
3. Add the `QuestionGenerator` and ensure it's linked to the config
4. Add the `QuestionUI` component to your UI canvas

## Architecture

The system uses a proper layered architecture with clean separation of concerns:

1. **Logic Layer** - Question generation and processing (no UI dependencies)
2. **UI Layer** - Visualization and user interaction
3. **Communication** - Interfaces and events for decoupled communication

## Core Components

### IQuestionProvider (Interface)

Defines the contract for question generation:
- Events for question lifecycle (started, ended)
- Methods for submission and state management
- Completely independent of UI concerns

### QuestionGenerator

Implements `IQuestionProvider`:
- Handles the timing of question generation
- Processes question answers
- Fires events that UI can subscribe to
- No references to UI components

### IFluencyConfigProvider (Interface)

Defines the contract for configuration:
- Creates configuration for the FluencySDK
- Separates configuration from implementation

### FluencyConfig

Implements `IFluencyConfigProvider`:
- Configures the factor sequence and other parameters
- Initializes the question generator with config

### QuestionUI

- Listens to events from any `IQuestionProvider` implementation
- Handles visualization of questions and answers
- Manages UI states and feedback

### AnswerButton

- Self-contained button component
- Handles its own state (idle, correct, wrong)
- Fires events when clicked

## Events

The system uses events for decoupled communication:
- `OnQuestionStarted`: Fired when a new question is generated
- `OnQuestionEnded`: Fired when a question is answered (with correctness result)

## Integration Tips

- To implement a custom question provider:
```csharp
public class CustomQuestionProvider : MonoBehaviour, IQuestionProvider
{
    // Implement the interface methods
}
```

- To create a custom UI that listens to questions:
```csharp
// Find any IQuestionProvider implementation
IQuestionProvider provider = FindObjectOfType<QuestionGenerator>();
provider.OnQuestionStarted += HandleNewQuestion;
```

# Fluency SDK Integration for Subway Surfers

This package provides integration between the Fluency SDK (educational question system) and the Subway Surfers game mechanics.

## Three-Loop Learning System

The system now implements a three-loop learning approach:

### Assessment Loop (10s timer)
- **Purpose**: Initial evaluation of student knowledge
- **Handler**: AnswerObjectsQuestionHandler (scatter questions)
- **Progression**: 2 correct in a row → Fluency, 2 incorrect in a row → Mastery
- **Fact Set Progression**: 50% correct streak + empty mastery pool → move whole fact set to fluency

### Mastery Loop (untimed)
- **Purpose**: Focused learning for struggling concepts
- **Handler**: PowerupQuestionHandler (blocking popups)
- **Progression**: 1 correct answer → Fluency

### Fluency Loop (7s/3s timer)
- **Purpose**: Speed and automaticity development
- **Handler**: FinishLineQuestionHandler (finish line representation)
- **Timer Progression**: 3 correct at 7s → move to 3s, 3 incorrect at 3s → back to 7s
- **Regression**: 3 incorrect at 7s → back to Assessment
- **Completion**: 50% correct at 3s + empty mastery pool → next fact set

## Components

### Question Time Monitoring

- `IQuestionTimeMonitor` - Interface for monitoring when questions can be shown
- `QuestionTimeMonitor` - Implementation that periodically checks when questions can be shown and manages question lifecycle

### Question Presentation Modes

- `IQuestionPresenter` - Interface for classes that present questions through gameplay mechanics
- `ShackleQuestionPresenter` - Presents questions by applying a shackle debuff
- `PowerupQuestionPresenter` - Presents questions through collectible powerups

### Supporting Systems

- `PowerupSpawnerController` - Controls spawning of standard and educational powerups
- `EducationalPowerupTag` - Component that marks powerups as educational and triggers questions
- `ObstacleDistanceTracker` - Tracks distances to upcoming obstacles for timing questions

## How to Use

1. Add `QuestionTimeMonitor` component to the scene to monitor question opportunities
2. Add `ShackleQuestionPresenter` and `PowerupQuestionPresenter` components to present questions
3. Configure the `EducationHandler` component to use the new question system
4. **Configure Question Handlers** in the Inspector:
   - **AnswerObjectsQuestionHandler**: Set `AcceptedLearningMode` to `Assessment`
   - **PowerupQuestionHandler**: Set `AcceptedLearningMode` to `Mastery`
   - **FinishLineQuestionHandler**: Set `AcceptedLearningMode` to `Fluency`
5. Adjust presentation weights in the `EducationHandler` to control how often each mode is used

## Testing Procedures

1. Test ShackleQuestionPresenter by running the game and observing when shackles are applied during gameplay
2. Test PowerupQuestionPresenter by checking if special powerups spawn when questions are ready
3. Verify that different presentation modes alternate based on configured weights
4. Ensure questions are displayed correctly and game state is properly managed during questions
5. Check that educational powerups are cleared when questions are completed

## Architecture Overview

The system uses a modular design to separate concerns:

1. The `QuestionTimeMonitor` is responsible for determining when questions can be shown
2. The `EducationHandler` decides which presentation mode to use
3. Each `IQuestionPresenter` implementation handles showing questions via a specific gameplay mechanic
4. The original FluencySDK handles the actual question UI and scoring

This allows for easy extension with new presentation modes in the future.