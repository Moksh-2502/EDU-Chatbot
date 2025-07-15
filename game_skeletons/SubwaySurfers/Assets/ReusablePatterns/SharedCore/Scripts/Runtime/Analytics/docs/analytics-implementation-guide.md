# Analytics Implementation Guide

## Overview

The SubwaySurfers project uses a modular analytics system built on top of a core analytics framework located in `SharedCore`. The system uses MixPanel as the analytics provider and supports automatic event sender discovery and initialization.

## Quick Start: How to Send Events

### 🚀 Direct Event Tracking (Most Common)
```csharp
using SharedCore.Analytics;
using YourProject.Analytics.Events;

// Send an event immediately
var event = new PlayerLevelUpEvent(5, 4, "experience");
IAnalyticsService.Instance?.TrackEvent(event);
```

### 📊 Create Custom Events
```csharp
using SharedCore.Analytics;

namespace YourProject.Analytics.Events
{
    public class PlayerLevelUpEvent : BaseAnalyticsEvent
    {
        public override string EventName => "player_level_up";
        
        // These properties are automatically included and converted to snake_case
        public int NewLevel { get; }
        public int PreviousLevel { get; }
        public string LevelUpMethod { get; }

        public PlayerLevelUpEvent(int newLevel, int previousLevel, string levelUpMethod)
        {
            NewLevel = newLevel;
            PreviousLevel = previousLevel;
            LevelUpMethod = levelUpMethod;
        }
        
        // Only override this if you need computed/dynamic properties
        protected override Dictionary<string, object> GetCustomProperties()
        {
            return new Dictionary<string, object>
            {
                { "level_difference", NewLevel - PreviousLevel },
                { "is_milestone", NewLevel % 10 == 0 }
            };
        }
    }
}
```

### 🔄 Automatic Event Tracking via Senders
Create event senders that automatically listen to game events and send analytics:
```csharp
[UnityEngine.Scripting.Preserve]
public class PlayerAnalyticsSender : IAnalyticsEventSender
{
    public int InitializationPriority => 10;
    public bool IsActive => _isInitialized;
    
    public void Initialize()
    {
        _playerManager.OnLevelUp += (newLevel, prevLevel) => {
            var evt = new PlayerLevelUpEvent(newLevel, prevLevel, "experience");
            IAnalyticsService.Instance?.TrackEvent(evt);
        };
    }
}
```

## Architecture

### Core Components

#### 1. Analytics Service (`IAnalyticsService`)
The main interface for tracking analytics events across different analytics services.
- **Location**: `Assets/ReusablePatterns/SharedCore/Scripts/Runtime/Analytics/IAnalyticsService.cs`
- **Implementation**: `MixPanelAnalyticsService`
- **Pattern**: Singleton with static instance access

#### 2. Analytics Events (`IAnalyticsEvent`)
Base interface that all analytics events must implement.
- **Location**: `Assets/ReusablePatterns/SharedCore/Scripts/Runtime/Analytics/Events/IAnalyticsEvent.cs`
- **Base Implementation**: `BaseAnalyticsEvent` provides automatic property discovery and common properties
- **Required Methods**: 
  - `string EventName { get; }`
  - `Dictionary<string, object> GetProperties()` (automatically implemented in BaseAnalyticsEvent)

#### 3. Analytics Event Senders (`IAnalyticsEventSender`)
Components responsible for listening to game events and sending corresponding analytics events.
- **Location**: `Assets/ReusablePatterns/SharedCore/Scripts/Runtime/Analytics/IAnalyticsEventSender.cs`
- **Auto-Discovery**: Automatically found via reflection and initialized by `AnalyticsManager`
- **Required Methods**:
  - `void Initialize()`
  - `void Dispose()`
  - `int InitializationPriority { get; }`
  - `bool IsActive { get; }`

#### 4. Analytics Manager
Orchestrates the entire analytics system, discovers and initializes event senders.
- **Location**: `Assets/ReusablePatterns/SharedCore/Scripts/Runtime/Analytics/AnalyticsManager.cs`
- **Responsibility**: Auto-discovery of senders, initialization, cleanup

#### 5. Analytics Configuration
ScriptableObject for configuring analytics settings.
- **Location**: `Assets/ReusablePatterns/SharedCore/Scripts/Runtime/Analytics/AnalyticsConfig.cs`
- **Settings**: 
  - `TrackingEnabled`: Global on/off switch
  - `TrackableEnvironments`: Which environments to track
  - `TrackableBuildIds`: Which build IDs to track

## Implementation Guide

### Step 1: Create Analytics Events

