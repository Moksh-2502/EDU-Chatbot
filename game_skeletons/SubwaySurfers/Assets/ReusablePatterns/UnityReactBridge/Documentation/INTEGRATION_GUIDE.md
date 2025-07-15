# Unity React Bridge - Integration Guide

This guide will walk you through integrating the Unity React Bridge into your Unity game project.

## Prerequisites

- Unity 2020.3 or later
- WebGL build target configured
- Basic understanding of C# and Unity development

## Step 1: Copy Package to Your Project

1. Copy the entire `UnityReactBridge` folder to your Unity project's `Assets` directory:
   ```
   YourUnityProject/
   ├── Assets/
   │   ├── UnityReactBridge/          ← Copy this entire folder here
   │   ├── Scripts/                   ← Your existing game scripts
   │   └── ...
   ```

2. Unity will automatically import all scripts and detect the package.

## Step 2: Add Bridge Manager to Your Scene

1. In your main game scene, locate the `ReactBridgeManager.prefab` in:
   ```
   Assets/UnityReactBridge/Prefabs/ReactBridgeManager.prefab
   ```

2. Drag the prefab into your scene hierarchy.

3. The prefab contains a `WebGLCommunicationManager` component that will:
   - Automatically initialize when the game starts
   - Handle incoming messages from React
   - Distribute user data to your game objects
   - Provide mock data when running in the Unity Editor

## Step 3: Receive User Data (Optional)

To receive user authentication data from React, implement the `IUserDataHandler` interface:

```csharp
using UnityEngine;
using AIEduChatbot.UnityReactBridge.Data;
using AIEduChatbot.UnityReactBridge.Handlers;

public class PlayerController : MonoBehaviour, IUserDataHandler
{
    [SerializeField] private Text playerNameText;
    
    public void OnUserDataReceived(UserData userData)
    {
        if (userData != null && !string.IsNullOrEmpty(userData.name))
        {
            playerNameText.text = $"Welcome, {userData.name}!";
            Debug.Log($"Player logged in: {userData.name} ({userData.email})");
        }
        else
        {
            playerNameText.text = "Welcome, Player!";
            Debug.Log("No user data available - guest mode");
        }
    }
}
```

### Available User Data Fields

```csharp
public class UserData
{
    public string id;        // Unique user identifier
    public string email;     // User's email address
    public string name;      // User's display name (optional)
    public string type;      // User type: "regular", "premium", "guest"
    public string image;     // Avatar URL (optional)
}
```

## Step 4: Send Events to React (Optional)

Use the static `ReactBridge` class to send events to the React interface:

```csharp
using AIEduChatbot.UnityReactBridge.Core;

public class GameManager : MonoBehaviour
{
    public void OnPlayerScored(int score)
    {
        // Send score update to React
        ReactBridge.SendScore(score, currentLevel);
    }
    
    public void OnGameStart()
    {
        // Notify React that game started
        ReactBridge.NotifyGameStarted();
    }
    
    public void OnGameEnd(int finalScore)
    {
        // Notify React with final results
        ReactBridge.NotifyGameEnded(finalScore, currentLevel);
    }
    
    public void OnCustomEvent()
    {
        // Send custom event with data
        var eventData = new { 
            playerName = "John", 
            achievement = "First Win" 
        };
        ReactBridge.SendGameEvent("AchievementUnlocked", eventData);
    }
}
```

### Common Event Methods

- `ReactBridge.NotifyGameStarted()`
- `ReactBridge.NotifyGamePaused()`
- `ReactBridge.NotifyGameResumed()`
- `ReactBridge.NotifyGameEnded(score, level)`
- `ReactBridge.SendScore(score, level)`
- `ReactBridge.SendGameEvent(eventType, payload)`

## Step 5: Build for WebGL

1. Switch to WebGL build target in Unity:
   - File → Build Settings
   - Select "WebGL" platform
   - Click "Switch Platform"

2. Configure WebGL settings:
   - Player Settings → WebGL Settings
   - Ensure "Data Caching" is enabled for better performance

3. Build your game:
   - Click "Build" and choose output directory
   - Unity will generate the necessary WebGL files

## Step 6: Test Integration

### In Unity Editor
- The bridge automatically provides mock user data for testing
- Check the Console for bridge initialization messages
- Use the example scripts' context menu items to test events

### In WebGL Build
- Deploy your WebGL build to a web server
- The React application will automatically detect Unity games
- User authentication data will be sent automatically
- Events will be logged in the browser console

## Troubleshooting

### Bridge Not Initializing
- Ensure `ReactBridgeManager` prefab is in your scene
- Check Console for error messages
- Verify WebGL build target is selected

### User Data Not Received
- Confirm your script implements `IUserDataHandler`
- Check that the GameObject with your script is active
- In Editor, mock data is sent after 2 seconds by default

### Events Not Sending
- Verify you're using `ReactBridge.SendGameEvent()` correctly
- Check browser console for JavaScript errors
- Ensure the React application is properly configured

### Build Errors
- Make sure all scripts are in the correct namespace
- Verify Unity version compatibility (2020.3+)
- Check for missing dependencies

## Advanced Configuration

### Customizing Mock Data (Editor Only)
You can modify the mock data in `WebGLCommunicationManager`:

```csharp
private void SendMockUserData()
{
    var mockUser = UserData.CreateGuest();
    mockUser.name = "Your Test Name";
    mockUser.email = "test@yourcompany.com";
    mockUser.type = "premium";
    
    // ... rest of method
}
```

### Disabling Logging
Set `enableLogging = false` in the `WebGLCommunicationManager` component.

### Custom Event Handling
Implement `IGameEventHandler` to receive events from React:

```csharp
public class CustomEventHandler : MonoBehaviour, IGameEventHandler
{
    public void OnGameEventReceived(GameEvent gameEvent)
    {
        Debug.Log($"Received event: {gameEvent.eventType}");
        
        switch (gameEvent.eventType)
        {
            case "CustomCommand":
                // Handle custom command from React
                break;
        }
    }
}
```

## Next Steps

- Review the [API Reference](API_REFERENCE.md) for complete method documentation
- Check out [Examples](EXAMPLES.md) for more usage patterns
- Customize the bridge for your specific game requirements

## Support

If you encounter issues:
1. Check the Unity Console for error messages
2. Verify your implementation against the examples
3. Ensure your React application is properly configured
4. Test with the provided example scripts first 