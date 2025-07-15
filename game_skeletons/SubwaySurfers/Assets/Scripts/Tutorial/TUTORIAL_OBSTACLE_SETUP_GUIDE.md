# Tutorial Obstacle Setup Guide

## 🎯 Quick Start

You now have a complete tutorial obstacle system with JSON-based configuration and Unity Editor tools. Here's how to get started:

### 1. **JSON Sequence Files Created** ✅

The following tutorial sequence JSON files have been created in `Assets/Scripts/Tutorial/Data/TutorialSequences/`:

#### **Basic Movement Sequences:**
- **`SwipeLeftSequence.json`** - Forces player to swipe left (blocks center & right lanes)
- **`SwipeRightSequence.json`** - Forces player to swipe right (blocks left & center lanes)  
- **`JumpSequence.json`** - Forces player to jump (traffic cones across all lanes)
- **`SlideSequence.json`** - Forces player to slide (high barriers across all lanes)

#### **Advanced Training Sequences:**
- **`ProgressiveSwipeLeftSequence.json`** - Progressive left movement training
- **`MixedJumpSequence.json`** - Mixed jump training with different obstacle types

### 2. **Unity Editor Import Tool** ✅

A complete Unity Editor utility has been created at `Assets/Scripts/Tutorial/Editor/TutorialSequenceImporter.cs`

#### **How to Use:**
1. **Open the Importer**: `Menu Bar → Trash Dash → Tutorial → Sequence Importer`
2. **Import All**: Click "Import All JSON Files" to convert all JSON files to ScriptableObjects
3. **Individual Import**: Select a JSON file in Project window → Right-click → `Create/Trash Dash/Tutorial/Import JSON to Sequence`

## 📋 **Available Obstacle Sequences**

### **Swipe Left Training**
```json
{
  "targetStepType": "SwipeLeft",
  "sequenceName": "Force Left Lane Movement",
  "obstacleGroups": [
    {
      "obstacleType": "TrashCan",
      "obstaclePrefabAddress": "ObstacleBin",
      "blockedLanes": [1, 2],        // Block center & right
      "spawnDistance": 15.0,
      "groupCount": 2,
      "groupSeparation": 6.0
    }
  ]
}
```

### **Swipe Right Training**
```json
{
  "targetStepType": "SwipeRight", 
  "sequenceName": "Force Right Lane Movement",
  "obstacleGroups": [
    {
      "obstacleType": "TrashCan",
      "obstaclePrefabAddress": "ObstacleBin", 
      "blockedLanes": [0, 1],        // Block left & center
      "spawnDistance": 15.0,
      "groupCount": 2,
      "groupSeparation": 6.0
    }
  ]
}
```

### **Jump Training**
```json
{
  "targetStepType": "Jump",
  "sequenceName": "Force Jump Movement", 
  "obstacleGroups": [
    {
      "obstacleType": "TrafficCone",
      "obstaclePrefabAddress": "ObstacleRoadworksCone",
      "blockedLanes": [0, 1, 2],     // All lanes (must jump)
      "spawnDistance": 15.0,
      "groupCount": 2,
      "groupSeparation": 8.0
    }
  ]
}
```

### **Slide Training**
```json
{
  "targetStepType": "Slide",
  "sequenceName": "Force Slide Movement",
  "obstacleGroups": [
    {
      "obstacleType": "HighBarrier", 
      "obstaclePrefabAddress": "ObstacleHighBarrier",
      "blockedLanes": [0, 1, 2],     // All lanes (must slide)
      "spawnDistance": 15.0,
      "groupCount": 2,
      "groupSeparation": 8.0
    }
  ]
}
```

## 🔧 **Configuration Options**

### **Lane System:**
- **Lane 0**: Left lane
- **Lane 1**: Center lane  
- **Lane 2**: Right lane

