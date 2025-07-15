# Tutorial Obstacle Spawning System

## Overview

The Tutorial Obstacle Spawning System provides a clean, data-driven approach to spawning obstacles during tutorial steps that force players to practice specific movements. The system is completely decoupled from the track management system while leveraging existing infrastructure.

## Architecture

### Clean Separation of Concerns

```
TrackManager (Agnostic)
    ↓ Configuration
ITrackRunnerConfigProvider (Override System)
    ↓ Events  
TutorialGameStateAdapter (Bridge)
    ↓ Events
TutorialObstacleSpawner (Tutorial-Specific)
```

## Key Components

### 1. Configuration System

#### `ITrackRunnerConfigProvider` Extensions
- **SetObstacleDensityOverride(float?)**: Controls regular obstacle spawning
- **SetAccelerationOverride(float?)**: Controls speed changes

#### `TutorialGameStateAdapter`
- Listens to tutorial state changes
- Sets density override to `0f` during tutorial (no regular obstacles)
- Sets acceleration override to `0f` during tutorial (constant speed)
- Automatically restores defaults when tutorial ends

### 2. Data-Driven Configuration

#### `TutorialObstacleSequence` (ScriptableObject)
- **TargetStepType**: Which tutorial step this sequence applies to
- **ObstacleGroups**: Array of obstacle configurations
- **DisableCollisionDamage**: Safety setting for tutorial mode
- **ValidationSupport**: Editor validation and debugging

#### `TutorialObstacleGroup` (Serializable)
- **ObstacleType**: Type of obstacle (TrashCan, LowBarrier, HighBarrier, TrafficCone)
- **ObstaclePrefab**: AssetReference to the obstacle prefab
- **BlockedLanes**: Which lanes to spawn obstacles in (0=left, 1=center, 2=right)
- **SpawnDistance**: How far ahead of player to spawn
- **GroupCount**: Number of obstacle groups to spawn
- **GroupSeparation**: Distance between groups

### 3. Event-Driven System

#### Tutorial Events
```csharp
TutorialObstacleSpawnRequest   // Request to spawn obstacles
TutorialObstacleCleanupRequest // Request to clean up obstacles
```

#### Event Flow
1. **Tutorial Step Starts** → Auto-spawn obstacles based on step type
2. **Tutorial Step Ends** → Auto-cleanup obstacles for that step
3. **Manual Requests** → Steps can request specific obstacle patterns

### 4. Obstacle Management

#### `TutorialObstacleSpawner`
- **Event-Driven**: Responds to tutorial step events
- **Addressable Integration**: Uses existing asset loading system
- **Safety Features**: Adds marker components and damage overrides
- **Cleanup Management**: Tracks and cleans up spawned obstacles

#### Safety Components
- **TutorialObstacleMarker**: Identifies tutorial-spawned obstacles
- **TutorialObstacleSafetyOverride**: Disables damage during tutorial

## Usage

### 1. Setting Up Obstacle Sequences

```csharp
// Create via: Assets → Create → SubwaySurfers → Tutorial → Obstacle Sequence
[CreateAssetMenu(fileName = "SwipeLeftSequence", menuName = "SubwaySurfers/Tutorial/Obstacle Sequence")]
```

### 2. Configuring Tutorial Steps

```csharp
public class SwipeLeftStep : TutorialStepBase
{
    protected override void OnStepStarted()
    {
        // Auto-spawn obstacles that force left swipe
        var request = new TutorialObstacleSpawnRequest
        {
            StepType = TutorialStepType.SwipeLeft,
            ObstacleType = TutorialObstacleType.TrashCan,
            BlockedLanes = new[] { 1, 2 }, // Block center and right
            SpawnDistance = 15f,
            GroupCount = 2,
            GroupSeparation = 5f
        };
        TutorialEventBus.PublishObstacleSpawnRequest(request);
    }
}
```

### 3. Manual Testing

