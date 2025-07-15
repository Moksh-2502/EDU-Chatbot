# Difficulty System Configuration Guide

## Overview

The difficulty system now uses a centralized configuration approach through the `DifficultySystemConfig` ScriptableObject. This consolidates all configurable values into a single, easy-to-manage asset.

## Creating a Configuration Asset

### Method 1: Through Unity Editor
1. Right-click in your Project window
2. Navigate to **Create > SubwaySurfers > Difficulty System Config**
3. Name your config asset (e.g., "MainDifficultyConfig")
4. The asset will be automatically populated with 5 pre-configured difficulty levels

### Method 2: Automatic Loading
- Place a config asset named "DifficultySystemConfig" in the `Assets/Resources/` folder
- All difficulty components will automatically find and load this config if no config is manually assigned

## Configuration Structure

### Difficulty Levels (Pre-populated)
The config comes with 5 balanced difficulty levels:

#### Level 0: "Accessible" 
- **Target**: 50-year-old newcomer difficulty
- **Speed**: 4.0 → 6.0 (slow acceleration: 0.05)
- **Obstacles**: 30% density (very sparse)
- **Collectables**: 150% frequency (more helpful items)

#### Level 1: "Beginner"
- **Speed**: 6.0 → 10.0 (gentle acceleration: 0.08)
- **Obstacles**: 50% density 
- **Collectables**: 120% frequency

#### Level 2: "Easy"
- **Speed**: 8.0 → 14.0 (standard acceleration: 0.1)
- **Obstacles**: 80% density
- **Collectables**: 100% frequency (baseline)

#### Level 3: "Normal"
- **Target**: Average Subway Surfer (1 minute into run)
- **Speed**: 10.0 → 18.0 (faster acceleration: 0.12)
- **Obstacles**: 100% density (standard)
- **Collectables**: 80% frequency

#### Level 4: "Expert"
- **Target**: Good Subway Surfer (2-3 minutes into run)
- **Speed**: 12.0 → 24.0 (rapid acceleration: 0.15)
- **Obstacles**: 150% density (challenging)
- **Collectables**: 60% frequency (limited help)

### Collectable Frequency Hierarchy
Each level maintains the exact hierarchy specified:
- **Score Multiplier**: Base frequency (most common)
- **Magnet**: 2x less frequent than Score Multiplier
- **Shield**: 5x less frequent than Score Multiplier
- **Invincibility**: 10x less frequent than Score Multiplier
- **Extra Life**: 20x less frequent than Score Multiplier

### System Timing Configuration
- **Starting Difficulty**: Level 0 (configurable 0-4)
- **Difficulty Cooldown**: 60 seconds between changes
- **Max Lives Threshold**: 30 seconds at max lives triggers increase
- **Auto Progression Start**: 240 seconds (4 minutes) switches to time-based
- **Auto Increase Interval**: 60 seconds in time-based mode

### Adaptive System Settings
- **Difficulty Frequency Multiplier**: 0.2 (how much difficulty affects collectable spawn)
- **Max Frequency Multiplier**: 2.0 (maximum spawn rate increase)
- **Global Spawn Multiplier**: 1.0 (overall spawn rate adjustment)
- **Adaptive Spawn Bonus**: 0.1 (bonus rate when adaptive system active)

### Spawn Timing Settings
- **Min Spawn Interval**: 2.0 seconds
- **Max Spawn Interval**: 8.0 seconds  
- **Difficulty Interval Reduction**: 0.3 (how much difficulty reduces intervals)

## Component Integration

All difficulty system components now reference the config:

### DifficultyManager
- Uses config for: starting level, cooldown time, difficulty levels array
- Auto-loads from Resources if no config assigned

### LifeBasedDifficultyAdjuster
- Uses config for: max lives threshold, difficulty cooldown
- Inherits debug logging setting from config

### TimeBasedDifficultyAdjuster  
- Uses config for: auto progression time, auto increase interval
- Inherits debug logging setting from config

### AdaptiveCollectableSpawner
- Uses config for: difficulty frequency multiplier, max frequency multiplier
- Maintains local base frequencies for fine-tuning

### AdaptiveLootIntegrationBridge
- Uses config for: spawn intervals, timing settings, integration flags
- Uses config for: global multipliers and adaptive bonuses

### GameDifficultyController
- Uses config for: debug logging coordination
- Auto-loads config for system-wide consistency

## Customization Guide

### Modifying Difficulty Levels
1. Select your config asset in the Project window
2. In the Inspector, expand the "Difficulty Levels" array
3. Adjust values for any level:
   - **Speed Parameters**: baseSpeed, maxSpeed, accelerationRate
   - **Obstacle Density**: obstacleDensityMultiplier (0.3 to 2.0 recommended)
   - **Collectable Config**: Modify frequencies while maintaining hierarchy

