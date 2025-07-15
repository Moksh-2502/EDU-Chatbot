# Leaderboard System

A comprehensive leaderboard system for Unity games using Unity Gaming Services Leaderboards with smart local player inclusion and efficient UI management using ObjectPool.

## Features

- ✅ **Unity Leaderboards Integration**: Full integration with Unity Gaming Services
- ✅ **Smart Local Player Inclusion**: Always shows local player, even if not in top X
- ✅ **Object Pooling**: Efficient UI management using Unity's ObjectPool
- ✅ **Async/Await Pattern**: Modern async programming with proper error handling
- ✅ **Auto-refresh**: Configurable automatic leaderboard updates
- ✅ **Multiple Leaderboards**: Support for weekly, all-time, or custom leaderboards
- ✅ **Offline Handling**: Graceful degradation when services are unavailable

## Architecture

### Core Components

1. **ILeaderboardService** - Extended interface adding smart local player logic on top of Unity SDK
2. **LeaderboardService** - Implementation using Unity SDK LeaderboardsService with smart logic
3. **LeaderboardEntryWithLocalFlag** - Lightweight wrapper around Unity's LeaderboardEntry
4. **LeaderboardEntryUIItem** - Individual UI component for displaying entries
5. **LeaderboardEntriesDrawer** - Main UI controller with ObjectPool management
6. **LeaderboardManager** - Game integration manager

**Note:** This implementation directly uses Unity's `Unity.Services.Leaderboards.Models.LeaderboardEntry` instead of creating redundant data models.

## Setup

### 1. Unity Services Configuration

Ensure your project has Unity Gaming Services configured:
- Unity Services window: Enable Authentication and Leaderboards
- Create leaderboards in Unity Dashboard
- Note your leaderboard IDs

### 2. Prefab Setup

Create a prefab for `LeaderboardEntryUIItem` with:
- Text component for rank number
- Text component for player name  
- Text component for score
- Optional: Visual indicator for local player

### 3. Scene Setup

Add `LeaderboardEntriesDrawer` to your scene:
- Set `entriesContainer` to a parent Transform
- Assign your `LeaderboardEntryUIItem` prefab
- Configure `maxEntriesToShow` and `leaderboardId`
- Optional: Add loading and error UI elements

## Usage

### Basic Usage

```csharp
// Initialize and display leaderboard
var drawer = FindObjectOfType<LeaderboardEntriesDrawer>();
drawer.RefreshLeaderboard();

// Submit a score
await drawer.SubmitScore(12500);
```

### Advanced Integration

```csharp
// Using LeaderboardManager for game integration
var leaderboardManager = FindObjectOfType<LeaderboardManager>();

// Submit score to multiple leaderboards
bool success = await leaderboardManager.SubmitGameScore(playerScore);

// Get local player's current ranking
var weeklyEntry = await leaderboardManager.GetLocalPlayerWeeklyEntry();
Debug.Log($"Player rank: {weeklyEntry?.rank}, Score: {weeklyEntry?.score}");
```

### Direct Service Usage

```csharp
// Using the service directly
ILeaderboardService service = new LeaderboardService();
await service.InitializeAsync();

// Get top 10 entries with local player guaranteed
var entries = await service.GetTopEntriesWithLocalPlayerAsync(10, "weekly_leaderboard");
foreach (var entry in entries)
{
    Debug.Log($"Rank: {entry.Rank + 1}, Player: {entry.PlayerName}, Score: {entry.Score}, IsLocal: {entry.IsLocalPlayer}");
}

// Submit score (Unity SDK uses double for scores)
bool success = await service.SubmitScoreAsync("weekly_leaderboard", 15000.0);
```

## Smart Local Player Logic

The system ensures the local player is always visible in the leaderboard:

1. **If local player is in top X**: Shows normal top X list with local player highlighted
2. **If local player is NOT in top X**: Shows top (X-1) entries + local player as the last entry
3. **If no local player data**: Shows top X entries normally

This guarantees players can always see their position relative to top performers.

## Configuration

### LeaderboardEntriesDrawer Settings

- `maxEntriesToShow`: Number of entries to display (default: 10)
- `leaderboardId`: Target leaderboard identifier
- `refreshInterval`: Auto-refresh interval in seconds (default: 30)

### Visual Customization

LeaderboardEntryUIItem supports:
- `normalTextColor`: Color for regular entries
- `localPlayerTextColor`: Color for local player entry
- `localPlayerIndicator`: GameObject to show for local player

## Error Handling

The system includes comprehensive error handling:
- Network connectivity issues
- Unity Services authentication failures
- Invalid leaderboard IDs
- Rate limiting
- Graceful UI degradation

## Performance

- **Object Pooling**: Efficient memory management for UI elements
- **Async Operations**: Non-blocking API calls
- **Smart Caching**: Reduces redundant service calls
- **Batch Operations**: Parallel API requests where possible

## Dependencies

- Unity Gaming Services (com.unity.services.leaderboards)
- Unity Authentication (com.unity.services.authentication)
- Unity Services Core (com.unity.services.core)

## Troubleshooting

### Common Issues

1. **"Not authenticated" errors**: Ensure Unity Services are properly initialized
2. **Empty leaderboards**: Check leaderboard IDs match Unity Dashboard
3. **UI not updating**: Verify entriesContainer and prefab are assigned
4. **Network errors**: Handle offline scenarios in your game flow

### Debug Logging

Enable debug logging to troubleshoot:
```csharp
// Service logs initialization and API calls
// UI logs entry updates and pool operations
// Check Unity Console for detailed error messages
```

## Best Practices

1. **Initialize Early**: Call `InitializeAsync()` during app startup
2. **Handle Failures**: Always check return values and handle errors gracefully
3. **Rate Limiting**: Don't submit scores too frequently
4. **UI Feedback**: Show loading states and error messages to users
5. **Testing**: Test with multiple accounts and network conditions

## Example Integration

See `LeaderboardManager.cs` for a complete example of integrating the leaderboard system with your game's scoring system. 