# Unity Storage Service Guide

This guide explains how to use the Unity storage system that bridges Unity games with React-side storage services.

## Overview

The Unity storage system allows Unity games to store and retrieve user data through the React frontend. The React side can implement various storage backends (local storage, cloud storage, etc.) while Unity games use a consistent interface.

## Architecture

```
Unity Game (C#) 
    ↓ Storage Request
ReactGameStorageService
    ↓ ReactBridge.SendGameEvent
React Frontend (TypeScript)
    ↓ Storage Operation
Storage Backend (Local/Cloud)
    ↓ Response
React Frontend
    ↓ StorageResponse Event
Unity Game
```

## Unity Side Implementation

### 1. Using the Storage Service

```csharp
using AIEduChatbot.UnityReactBridge.Storage;
using Cysharp.Threading.Tasks;

public class GameDataManager : MonoBehaviour
{
    private IGameStorageService _storageService;

    void Start()
    {
        _storageService = new ReactGameStorageService();
    }

    // Save player progress
    public async UniTask SavePlayerProgress(PlayerData playerData)
    {
        try
        {
            await _storageService.SaveAsync("player_progress", playerData);
            Debug.Log("Player progress saved successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save player progress: {ex.Message}");
        }
    }

    // Load player progress
    public async UniTask<PlayerData> LoadPlayerProgress()
    {
        try
        {
            var playerData = await _storageService.LoadAsync<PlayerData>("player_progress");
            if (playerData != null)
            {
                Debug.Log("Player progress loaded successfully");
                return playerData;
            }
            else
            {
                Debug.Log("No player progress found, creating new");
                return new PlayerData(); // Return default data
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load player progress: {ex.Message}");
            return new PlayerData(); // Return default data on error
        }
    }

    // Check if save data exists
    public async UniTask<bool> HasSaveData()
    {
        try
        {
            return await _storageService.ExistsAsync("player_progress");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to check save data: {ex.Message}");
            return false;
        }
    }

    // Delete save data
    public async UniTask DeleteSaveData()
    {
        try
        {
            await _storageService.DeleteAsync("player_progress");
            Debug.Log("Save data deleted successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to delete save data: {ex.Message}");
        }
    }
}

[System.Serializable]
public class PlayerData
{
    public int level = 1;
    public int score = 0;
    public float[] position = new float[3];
    public string playerName = "Player";
    public bool[] unlockedAchievements = new bool[10];
}
```

### 2. Storage Service Features

- **Automatic User Context**: The service automatically includes the current user ID from the session
- **Fallback Mechanism**: Falls back to PlayerPrefs when React bridge is unavailable
- **Error Handling**: Graceful error handling with fallback to local storage
- **Async/Await Support**: Full async support using UniTask

## React Side Implementation

### 1. Basic Usage

```typescript
import { useUnityBridgeWithStorage } from '@/hooks/useUnityBridgeWithStorage';
import { LocalReactStorageService } from '@/lib/types/unity-storage';

export function GamePage() {
  const userData = { id: 'user123', name: 'John Doe' };

  const gameConfig = {
    loaderUrl: '/games/mygame/Build/mygame.loader.js',
    dataUrl: '/games/mygame/Build/mygame.data',
    frameworkUrl: '/games/mygame/Build/mygame.framework.js',
    codeUrl: '/games/mygame/Build/mygame.wasm',
  };

  const {
    unityProvider,
    bridgeState,
    sendUserData,
  } = useUnityBridgeWithStorage({
    gameConfig,
    enableLogging: true,
    storageService: new LocalReactStorageService(),
    events: {
      onGameLoaded: () => {
        sendUserData(userData);
      },
      onGameEvent: (eventData) => {
        console.log('Game event:', eventData);
      },
    },
  });

  return (
    <Unity unityProvider={unityProvider} style={{ width: '100%', height: '600px' }} />
  );
}
```

### 2. Custom Storage Implementation

