# Tutorial Question System

## Overview

The Tutorial Question System allows you to integrate educational questions directly into the tutorial flow using the existing QuestionPopUp with mastery mode functionality, without affecting student progression.

## Features

- **Mastery Mode**: Questions use mastery learning mode with retry functionality
- **No Progression Impact**: Tutorial questions don't affect student learning progress
- **Seamless Integration**: Uses existing QuestionPopUp UI and infrastructure
- **Isolated Processing**: Questions are processed separately from real educational content

## Setup

### 1. Add TutorialQuestionHandler to Scene

1. Drag the `TutorialQuestionHandlerPrefab.prefab` from `Assets/Scripts/Tutorial/Steps/` into your scene
2. The handler will automatically register itself with the EducationHandler system

### 2. Configure Tutorial Step

Add a QuestionTutorial step to your TutorialConfig:

```csharp
new TutorialStepData
{
    stepType = TutorialStepType.QuestionTutorial,
    stepName = "Practice Question",
    instructions = "Try answering this practice question!",
    requiredSuccessfulActions = 1,
    allowSkipping = false
}
```

### 3. Usage in Tutorial Flow

Simply include `TutorialStepType.QuestionTutorial` in your tutorial step sequence. When this step is reached:

1. A mock mastery question is created (currently "What is 2 × 3?")
2. The QuestionPopUp appears with full mastery mode functionality
3. Student can answer incorrectly and retry (mastery mode behavior)
4. Once answered correctly, the tutorial step completes
5. No data is saved to student progress

## Technical Details

### QuestionTutorialStep

- Creates mock questions with `FactSetId = "tutorial"`
- Uses `LearningMode.Grounding` for retry functionality
- Questions are untimed (grounding mode)
- Completes when question is answered (any answer type)

### TutorialQuestionHandler

- Only handles questions with `FactSetId = "tutorial"`
- Accepts only `LearningMode.Grounding` questions
- Pauses game during question presentation
- **Does not process results** - no progression impact
- No analytics tracking for tutorial questions

### Mock Question Structure

```csharp
Question: "What is 2 × 3?"
Choices: [6 (correct), 5, 7, 8]
LearningMode: Mastery
TimeToAnswer: null (untimed)
```

## Customizing Questions

To customize the tutorial question, modify the `CreateMockMasteryQuestion()` method in `QuestionTutorialStep.cs`:

```csharp
private IQuestion CreateMockMasteryQuestion()
{
    var mockFact = new Fact
    {
        Id = "tutorial_mock_fact",
        FactSetId = "tutorial", // MUST be "tutorial"
        FactorA = 3, // Change as needed
        FactorB = 4, // Change as needed
        Text = "What is 3 × 4?" // Update question text
    };

    return new Question(mockFact)
    {
        Id = "tutorial_question_" + System.Guid.NewGuid().ToString(),
        Text = "What is 3 × 4?", // Update question text
        Choices = new[]
        {
            new QuestionChoice<int> { Value = 12, IsCorrect = true }, // Correct answer
            new QuestionChoice<int> { Value = 10, IsCorrect = false },
            new QuestionChoice<int> { Value = 14, IsCorrect = false },
            new QuestionChoice<int> { Value = 16, IsCorrect = false }
        },
        LearningMode = LearningMode.Grounding, // Keep as Grounding
        LearningStage = LearningStage.Grounding, // Keep as Grounding
        TimeToAnswer = null // Keep untimed
    };
}
```

## Important Notes

- **FactSetId must be "tutorial"** for proper isolation
- Questions use mastery mode for retry functionality on wrong answers
- Tutorial questions do NOT affect student progress or analytics
- The TutorialQuestionHandler must be present in the scene for questions to work
- Only one tutorial question per step is supported currently

## Debugging

Enable console logging to see tutorial question flow:
- `[QuestionTutorialStep]` logs for step lifecycle
- `[TutorialQuestionHandler]` logs for question handling
- Questions are identified by unique GUIDs in logs

## Example Tutorial Configuration

```csharp
public TutorialStepData[] steps = new TutorialStepData[]
{
    // Regular tutorial steps
    new TutorialStepData { stepType = TutorialStepType.SwipeLeft, ... },
    new TutorialStepData { stepType = TutorialStepType.SwipeRight, ... },
    
    // Question tutorial step
    new TutorialStepData 
    { 
        stepType = TutorialStepType.QuestionTutorial,
        stepName = "Practice Question",
        instructions = "Let's practice with a math question!",
        requiredSuccessfulActions = 1
    },
    
    // Continue with more tutorial steps
    new TutorialStepData { stepType = TutorialStepType.Jump, ... },
    new TutorialStepData { stepType = TutorialStepType.Completion, ... }
};
``` 