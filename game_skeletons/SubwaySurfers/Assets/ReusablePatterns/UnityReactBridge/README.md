# Unity React Bridge

A plug-and-play communication bridge between Unity WebGL games and React applications.

## Features

- ✅ **Zero Configuration**: Works out of the box with sensible defaults
- ✅ **Graceful Degradation**: Games function normally without React bridge
- ✅ **Type Safe**: Full C# type safety with JSON serialization
- ✅ **User Data Injection**: Automatically receive user authentication data from React
- ✅ **Bi-directional Communication**: Send and receive messages between Unity and React
- ✅ **Error Handling**: Robust error handling with fallback behavior
- ✅ **Unity 2020.3+**: Compatible with modern Unity versions

## Quick Start (3 Steps)

### 1. Copy Package
```bash
cp -r UnityReactBridge/ /path/to/your/unity/project/Assets/
```

### 2. Add Prefab to Scene
- Drag `ReactBridgeManager.prefab` from `Prefabs/` folder to your scene
- The bridge will automatically initialize when the game starts

### 3. (Optional) Handle User Data
```csharp
public class PlayerController : MonoBehaviour, IUserDataHandler
{
    public void OnUserDataReceived(UserData userData)
    {
        if (userData != null && !string.IsNullOrEmpty(userData.name))
        {
            playerNameText.text = $"Welcome, {userData.name}!";
        }
        else
        {
            playerNameText.text = "Welcome, Player!"; // Graceful fallback
        }
    }
}
```

## What You Get

- **User Authentication Data**: Name, email, user type from React auth (now via AWS Cognito)
- **Cognito JWT Tokens**: Secure authentication with proper token validation
- **React Communication**: Send game events back to React interface
- **Error Safety**: All communication wrapped in try-catch blocks
- **Console Logging**: Development-friendly logging and debugging
- **Production Ready**: Optimized for WebGL builds

## Documentation

- 📖 [Integration Guide](Documentation/INTEGRATION_GUIDE.md) - Detailed setup instructions
- 📚 [API Reference](Documentation/API_REFERENCE.md) - Complete API documentation
- 💡 [Examples](Documentation/EXAMPLES.md) - Usage examples and patterns

## Requirements

- Unity 2020.3 or later
- WebGL build target
- React application with Unity React Bridge (React side package)

## Support

This package is designed for **graceful degradation**:
- Works in Unity Editor (with mock data)
- Works in WebGL without React (uses fallbacks)
- Works in React environment (full functionality)

Your game will **always function**, regardless of the environment! 