```typescript
import { IReactStorageService } from '@/lib/types/unity-storage';

// Example: Firebase storage implementation
export class FirebaseStorageService implements IReactStorageService {
  constructor(private firestore: any) {}

  async load<T = any>(userId: string, key: string): Promise<T | null> {
    try {
      const doc = await this.firestore
        .collection('user_data')
        .doc(userId)
        .collection('game_data')
        .doc(key)
        .get();

      if (doc.exists) {
        return doc.data() as T;
      }
      return null;
    } catch (error) {
      console.error('Firebase load error:', error);
      return null;
    }
  }

  async save<T = any>(userId: string, key: string, data: T): Promise<void> {
    try {
      await this.firestore
        .collection('user_data')
        .doc(userId)
        .collection('game_data')
        .doc(key)
        .set(data);
    } catch (error) {
      console.error('Firebase save error:', error);
      throw error;
    }
  }

  async delete(userId: string, key: string): Promise<void> {
    try {
      await this.firestore
        .collection('user_data')
        .doc(userId)
        .collection('game_data')
        .doc(key)
        .delete();
    } catch (error) {
      console.error('Firebase delete error:', error);
      throw error;
    }
  }

  async exists(userId: string, key: string): Promise<boolean> {
    try {
      const doc = await this.firestore
        .collection('user_data')
        .doc(userId)
        .collection('game_data')
        .doc(key)
        .get();

      return doc.exists;
    } catch (error) {
      console.error('Firebase exists error:', error);
      return false;
    }
  }
}
```

### 3. Using the Enhanced Component

```typescript
import { UnityGameWithStorage } from '@/components/unity-game-with-storage';

export function GameContainer() {
  const gameConfig = {
    loaderUrl: '/games/mygame/Build/mygame.loader.js',
    dataUrl: '/games/mygame/Build/mygame.data',
    frameworkUrl: '/games/mygame/Build/mygame.framework.js',
    codeUrl: '/games/mygame/Build/mygame.wasm',
  };

  const userData = {
    id: 'user123',
    name: 'John Doe',
    email: 'john@example.com',
  };

  return (
    <UnityGameWithStorage
      gameConfig={gameConfig}
      userData={userData}
      enableLogging={true}
      onGameEvent={(eventType, payload) => {
        console.log(`Game event: ${eventType}`, payload);
      }}
    />
  );
}
```

## Data Flow

### 1. Storage Request (Unity → React)

1. Unity calls `storageService.SaveAsync("key", data)`
2. ReactGameStorageService generates correlation ID
3. Sends StorageRequest event via ReactBridge
4. React receives event and processes with UnityStorageHandler
5. React sends StorageResponse event back to Unity
6. Unity receives response and completes the async operation

### 2. Request/Response Format

**Storage Request:**
```json
{
  "operation": "save",
  "correlationId": "uuid-1234",
  "userId": "user123",
  "key": "player_progress",
  "data": { "level": 5, "score": 1000 }
}
```

**Storage Response:**
```json
{
  "correlationId": "uuid-1234",
  "response": {
    "success": true,
    "data": null
  }
}
```

## Best Practices

### 1. Error Handling

- Always use try-catch blocks when calling storage operations
- Provide fallback values for load operations
- Log errors for debugging

### 2. Data Structure

- Use serializable classes with `[System.Serializable]` attribute
- Keep data structures simple and avoid circular references
- Use appropriate data types (avoid complex Unity objects)

### 3. Performance

- Batch storage operations when possible
- Cache frequently accessed data
- Avoid storing large amounts of data

### 4. Security

- Never store sensitive data in local storage
- Implement proper user authentication
- Validate data on both Unity and React sides

## Troubleshooting

### Common Issues

1. **Storage requests timeout**: Check if React bridge is properly initialized
2. **Data not persisting**: Verify storage service implementation
3. **Correlation ID mismatches**: Ensure proper event handling setup
4. **Fallback to PlayerPrefs**: ReactBridge is not available or initialized

### Debug Logging

Enable logging in both Unity and React:

```csharp
// Unity: Enable in ReactGameStorageService constructor
Debug.Log("[ReactGameStorageService] Initialized with storage handler");
```

```typescript
// React: Enable in useUnityBridgeWithStorage
const bridge = useUnityBridgeWithStorage({
  // ...
  enableLogging: true,
  enableStorageLogging: true,
});
```

## Migration from PlayerPrefs

To migrate existing PlayerPrefs data:

```csharp
public async UniTask MigrateFromPlayerPrefs()
{
    var keys = new[] { "player_progress", "settings", "achievements" };
    
    foreach (var key in keys)
    {
        if (PlayerPrefs.HasKey(key))
        {
            var data = PlayerPrefs.GetString(key);
            if (!string.IsNullOrEmpty(data))
            {
                // Convert PlayerPrefs string to object
                var deserializedData = JsonUtility.FromJson<YourDataType>(data);
                
                // Save to new storage system
                await _storageService.SaveAsync(key, deserializedData);
                
                // Optionally remove from PlayerPrefs
                PlayerPrefs.DeleteKey(key);
            }
        }
    }
    
    PlayerPrefs.Save();
}
``` 