# Tutorial System Migration Guide

## Overview

This document outlines the migration from the legacy tutorial system to the new event-driven tutorial architecture. The legacy code has been deprecated but remains functional during the transition period.

## Phase 1: Legacy Code Deprecation ✅

The following legacy tutorial components have been marked as `[System.Obsolete]` and will generate compiler warnings:

### GameState.cs - Deprecated Components

#### Fields
- `tutorialValidatedObstacles` - Use `TutorialUIController` instead
- `sideSlideTuto`, `upSlideTuto`, `downSlideTuto`, `finishTuto` - Use `TutorialUIController` instead
- `m_TutorialClearedObstacle`, `m_CountObstacles`, `m_DisplayTutorial` - Use `TutorialManager` instead
- `m_CurrentSegmentObstacleIndex`, `m_NextValidSegment`, `k_ObstacleToClear` - Use `TutorialManager` instead

#### Methods
- `OnCurrentSegmentChanged()` - Logic moved to `TutorialManager`
- `OnSegmentCreated()` - Logic moved to `TutorialManager`
- `TutorialCheckObstacleClear()` - Use `TutorialManager` obstacle tracking
- `DisplayTutorial()` - Use `TutorialUIController`
- `FinishTutorial()` - Use `TutorialManager.CompleteTutorial()`

### CharacterInputController.cs - Deprecated Components

#### Fields
- `currentTutorialLevel` - Use `TutorialManager.CurrentStepType` instead
- `tutorialWaitingForValidation` - Use `TutorialManager.IsPaused` instead

#### Methods
- `TutorialMoveCheck()` - Use `MovementValidator` instead

### CharacterCollider.cs - Deprecated Components

#### Fields
- `tutorialHitObstacle` - Use tutorial event system instead
- `m_TutorialHitObstacle` - Use tutorial event system instead

## Phase 2: Complete Migration Tasks

To fully migrate to the new tutorial system, complete these tasks:

### 1. Remove Legacy UI References
```csharp
// Remove these from GameState inspector
public Text tutorialValidatedObstacles;      // DELETE
public GameObject sideSlideTuto;             // DELETE
public GameObject upSlideTuto;               // DELETE
public GameObject downSlideTuto;             // DELETE
public GameObject finishTuto;                // DELETE
```

### 2. Update UI References
Replace legacy tutorial UI with new `TutorialUIController`:
```csharp
// OLD - Direct UI manipulation
sideSlideTuto.SetActive(true);

// NEW - Event-driven UI
TutorialEventBus.PublishUIEvent(new TutorialUIEvent
{
    EventType = TutorialUIEvent.UIEventType.ShowInstructions,
    Message = "Swipe left or right to change lanes"
});
```

### 3. Replace Input Validation
```csharp
// OLD - Direct tutorial level check
if (!TutorialMoveCheck(0)) return;

// NEW - Event-driven validation
// Input validation is now handled automatically by MovementValidator
// Just publish the input event and let the system handle validation
```

### 4. Update Obstacle Tracking
```csharp
// OLD - Manual obstacle counting
m_TutorialHitObstacle = true;

// NEW - Event-driven obstacle tracking
PublishTutorialObstacleEvent(TutorialAction.ObstacleHit);
```

### 5. Remove Legacy Methods Entirely

After migration, delete these methods completely:
- `GameState.TutorialCheckObstacleClear()`
- `GameState.DisplayTutorial()`
- `GameState.OnCurrentSegmentChanged()` (tutorial-specific parts)
- `GameState.OnSegmentCreated()` (tutorial-specific parts)
- `CharacterInputController.TutorialMoveCheck()`

### 6. Remove Legacy Fields

After migration, delete these fields completely:
- All deprecated fields listed above
- Legacy UI references in GameState
- Tutorial validation fields in CharacterInputController
- Tutorial collision fields in CharacterCollider

## Phase 3: Adapter Cleanup

Once legacy code is fully removed:

### 1. Update TutorialGameStateAdapter
```csharp
// Remove the ShouldGameStateHandleTutorial() method
// Simplify the adapter to only handle new system

public void EnableNewTutorialSystem(bool enable)
{
    // This becomes unnecessary - always use new system
}
```

### 2. Simplify Integration
Remove backward compatibility code from adapters once legacy system is no longer needed.

## Testing Migration

### 1. Compiler Warnings
Ensure no `[System.Obsolete]` warnings remain in your code.

### 2. Runtime Testing
- Start tutorial via `TutorialManager.StartTutorial()`
- Verify all steps work correctly
- Confirm UI updates through events
- Test input validation through new system

### 3. Performance Verification
- Remove `FindObjectOfType` calls where possible
- Cache adapter references
- Verify event subscriptions are properly cleaned up

## Configuration Migration

### 1. Create TutorialConfig Asset
```csharp
// In Project window: Create > SubwaySurfers > Tutorial > Tutorial Config
// Configure all tutorial steps and settings
```

### 2. Update Scene Setup
```csharp
// Add TutorialManager to scene
// Add TutorialUIController to UI canvas
// Add TutorialEventBus (auto-created)
// Assign TutorialConfig to TutorialManager
```

## Benefits After Migration

### Code Quality
- ✅ No more scattered tutorial logic
- ✅ Clean separation of concerns
- ✅ Event-driven architecture
- ✅ Easy to test and debug

### Maintainability
- ✅ Data-driven configuration
- ✅ Modular components
- ✅ Clear interfaces
- ✅ Comprehensive documentation

### Extensibility
- ✅ Easy to add new tutorial steps
- ✅ Pluggable validation system
- ✅ Configurable UI components
- ✅ Analytics integration ready

## Common Migration Issues

### 1. UI References Not Found
**Problem**: Legacy UI components referenced in inspector
**Solution**: Replace with TutorialUIController setup

### 2. Tutorial Not Starting
**Problem**: Legacy GameState.IsTutorial logic
**Solution**: Use TutorialManager.StartTutorial() instead

### 3. Input Validation Broken
**Problem**: TutorialMoveCheck() method removed
**Solution**: Input validation now automatic through MovementValidator

### 4. Events Not Firing
**Problem**: TutorialEventBus not initialized
**Solution**: Ensure TutorialEventBus exists in scene (auto-created)

## Support

For questions about migration:
1. Check this migration guide
2. Review the main README.md
3. Look at example usage in TutorialManager
4. Check deprecated method warnings for replacement guidance

## Timeline

- **Phase 1**: Legacy code deprecated ✅ (Current)
- **Phase 2**: Complete migration (Next sprint)
- **Phase 3**: Remove legacy code entirely (Following sprint)
- **Phase 4**: Performance optimization (Final polish)

Legacy code will be fully removed in Phase 3, so please plan migration accordingly. 