### Adjusting Timing
- **difficultyCooldown**: Time between difficulty changes (recommended: 30-120 seconds)
- **maxLivesThreshold**: Time at max lives before increase (recommended: 15-60 seconds)
- **autoProgressionStartTime**: When to switch to time-based (recommended: 180-300 seconds)
- **autoIncreaseInterval**: Time-based increase frequency (recommended: 30-120 seconds)

### Fine-tuning Collectables
- **difficultyFrequencyMultiplier**: How much difficulty affects spawns (0.1-0.5)
- **maxFrequencyMultiplier**: Maximum spawn rate multiplier (1.5-3.0)
- **globalSpawnMultiplier**: Overall spawn rate (0.5-2.0)
- **adaptiveSpawnBonus**: Extra spawns when adaptive (0.0-0.5)

## Validation Features

### Automatic Validation
The config includes built-in validation:
- **Hierarchy Checking**: Ensures collectable frequencies maintain proper order
- **Progression Validation**: Warns if difficulty levels don't increase properly
- **Range Clamping**: Automatically keeps values within reasonable ranges

### Manual Validation
- Right-click config asset → **Validate Configuration**
- Checks all settings and reports any issues
- Auto-fixes simple problems like incorrect level indices

### Context Menu Actions
- **Initialize Default Levels**: Resets to the 5 pre-configured levels
- **Validate Configuration**: Runs full validation check

## Debug Features

### Centralized Debug Logging
- Single **enableDebugLogging** flag controls all difficulty system logging
- Each component respects this setting while maintaining local override capability
- **Unified Log Prefix**: All difficulty system logs use the `[DIFFICULTY]` prefix for easy filtering
- **Component Identification**: Each component has a specific sub-prefix:
  - `[DIFFICULTY] [Manager]` - DifficultyManager
  - `[DIFFICULTY] [LifeAdjuster]` - LifeBasedDifficultyAdjuster  
  - `[DIFFICULTY] [TimeAdjuster]` - TimeBasedDifficultyAdjuster
  - `[DIFFICULTY] [CollectableSpawner]` - AdaptiveCollectableSpawner
  - `[DIFFICULTY] [LootBridge]` - AdaptiveLootIntegrationBridge
  - `[DIFFICULTY] [Controller]` - GameDifficultyController
  - `[DIFFICULTY] [Config]` - DifficultySystemConfig

### Console Filtering
To filter difficulty system logs in Unity Console:
1. **Show All Difficulty Logs**: Filter by `[DIFFICULTY]`
2. **Show Specific Component**: Filter by `[DIFFICULTY] [Manager]`, `[DIFFICULTY] [LifeAdjuster]`, etc.
3. **Multiple Components**: Use console search with multiple terms

### Editor Testing Tools
Each component includes context menu testing:
- **DifficultyManager**: Set specific levels, reset system
- **LifeBasedAdjuster**: Trigger thresholds, reset state
- **TimeBasedAdjuster**: Force time-based mode, simulate time
- **AdaptiveCollectableSpawner**: Test weighted selection, show frequencies
- **AdaptiveLootIntegrationBridge**: Test spawns, show current rates
- **GameDifficultyController**: Set levels, show current state

## Best Practices

### Configuration Management
1. **Create Multiple Configs**: Different configs for different game modes
2. **Use Descriptive Names**: Clear naming for different difficulty curves
3. **Version Control**: Include config assets in version control
4. **Test Thoroughly**: Use editor tools to test all difficulty transitions

### Performance Considerations
1. **Reasonable Cooldowns**: Don't set cooldowns too low (< 5 seconds)
2. **Balanced Frequencies**: Avoid extreme collectable frequency values
3. **Smooth Progression**: Ensure difficulty increases feel fair and gradual

### Balancing Guidelines
1. **Start Accessible**: Level 0 should be very forgiving
2. **Maintain Hierarchy**: Keep collectable frequency ratios consistent
3. **Test Edge Cases**: Verify behavior at minimum and maximum difficulty
4. **Player Feedback**: Use debug logging to understand system behavior

## Troubleshooting

### Common Issues
- **No Config Found**: Ensure config is assigned or placed in Resources folder
- **Values Not Applying**: Check component references are properly set
- **Unexpected Behavior**: Enable debug logging to trace system decisions

### Debug Steps
1. Enable debug logging in config
2. Check Unity Console for difficulty system messages
3. Use context menu actions to test individual components
4. Verify config validation passes without errors

This centralized system provides complete control over difficulty progression while maintaining the exact behavioral requirements specified in the original IFD document. 