Analytics events should inherit from `BaseAnalyticsEvent` for automatic property discovery and base properties. **All public properties are automatically included and converted to snake_case format** (e.g., `UnityVersion` becomes `unity_version`).

#### Example: Simple Event (Recommended Approach)
```csharp
using SharedCore.Analytics;

namespace YourProject.Analytics.Events
{
    /// <summary>
    /// Analytics event for player leveling up.
    /// All public properties are automatically tracked.
    /// </summary>
    public class PlayerLevelUpEvent : BaseAnalyticsEvent
    {
        public override string EventName => "player_level_up";

        // These properties are automatically included in analytics
        public int NewLevel { get; }
        public int PreviousLevel { get; }
        public string LevelUpMethod { get; }
        public float TimeToLevel { get; }

        public PlayerLevelUpEvent(int newLevel, int previousLevel, string levelUpMethod, float timeToLevel)
        {
            NewLevel = newLevel;
            PreviousLevel = previousLevel;
            LevelUpMethod = levelUpMethod;
            TimeToLevel = timeToLevel;
        }

        // Only override this for computed/dynamic properties not stored as class properties
        protected override Dictionary<string, object> GetCustomProperties()
        {
            return new Dictionary<string, object>
            {
                { "level_difference", NewLevel - PreviousLevel },
                { "is_milestone_level", NewLevel % 10 == 0 },
                { "leveling_speed", NewLevel > PreviousLevel ? TimeToLevel / (NewLevel - PreviousLevel) : 0 }
            };
        }
    }
}
```

#### Example: Complex Event with Automatic Property Discovery
```csharp
using SharedCore.Analytics;

namespace YourProject.Analytics.Events
{
    /// <summary>
    /// Analytics event for in-app purchases.
    /// Demonstrates automatic property discovery.
    /// </summary>
    public class PurchaseEvent : BaseAnalyticsEvent
    {
        public override string EventName => "purchase_completed";

        // All these properties are automatically tracked as snake_case
        public string ProductId { get; }
        public decimal Price { get; }
        public string Currency { get; }
        public string Store { get; }
        public bool IsFirstPurchase { get; }
        public string PurchaseSource { get; }
        public int PlayerLevel { get; }

        public PurchaseEvent(string productId, decimal price, string currency, string store, 
                           bool isFirstPurchase, string purchaseSource, int playerLevel)
        {
            ProductId = productId;
            Price = price;
            Currency = currency;
            Store = store;
            IsFirstPurchase = isFirstPurchase;
            PurchaseSource = purchaseSource;
            PlayerLevel = playerLevel;
        }

        // Only add computed properties here
        protected override Dictionary<string, object> GetCustomProperties()
        {
            return new Dictionary<string, object>
            {
                { "product_category", GetProductCategory(ProductId) },
                { "price_tier", GetPriceTier(Price) },
                { "days_since_install", GetDaysSinceInstall() }
            };
        }

        private string GetProductCategory(string productId)
        {
            if (productId.Contains("character")) return "character";
            if (productId.Contains("powerup")) return "powerup";
            if (productId.Contains("currency")) return "currency";
            return "other";
        }

        private string GetPriceTier(decimal price)
        {
            if (price < 1) return "micro";
            if (price < 5) return "small";
            if (price < 20) return "medium";
            return "premium";
        }

        private int GetDaysSinceInstall()
        {
            // Implementation to calculate days since install
            return 0; // Placeholder
        }
    }
}
```

### Step 2: Create Analytics Event Senders

Event senders listen to game events and send corresponding analytics events. They are automatically discovered and initialized by the system.

#### Key Requirements:
1. **Must implement `IAnalyticsEventSender`**
2. **Must have `[UnityEngine.Scripting.Preserve]` attribute** for IL2CPP compatibility
3. **Must have parameterless constructor**
4. **Should handle initialization gracefully**

