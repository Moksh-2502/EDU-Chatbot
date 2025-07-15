# Game Event System Usage Examples

This document shows how to use the inheritance-based GameEvent system with automatic type registration.

## Overview

The GameEvent system uses an abstract base class where all events inherit from `GameEvent` and implement the abstract `EventType` property. The system automatically discovers and registers all GameEvent subclasses at startup using reflection.

## Key Features

### Abstract GameEvent Base Class
```csharp
public abstract class GameEvent
{
    [JsonProperty("eventType")]
    public abstract string EventType { get; }
    
    [SerializeField] public long timestamp;
    
    protected GameEvent()
    {
        this.timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }
}
```

### Automatic Registration
- Uses reflection to find all GameEvent subclasses at startup
- No manual registration required in GameEventRegistry  
- Events are registered based on their EventType property value

## Core Events

### UnityReadyEvent
Sent when Unity finishes initialization:

```csharp
public class UnityReadyEvent : GameEvent
{
    public override string EventType => "UnityReady";
    public string status;
    
    public UnityReadyEvent()
    {
        status = "ready";
    }
}
```

### Storage Events
Located in `AIEduChatbot.SharedCore.Storage` namespace:

```csharp
// Storage request from Unity to React
public class StorageRequestEvent : GameEvent
{
    public override string EventType => "StorageRequest";
    
    [SerializeField] public string operation;
    [SerializeField] public string correlationId;
    [SerializeField] public string userId;
    [SerializeField] public string key;
    [SerializeField] public string jsonData; // JSON string containing the data to save
    
    // Methods for working with JSON data
    public T GetValue<T>() // Deserializes jsonData to type T
    public bool HasData { get; } // Checks if jsonData is not null/empty
}

// Storage response from React to Unity
public class StorageResponseEvent : GameEvent
{
    public override string EventType => "StorageResponse";
    
    [SerializeField] public string correlationId;
    [SerializeField] public bool success;
    [SerializeField] public string jsonData; // JSON string containing the loaded data
    [SerializeField] public string error;
    
    // Methods for working with JSON data
    public T GetValue<T>() // Deserializes jsonData to type T
    public bool HasData { get; } // Checks if jsonData is not null/empty
}
```

## Usage

### Creating Custom Events

```csharp
using AIEduChatbot.UnityReactBridge.Data;

[Serializable]
public class CustomEvent : GameEvent
{
    public override string EventType => "CustomEvent";
    
    [SerializeField] public string message;
    
    public CustomEvent(string message)
    {
        this.message = message;
    }
}
```

### Sending Events to React

```csharp
// Send built-in events
var readyEvent = new UnityReadyEvent();
ReactBridge.SendGameEvent(readyEvent);

// Send custom events
var customEvent = new CustomEvent("Hello React!");
ReactBridge.SendGameEvent(customEvent);
```

### Receiving Events from React

```csharp
public class GameManager : MonoBehaviour, IGameEventHandler
{
    void Start()
    {
        IGameEventHandlerCollection.Instance.RegisterHandler(this);
    }

    public void OnGameEventReceived(GameEvent gameEvent)
    {
        switch (gameEvent)
        {
            case StorageResponseEvent storageResponse:
                Debug.Log($"Storage response: {storageResponse.success}");
                break;
            case CustomEvent customEvent:
                Debug.Log($"Custom event: {customEvent.message}");
                break;
            default:
                Debug.Log($"Unhandled event: {gameEvent.EventType}");
                break;
        }
    }
}
```

### React Side Handling

```typescript
const bridge = useUnityBridgeWithStorage({
  gameConfig: unityConfig,
  events: {
    onGameEvent: (gameEvent) => {
      switch (gameEvent.eventType) {
        case 'UnityReady':
          console.log('Unity is ready:', gameEvent.status);
          break;
        case 'CustomEvent':
          console.log('Custom event:', gameEvent.message);
          break;
      }
    }
  }
});
```

## Storage Integration

Storage events are handled automatically by `ReactGameStorageService`:

```csharp
// Storage operations automatically use the event system
var storageService = new ReactGameStorageService();
await storageService.SaveAsync("player_data", playerData);
var data = await storageService.LoadAsync<PlayerData>("player_data");
```

### Working with Storage Event Data

Storage events now use JSON strings for data to ensure proper serialization:

```csharp
// Receiving storage requests (on React side)
public void OnGameEventReceived(GameEvent gameEvent)
{
    switch (gameEvent)
    {
        case StorageRequestEvent request:
            if (request.operation == "save" && request.HasData)
            {
                // Deserialize the data to a specific type
                var playerData = request.GetValue<PlayerData>();
                // Process the save request...
            }
            break;
            
        case StorageResponseEvent response:
            if (response.success && response.HasData)
            {
                // Deserialize the loaded data
                var playerData = response.GetValue<PlayerData>();
                // Use the loaded data...
            }
            break;
    }
}
```

### React Side Storage Handling

TypeScript interfaces for the updated storage events:

```typescript
interface StorageRequestEvent extends GameEvent {
  eventType: 'StorageRequest';
  operation: 'load' | 'save' | 'delete' | 'exists';
  correlationId: string;
  userId: string;
  key: string;
  jsonData?: string; // JSON string containing the data to save
}

interface StorageResponseEvent extends GameEvent {
  eventType: 'StorageResponse';
  correlationId: string;
  success: boolean;
  jsonData?: string; // JSON string containing the loaded data
  error?: string;
}
```

## Benefits

1. **Automatic Registration**: No manual event type registration needed
2. **Type Safety**: Strongly-typed event classes
3. **Clean Code**: No constructor parameters for event types
4. **Extensible**: Easy to add new event types
5. **Reflection-Based**: Automatic discovery of all event types 