### **Obstacle Types Available:**
- **`TrashCan`** → `ObstacleBin` (lane blocking)
- **`TrashCan`** → `ObstacleWheelyBin` (alternative lane blocking)
- **`TrafficCone`** → `ObstacleRoadworksCone` (jump obstacles)
- **`LowBarrier`** → `ObstacleLowBarrier` (jump obstacles)
- **`HighBarrier`** → `ObstacleHighBarrier` (slide obstacles)

### **Spawn Configuration:**
- **`spawnDistance`**: Distance ahead of player to spawn obstacles
- **`groupCount`**: Number of obstacle groups to spawn
- **`groupSeparation`**: Distance between obstacle groups
- **`blockedLanes`**: Array of lane indices to block

## 🎮 **Tutorial Step Integration**

The tutorial steps automatically request obstacles when they start:

### **SwipeLeftStep.cs** ✅
```csharp
protected override void OnStepStarted()
{
    Debug.Log("SwipeLeftStep: Started - Teaching left lane movement");
    RequestTutorialObstacles();
}

private void RequestTutorialObstacles()
{
    var request = new TutorialObstacleSpawnRequest
    {
        StepType = TutorialStepType.SwipeLeft,
        ForceSpawn = true
    };
    
    TutorialEventBus.PublishObstacleSpawnRequest(request);
}
```

### **JumpStep.cs** ✅
```csharp  
protected override void OnStepStarted()
{
    Debug.Log("JumpStep: Started - Teaching jump mechanics");
    RequestTutorialObstacles();
}

private void RequestTutorialObstacles()
{
    var request = new TutorialObstacleSpawnRequest
    {
        StepType = TutorialStepType.Jump,
        ForceSpawn = true
    };
    
    TutorialEventBus.PublishObstacleSpawnRequest(request);
}
```

## 🏗️ **Architecture Overview**

```
TrackManager (Clean - No Tutorial References)
    ↓ Configuration Override
ITrackRunnerConfigProvider (Obstacle Density = 0)
    ↓ Tutorial Events  
TutorialGameStateAdapter (Controls Game State)
    ↓ Obstacle Events
TutorialObstacleSpawner (Tutorial-Specific Spawning)
    ↓ ScriptableObject Config
TutorialObstacleSequence (Data-Driven Configuration)
```

## 📝 **Setup Checklist**

### **Phase 1: Import Sequences** ✅
- [x] JSON files created
- [x] Editor utility ready
- [ ] Import all JSON files to ScriptableObjects
- [ ] Validate imported sequences

### **Phase 2: Configure TutorialObstacleSpawner**
- [ ] Add `TutorialObstacleSpawner` component to scene
- [ ] Assign imported `TutorialObstacleSequence` assets
- [ ] Configure lane offset and obstacle layer
- [ ] Test obstacle spawning

### **Phase 3: Tutorial Integration**  
- [x] Tutorial steps enhanced with obstacle requests
- [x] Event system integrated
- [ ] Test complete tutorial flow
- [ ] Validate obstacle cleanup

### **Phase 4: Polish & Testing**
- [ ] Adjust spawn distances and timing
- [ ] Test with different difficulty levels
- [ ] Validate tutorial completion flow
- [ ] Performance testing

## 🚀 **Next Steps**

1. **Open Unity Editor**
2. **Import Sequences**: `Trash Dash → Tutorial → Sequence Importer → Import All JSON Files`
3. **Add TutorialObstacleSpawner** to your tutorial scene
4. **Assign ScriptableObject sequences** to the spawner
5. **Test tutorial flow** with obstacle spawning

## 🔍 **Troubleshooting**

### **Import Issues:**
- Ensure JSON files are in correct format
- Check Unity Console for parsing errors
- Validate addressable asset references

### **Spawning Issues:**
- Verify `TutorialObstacleSpawner` is in scene
- Check obstacle prefab addressable references
- Ensure tutorial events are firing correctly

### **Performance:**
- Monitor obstacle cleanup
- Check spawning frequency
- Validate memory usage during tutorial

---

**🎉 Your tutorial obstacle system is ready to use!** The clean architecture ensures maintainability while providing powerful, data-driven obstacle configuration. 