# Unity React Bridge - API Reference

## Core Classes

### ReactBridge (Static Class)

Main API for sending events from Unity to React.

#### Properties

- `bool IsAvailable` - Returns true if React bridge is available and functional

#### Methods

##### Game Events
- `SendGameEvent(string eventType, object payload = null)` - Send custom event to React
- `SendGameEvent(GameEvent gameEvent)` - Send structured game event to React

##### Convenience Methods
- `SendScore(int score, int level = 1)` - Send score update to React
- `SendGameState(string state)` - Send game state change to React

##### Notification Helpers
- `NotifyGameStarted()` - Notify React that game has started
- `NotifyGamePaused()` - Notify React that game is paused
- `NotifyGameResumed()` - Notify React that game has resumed
- `NotifyGameEnded(int finalScore = 0, int level = 1)` - Notify React that game has ended
- `NotifyLevelCompleted(int level, int score, float time = 0f)` - Notify React of level completion
- `NotifyError(string errorMessage, string errorCode = null)` - Notify React of game error

#### Example Usage

```csharp
// Check if bridge is available
if (ReactBridge.IsAvailable)
{
    // Send score update
    ReactBridge.SendScore(1000, 3);
    
    // Send custom event
    var data = new { playerName = "John", achievement = "First Win" };
    ReactBridge.SendGameEvent("AchievementUnlocked", data);
    
    // Notify game state changes
    ReactBridge.NotifyGameStarted();
    ReactBridge.NotifyGameEnded(finalScore, currentLevel);
}
```

---

### WebGLCommunicationManager (MonoBehaviour)

Handles incoming messages from React and distributes them to game objects.

#### Properties

- `UserData GetCurrentUserData()` - Get current user data (if available)
- `bool HasUserData()` - Check if valid user data has been received

#### Methods

- `RefreshUserDataDistribution()` - Manually trigger user data distribution

#### Inspector Settings

- `bool enableLogging` - Enable/disable debug logging
- `bool sendMockDataInEditor` - Send mock data when running in Unity Editor
- `float mockDataDelaySeconds` - Delay before sending mock data in Editor

---

## Data Classes

### UserData

Represents user authentication data from React.

#### Fields

```csharp
public string id;        // Unique user identifier
public string email;     // User's email address  
public string name;      // User's display name (optional)
public string type;      // User type: "regular", "premium", "guest"
public string image;     // Avatar URL (optional)
```

#### Methods

- `static UserData CreateGuest()` - Create default guest user
- `bool IsValid()` - Check if user data is valid
- `string ToString()` - Get string representation

#### Example Usage

```csharp
public void OnUserDataReceived(UserData userData)
{
    if (userData != null && userData.IsValid())
    {
        Debug.Log($"User: {userData.name} ({userData.type})");
        
        switch (userData.type)
        {
            case "premium":
                EnablePremiumFeatures();
                break;
            case "regular":
                EnableStandardFeatures();
                break;
            case "guest":
                EnableGuestMode();
                break;
        }
    }
}
```

---

### GameEvent

Represents a game event that can be sent to or received from React.

#### Fields

```csharp
public string eventType;    // Type of event
public string payload;      // JSON payload (optional)
public long timestamp;      // Unix timestamp
```

#### Methods

- `GameEvent(string eventType, object payload = null)` - Constructor
- `T GetPayload<T>()` - Deserialize payload to specific type
- `DateTime GetDateTime()` - Get timestamp as DateTime

#### Example Usage

```csharp
// Create and send event
var gameEvent = new GameEvent("PlayerDied", new { level = 3, score = 1500 });
ReactBridge.SendGameEvent(gameEvent);

// Handle received event
public void OnGameEventReceived(GameEvent gameEvent)
{
    if (gameEvent.eventType == "CustomCommand")
    {
        var commandData = gameEvent.GetPayload<CommandData>();
        // Process command...
    }
}
```

---

## Interface Classes

### IUserDataHandler

Implement this interface to receive user authentication data from React.

#### Methods

- `void OnUserDataReceived(UserData userData)` - Called when user data is received

#### Implementation Example

```csharp
public class PlayerController : MonoBehaviour, IUserDataHandler
{
    public void OnUserDataReceived(UserData userData)
    {
        if (userData != null)
        {
            // Customize game for authenticated user
            playerNameText.text = $"Welcome, {userData.name}!";
            LoadUserPreferences(userData.id);
        }
        else
        {
            // Handle guest/anonymous user
            playerNameText.text = "Welcome, Guest!";
            SetDefaultPreferences();
        }
    }
}
```

---

### IGameEventHandler

Implement this interface to receive custom events from React.

#### Methods

- `void OnGameEventReceived(GameEvent gameEvent)` - Called when game event is received

#### Implementation Example

```csharp
public class EventHandler : MonoBehaviour, IGameEventHandler
{
    public void OnGameEventReceived(GameEvent gameEvent)
    {
        switch (gameEvent.eventType)
        {
            case "PauseGame":
                PauseGameplay();
                break;
            case "ResumeGame":
                ResumeGameplay();
                break;
            case "ChangeSettings":
                var settings = gameEvent.GetPayload<GameSettings>();
                ApplySettings(settings);
                break;
        }
    }
}
```

---

## Common Event Types
### Custom Events

You can send any custom event type with any payload:

```csharp
// Achievement unlocked
ReactBridge.SendGameEvent("AchievementUnlocked", new { 
    achievementId = "first_win",
    playerName = userData.name,
    timestamp = DateTime.Now
});

// Item collected
ReactBridge.SendGameEvent("ItemCollected", new {
    itemType = "coin",
    quantity = 10,
    totalCoins = playerCoins
});

// Boss defeated
ReactBridge.SendGameEvent("BossDefeated", new {
    bossName = "Dragon King",
    timeTaken = 120.5f,
    damageDealt = 5000
});
```

---

## Error Handling

All bridge methods include built-in error handling:

- Methods fail gracefully if React bridge is not available
- Errors are logged to Unity Console with `[ReactBridge]` prefix
- Invalid JSON payloads are handled safely
- Network/communication errors don't crash the game

### Best Practices

1. **Always check availability** (optional but recommended):
   ```csharp
   if (ReactBridge.IsAvailable)
   {
       ReactBridge.SendScore(score, level);
   }
   ```

2. **Handle null user data**:
   ```csharp
   public void OnUserDataReceived(UserData userData)
   {
       if (userData == null)
       {
           // Use fallback/guest mode
           return;
       }
       // Process user data...
   }
   ```

3. **Use try-catch for critical operations**:
   ```csharp
   try
   {
       var complexData = CalculateGameStats();
       ReactBridge.SendGameEvent("GameStats", complexData);
   }
   catch (Exception ex)
   {
       Debug.LogError($"Failed to send game stats: {ex.Message}");
   }
   ```

---

## Platform Compatibility

| Platform | User Data | Send Events | Notes |
|----------|-----------|-------------|-------|
| WebGL + React | ✅ Full | ✅ Full | Complete functionality |
| WebGL Standalone | ❌ Mock | ✅ Logged | Events logged to console |
| Unity Editor | ✅ Mock | ✅ Logged | Mock data for testing |
| Other Platforms | ❌ None | ✅ Logged | Bridge disabled, events logged |

The bridge is designed for **graceful degradation** - your game will always function regardless of the environment! 