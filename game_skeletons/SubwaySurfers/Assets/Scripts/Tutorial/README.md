# Tutorial System Refactoring

## Overview

This document describes the new tutorial system architecture that replaces the scattered tutorial logic previously spread across multiple components. The new system follows proper separation of concerns and provides a clean, maintainable, and extensible tutorial framework.

## Architecture

### Core Components

#### 1. Events System (`Assets/Scripts/Tutorial/Events/`)
- **TutorialEvents.cs**: Defines all tutorial-related events and data structures
- **TutorialEventBus.cs**: Centralized event system for decoupled communication

#### 2. Core System (`Assets/Scripts/Tutorial/Core/`)
- **ITutorialManager.cs**: Interface for the main tutorial orchestrator
- **TutorialManager.cs**: Concrete implementation managing tutorial flow
- **ITutorialStep.cs**: Interface defining tutorial step contracts
- **TutorialStepBase.cs**: Base class for all tutorial steps
- **TutorialConfig.cs**: ScriptableObject for data-driven configuration

#### 3. Tutorial Steps (`Assets/Scripts/Tutorial/Steps/`)
- **LeftRightSwipeStep.cs**: Teaches lane changing (left/right movement)
- **JumpStep.cs**: Teaches jumping over obstacles
- **SlideStep.cs**: Teaches sliding under obstacles
- **CompletionStep.cs**: Handles tutorial completion

#### 4. Validation System (`Assets/Scripts/Tutorial/Validation/`)
- **ITutorialValidator.cs**: Interfaces for validation components
- **MovementValidator.cs**: Validates player movement actions

#### 5. UI System (`Assets/Scripts/Tutorial/UI/`)
- **ITutorialUI.cs**: Interfaces for tutorial UI components
- **TutorialUIController.cs**: Manages tutorial UI elements and interactions

#### 6. Integration Layer (`Assets/Scripts/Tutorial/Integration/`)
- **TutorialInputAdapter.cs**: Bridges existing input system with tutorial events
- **TutorialGameStateAdapter.cs**: Bridges existing GameState with new tutorial system

## Key Features

### 1. Event-Driven Architecture
- Loose coupling between components through events
- Easy to extend and modify without affecting other systems
- Clean separation of concerns

### 2. Data-Driven Configuration
- Tutorial behavior configured via ScriptableObjects
- Easy to modify tutorial flow without code changes
- Designer-friendly configuration

### 3. Backward Compatibility
- Gradual migration from old system
- Toggle between old and new systems
- Minimal disruption to existing functionality

### 4. Extensible Design
- Easy to add new tutorial steps
- Pluggable validation system
- Modular UI components

## Usage

### Creating a Tutorial Config

1. Right-click in Project window
2. Navigate to Create > SubwaySurfers > Tutorial > Tutorial Config
3. Configure tutorial steps, settings, and audio clips

### Starting the Tutorial

```csharp
var tutorialManager = FindObjectOfType<TutorialManager>();
tutorialManager.StartTutorial();
```

### Handling Tutorial Events

```csharp
// Subscribe to tutorial events
TutorialEventBus.OnStepCompleted += HandleStepCompleted;
TutorialEventBus.OnTutorialCompleted += HandleTutorialCompleted;

private void HandleStepCompleted(TutorialStepCompletedEvent stepEvent)
{
    Debug.Log($"Step {stepEvent.StepName} completed: {stepEvent.Success}");
}
```

### Creating Custom Tutorial Steps

```csharp
public class CustomTutorialStep : TutorialStepBase
{
    public CustomTutorialStep(TutorialStepData stepData) : base(stepData) { }

    protected override bool ValidateAction(TutorialActionPerformedEvent actionEvent)
    {
        // Implement your validation logic
        return actionEvent.Action == TutorialAction.SwipeUp;
    }

    protected override void OnStepStarted()
    {
        Debug.Log("Custom tutorial step started");
    }
}
```

## Migration Guide

### Phase 1: Core Infrastructure ✅
- Implemented event system and core interfaces
- Created tutorial manager and step base classes
- Added validation and UI systems

### Phase 2: Integration ✅
- Modified existing systems to publish tutorial events
- Added adapters for backward compatibility
- Integrated with GameState and CharacterInputController

### Phase 3: Legacy Code Deprecation ✅
- Marked all legacy tutorial code as `[System.Obsolete]`
- Added compiler warnings for deprecated methods and fields
- Maintained backward compatibility during transition
- Created comprehensive migration documentation

### Phase 4: Complete Migration (Next)
- Remove legacy tutorial logic from existing classes entirely
- Full UI integration with new system
- Performance optimization and testing
- Remove adapters and backward compatibility code

## Configuration

### Tutorial Steps
Each step can be configured with:
- Step type and name
- Instructions text
- Required successful actions
- Timeout duration
- Skip permissions
- Audio clips and images

### General Settings
- Allow skipping tutorial
- Allow restarting tutorial
- Step transition delays
- UI settings (progress bar, step counter)

## Events Reference

### Tutorial Events
- `OnStepStarted`: Fired when a tutorial step begins
- `OnStepCompleted`: Fired when a tutorial step is completed
- `OnActionPerformed`: Fired when player performs an action
- `OnProgressChanged`: Fired when tutorial progress updates
- `OnStateChanged`: Fired when tutorial state changes
- `OnTutorialCompleted`: Fired when tutorial is finished
- `OnUIEvent`: Fired for UI-related events

### Action Types
- `SwipeLeft`: Player swiped left
- `SwipeRight`: Player swiped right
- `SwipeUp`: Player swiped up (jump)
- `SwipeDown`: Player swiped down (slide)
- `ObstacleHit`: Player hit an obstacle
- `ObstacleAvoided`: Player successfully avoided an obstacle

## Benefits

### For Developers
- Clean, maintainable code
- Easy to debug and test
- Extensible architecture
- Proper separation of concerns

### For Designers
- Data-driven configuration
- No code changes needed for adjustments
- Visual feedback and progress tracking
- Easy to add new tutorial content

### For Players
- Consistent tutorial experience
- Clear progress indication
- Better feedback and guidance
- Smooth tutorial flow

## Future Enhancements

1. **Analytics Integration**: Track tutorial completion rates and drop-off points
2. **Localization Support**: Multi-language tutorial instructions
3. **Advanced Validation**: More sophisticated player action validation
4. **Tutorial Replay**: Allow players to replay specific tutorial steps
5. **A/B Testing**: Support for different tutorial variants
6. **Voice Guidance**: Audio instructions for accessibility
7. **Gesture Recognition**: More advanced input detection and validation

## Troubleshooting

### Common Issues

1. **Tutorial Not Starting**
   - Ensure TutorialManager is in the scene
   - Check TutorialConfig is assigned
   - Verify adapters are initialized

2. **Events Not Firing**
   - Check TutorialEventBus is present
   - Verify event subscriptions
   - Ensure adapters are properly connected

3. **UI Not Updating**
   - Check TutorialUIController setup
   - Verify UI references are assigned
   - Ensure event bus connections

### Debug Commands

```csharp
// Test tutorial start
tutorialManager.StartTutorial();

// Skip current step
tutorialManager.SkipCurrentStep();

// Check tutorial state
Debug.Log($"Tutorial Active: {tutorialManager.IsActive}");
Debug.Log($"Current Step: {tutorialManager.CurrentStepType}");
``` 