#### Example: Player Analytics Sender
```csharp
using SharedCore.Analytics;
using UnityEngine;
using YourProject.Analytics.Events;

namespace YourProject.Analytics.Senders
{
    [UnityEngine.Scripting.Preserve]
    public class PlayerAnalyticsSender : IAnalyticsEventSender
    {
        private IPlayerManager _playerManager;
        private ILevelManager _levelManager;
        private bool _isInitialized;

        public int InitializationPriority => 10; // Higher priority than core game events
        public bool IsActive => _isInitialized && _playerManager != null;

        public void Initialize()
        {
            if (_isInitialized)
                return;

            try
            {
                // Find required components
                _playerManager = Object.FindFirstObjectByType<PlayerManager>(FindObjectsInactive.Include);
                _levelManager = Object.FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);

                if (_playerManager != null)
                {
                    // Subscribe to events
                    _playerManager.OnLevelUp += OnPlayerLevelUp;
                    _playerManager.OnPlayerDeath += OnPlayerDeath;
                    
                    _isInitialized = true;
                    Debug.Log("PlayerAnalyticsSender initialized successfully");
                }
                else
                {
                    Debug.LogWarning("PlayerAnalyticsSender: PlayerManager not found during initialization");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"PlayerAnalyticsSender initialization failed: {ex.Message}");
            }
        }

        private void OnPlayerLevelUp(int newLevel, int previousLevel)
        {
            if (!IsActive) return;

            try
            {
                var levelUpMethod = DetermineLevelUpMethod();
                var timeToLevel = CalculateTimeToLevel();
                var levelUpEvent = new PlayerLevelUpEvent(newLevel, previousLevel, levelUpMethod, timeToLevel);
                IAnalyticsService.Instance?.TrackEvent(levelUpEvent);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"PlayerAnalyticsSender: Error tracking level up event: {ex.Message}");
            }
        }

        private void OnPlayerDeath(string causeOfDeath, float survivalTime)
        {
            if (!IsActive) return;

            try
            {
                var deathEvent = new PlayerDeathEvent(causeOfDeath, survivalTime, _playerManager.CurrentLevel);
                IAnalyticsService.Instance?.TrackEvent(deathEvent);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"PlayerAnalyticsSender: Error tracking death event: {ex.Message}");
            }
        }

        private string DetermineLevelUpMethod()
        {
            // Add logic to determine how player leveled up
            return "experience"; // Default
        }

        private float CalculateTimeToLevel()
        {
            // Add logic to calculate time taken to level up
            return 0f; // Placeholder
        }

        public void Dispose()
        {
            if (_playerManager != null)
            {
                _playerManager.OnLevelUp -= OnPlayerLevelUp;
                _playerManager.OnPlayerDeath -= OnPlayerDeath;
            }

            _playerManager = null;
            _levelManager = null;
            _isInitialized = false;
        }
    }
}
```

### Step 3: Manual Event Tracking

For one-off events or immediate tracking, you can send analytics events directly:

```csharp
using SharedCore.Analytics;
using YourProject.Analytics.Events;

public class SomeGameSystem : MonoBehaviour
{
    public void OnSpecialAction()
    {
        // Create and send analytics event directly
        var actionEvent = new SpecialActionEvent("button_click", "main_menu", Time.time);
        IAnalyticsService.Instance?.TrackEvent(actionEvent);
    }
    
    public void OnPurchaseCompleted(string productId, decimal price)
    {
        // Send purchase event - all properties automatically tracked
        var purchaseEvent = new PurchaseEvent(
            productId, 
            price, 
            "USD", 
            "unity", 
            IsFirstTimePurchaser(),
            "main_menu",
            GetCurrentPlayerLevel()
        );
        IAnalyticsService.Instance?.TrackEvent(purchaseEvent);
    }

    public void OnLevelCompleted(int levelNumber, float completionTime, int score)
    {
        // Simple event creation with automatic property tracking
        var levelEvent = new LevelCompletedEvent(levelNumber, completionTime, score, GetDifficulty());
        IAnalyticsService.Instance?.TrackEvent(levelEvent);
    }
}
```

### Step 4: Configuration

#### Create Analytics Config Asset
1. Right-click in Project window
2. Choose `Create > ReusablePatterns > Analytics > Analytics Config`
3. Configure settings:
   - **TrackingEnabled**: Set to `true` for production
   - **TrackableEnvironments**: e.g., `["production", "staging"]`
   - **TrackableBuildIds**: Specific build IDs to track

#### Setup Analytics Manager
1. Add `AnalyticsManager` component to a GameObject in your scene
2. Assign the `AnalyticsConfig` asset to the manager
3. Ensure the GameObject persists across scenes (or add to each scene)

## Best Practices

### 1. Event Naming Convention
- Use snake_case for event names
- Be descriptive but concise
- Group related events with prefixes: `player_level_up`, `player_death`, `shop_purchase_completed`

### 2. Property Design
- **Automatic Properties**: Define as public properties in your event class - they're automatically converted to snake_case
- **Custom Properties**: Use `GetCustomProperties()` only for computed/dynamic values
- Keep property values simple (strings, numbers, booleans)
- Avoid nested objects in properties

### 3. Error Handling
- Always wrap analytics calls in try-catch blocks
- Log errors but don't let analytics failures break game functionality
- Use null-conditional operators when accessing the analytics service