```csharp
// Available via context menu in TutorialObstacleSpawner
[ContextMenu("Test Spawn Swipe Left Obstacles")]
private void TestSpawnSwipeLeftObstacles() { ... }

[ContextMenu("Cleanup All Test Obstacles")]
private void TestCleanupAllObstacles() { ... }
```

## Obstacle Patterns

### Swipe Left Step
- **Blocks**: Center (1) and Right (2) lanes
- **Forces**: Player to use left lane
- **Obstacles**: TrashCans or Bins

### Swipe Right Step  
- **Blocks**: Left (0) and Center (1) lanes
- **Forces**: Player to use right lane
- **Obstacles**: TrashCans or Bins

### Jump Step
- **Blocks**: All lanes (0, 1, 2)
- **Forces**: Player to jump over obstacles
- **Obstacles**: LowBarriers or TrafficCones

### Slide Step
- **Blocks**: All lanes (0, 1, 2)  
- **Forces**: Player to slide under obstacles
- **Obstacles**: HighBarriers

## Configuration Example

```json
{
  "targetStepType": "SwipeLeft",
  "sequenceName": "Force Left Lane Usage", 
  "obstacleGroups": [
    {
      "obstacleType": "TrashCan",
      "obstaclePrefab": "Assets/Bundles/Themes/Default/Obstacles/ObstacleBin.prefab",
      "blockedLanes": [1, 2],
      "spawnDistance": 15.0,
      "groupCount": 2,
      "groupSeparation": 5.0
    }
  ],
  "disableCollisionDamage": true,
  "invincibilityDuration": 2.0
}
```

## Benefits

### 🏗️ **Clean Architecture**
- TrackManager remains tutorial-agnostic
- Configuration-driven behavior
- Event-driven communication
- Proper separation of concerns

### 🎮 **Enhanced Tutorial Experience**
- Obstacles force correct player actions
- Multiple practice opportunities (2 groups, 5m apart)
- No life loss during learning
- Clear visual feedback

### 🔧 **Developer Friendly**
- Data-driven configuration via ScriptableObjects
- Editor validation and testing tools
- Comprehensive debugging logs
- Easy to extend for new obstacle types

### 🎯 **Educational Effectiveness**
- Forces players to practice actual game mechanics
- Provides immediate consequences for incorrect actions
- Gradual skill building with multiple attempts
- Clear cause-and-effect learning

## Extension Points

### Adding New Obstacle Types
1. Add new enum value to `TutorialObstacleType`
2. Create obstacle sequence ScriptableObject
3. Configure prefab reference and spawn patterns

### Custom Tutorial Steps
1. Inherit from `TutorialStepBase`
2. Override `OnStepStarted()` to request obstacles
3. Override `OnStepCompleted()` to cleanup obstacles
4. Implement specific validation logic

### Advanced Patterns
- **Progressive Difficulty**: Spawn closer obstacles in later groups
- **Multi-Action Steps**: Combine multiple obstacle types
- **Timed Challenges**: Use spawn distance to create time pressure
- **Adaptive Spawning**: Adjust patterns based on player performance

## Troubleshooting

### Common Issues

**Obstacles not spawning:**
- Check if tutorial is active (density override = 0)
- Verify AssetReference is valid in obstacle sequence
- Ensure TutorialObstacleSpawner is in scene and configured

**Obstacles causing damage:**
- Verify `disableCollisionDamage = true` in sequence
- Check TutorialObstacleSafetyOverride component is added

**Performance issues:**
- Limit `groupCount` and total obstacles per step
- Ensure proper cleanup when steps complete
- Monitor spawned obstacle count in debug logs

### Debug Tools

- **Enable Debug Logs**: Set `enableDebugLogs = true` in TutorialObstacleSpawner
- **Context Menu Testing**: Use built-in test methods
- **Configuration Validation**: Check ScriptableObject validation
- **Runtime Inspection**: Examine spawned obstacle markers

This system provides a robust, maintainable foundation for tutorial obstacle spawning while maintaining clean architecture principles. 