### 4. Performance Considerations
- Keep event properties lightweight
- Avoid expensive calculations in GetCustomProperties()
- Consider batching events for high-frequency actions

### 5. Privacy and Compliance
- Only track necessary data
- Respect user privacy settings
- Follow platform-specific analytics guidelines

## Common Patterns

### 1. Funnel Analysis
Track user progression through game flows:
```csharp
// Tutorial funnel - properties automatically tracked
new TutorialStepEvent("tutorial_started", 1, "first_run");
new TutorialStepEvent("tutorial_step_completed", 2, "movement");
new TutorialStepEvent("tutorial_completed", 10, "success");
```

### 2. Economy Tracking
Monitor in-game economy:
```csharp
// Currency events - all properties automatically included
new CurrencyGainedEvent("coins", 100, "level_completion", playerLevel, timeInLevel);
new CurrencySpentEvent("coins", 50, "character_unlock", playerLevel, remainingCoins);
```

### 3. Performance Metrics
Track game performance:
```csharp
// Performance events - simple property tracking
new PerformanceEvent("level_load_time", loadTimeMs, levelName, deviceTier);
new PerformanceEvent("fps_average", averageFPS, sceneType, playerCount);
```

## Key Changes in Property Handling

### ✅ New Approach (Automatic Property Discovery)
```csharp
public class PlayerLevelUpEvent : BaseAnalyticsEvent
{
    public override string EventName => "player_level_up";
    
    // These are automatically tracked as snake_case
    public int NewLevel { get; }
    public int PreviousLevel { get; }
    public string LevelUpMethod { get; }
    
    public PlayerLevelUpEvent(int newLevel, int previousLevel, string levelUpMethod)
    {
        NewLevel = newLevel;           // Becomes "new_level"
        PreviousLevel = previousLevel; // Becomes "previous_level" 
        LevelUpMethod = levelUpMethod; // Becomes "level_up_method"
    }
    
    // Only for computed properties
    protected override Dictionary<string, object> GetCustomProperties()
    {
        return new Dictionary<string, object>
        {
            { "level_difference", NewLevel - PreviousLevel }
        };
    }
}
```

### ❌ Old Approach (Manual Property Definition)
```csharp
// Don't do this anymore - properties are now automatic
protected override Dictionary<string, object> GetCustomProperties()
{
    return new Dictionary<string, object>
    {
        { "new_level", NewLevel },        // Redundant - automatically included
        { "previous_level", PreviousLevel }, // Redundant - automatically included
        { "level_up_method", LevelUpMethod }, // Redundant - automatically included
        { "level_difference", NewLevel - PreviousLevel } // Still needed - computed property
    };
}
```

## Troubleshooting

### Common Issues

1. **Events not being sent**
   - Check if `TrackingEnabled` is true in AnalyticsConfig
   - Verify environment/build ID configuration
   - Ensure Analytics Manager is in the scene and configured

2. **Event Senders not initializing**
   - Add `[UnityEngine.Scripting.Preserve]` attribute
   - Check for exceptions in console during initialization
   - Verify required components exist in the scene

3. **Missing properties in analytics**
   - Ensure properties are public (private properties are not automatically tracked)
   - Check GetCustomProperties() implementation for computed values
   - Verify property values are not null

4. **Performance issues**
   - Reduce frequency of high-volume events
   - Simplify property calculations in GetCustomProperties()
   - Consider event batching for rapid-fire events

### Debugging

Enable debug logging by checking the Debug.Log statements in the analytics system. The system logs:
- Event sender discovery and initialization
- Event tracking attempts
- Property discovery and conversion
- Configuration validation
- Error conditions

## Example Implementation Checklist

When implementing analytics for a new feature:

- [ ] Define what events need tracking
- [ ] Create event classes inheriting from `BaseAnalyticsEvent`
- [ ] Define all relevant data as public properties (automatically tracked)
- [ ] Use `GetCustomProperties()` only for computed/dynamic values
- [ ] Create event sender class implementing `IAnalyticsEventSender` (if needed)
- [ ] Add `[UnityEngine.Scripting.Preserve]` attribute to sender
- [ ] Subscribe to relevant game events in Initialize()
- [ ] Implement proper cleanup in Dispose()
- [ ] Test events are being sent (check debug logs)
- [ ] Verify properties appear correctly in snake_case format
- [ ] Verify events appear in MixPanel dashboard
- [ ] Document events for analytics team

## Additional Resources

- **MixPanel Documentation**: https://docs.mixpanel.com/
- **Unity Analytics Best Practices**: https://docs.unity3d.com/Manual/UnityAnalytics.html
- **GDPR Compliance**: Ensure compliance with data protection regulations 