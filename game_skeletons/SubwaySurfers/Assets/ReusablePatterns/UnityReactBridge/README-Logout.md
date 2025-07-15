# Unity-React Bridge: Logout Functionality

This document describes the logout functionality implemented in the Unity-React Bridge system, which allows Unity to trigger user logout in the React application.

## Overview

The logout functionality enables Unity to send a logout message to React, which then handles the user authentication logout process using NextAuth. This is useful when you want to implement logout buttons or automatic logout scenarios within your Unity game.

## Architecture

The logout flow consists of three main components:

1. **Unity**: Creates and sends `LogoutMessage` to React
2. **React**: Receives the message and calls NextAuth's `signOut` function
3. **NextAuth**: Handles the authentication logout and redirects the user

## Implementation Details

### Unity Side

#### 1. LogoutMessage Class
```csharp
// Located in: Scripts/Data/LogoutMessage.cs
public class LogoutMessage : ReactGameMessage
{
    public override string MessageType => "logout";
    public string Reason { get; set; }
    
    public LogoutMessage(string reason = "user_requested")
    {
        Reason = reason ?? "user_requested";
    }
}
```

#### 2. Usage Example
```csharp
// Simple usage
ReactBridge.SendGameMessage(new LogoutMessage());

// With custom reason
ReactBridge.SendGameMessage(new LogoutMessage("session_expired"));

// Check availability first
if (ReactBridge.IsAvailable)
{
    ReactBridge.SendGameMessage(new LogoutMessage("user_requested"));
}
```

### React Side

#### 1. Message Handler
```typescript
// Located in: hooks/useUnityBridge.ts
case 'logout': {
  const logoutMessage = message as LogoutMessage;
  try {
    console.log('[Unity Bridge] User logout requested from Unity', {
      reason: logoutMessage.reason || 'user_requested',
      timestamp: logoutMessage.timestamp
    });
    
    await signOut({
      redirectTo: '/',
    });
    
    logUnityBridge('User successfully logged out');
  } catch (error) {
    console.error('[Unity Bridge] Error during logout:', error);
  }
  break;
}
```

#### 2. TypeScript Interface
```typescript
// Located in: lib/types/unity-bridge.ts
export interface LogoutMessage extends ReactGameMessage {
  messageType: 'logout';
  reason?: string;
}
```

## Usage Scenarios

### 1. Logout Button in Unity UI
```csharp
public class LogOutButton : MonoBehaviour
{
    public void OnLogoutButtonClick()
    {
        ReactBridge.SendGameMessage(new LogoutMessage("user_requested"));
    }
}
```

### 2. Automatic Logout on Game Event
```csharp
public class GameSession : MonoBehaviour
{
    private void OnSessionTimeout()
    {
        ReactBridge.SendGameMessage(new LogoutMessage("session_timeout"));
    }
    
    private void OnInactivity()
    {
        ReactBridge.SendGameMessage(new LogoutMessage("user_inactive"));
    }
}
```

### 3. Error-based Logout
```csharp
public class AuthenticationManager : MonoBehaviour
{
    private void OnTokenExpired()
    {
        ReactBridge.SendGameMessage(new LogoutMessage("token_expired"));
    }
    
    private void OnAuthenticationError()
    {
        ReactBridge.SendGameMessage(new LogoutMessage("auth_error"));
    }
}
```

## Message Flow

1. **Unity**: Creates `LogoutMessage` with optional reason
2. **Unity**: Calls `ReactBridge.SendGameMessage(new LogoutMessage(reason))`
3. **Unity**: Serializes message to JSON and sends via WebGL bridge
4. **React**: Receives message in `useUnityBridge` hook
5. **React**: Parses message and extracts logout reason
6. **React**: Gets current URL and encodes it as query parameter
7. **React**: Calls NextAuth `signOut({ redirectTo: '/login?redirectUrl=...' })`
8. **NextAuth**: Clears session and redirects user to login page with redirect URL
9. **User**: After logging in, gets redirected to the original URL

## Logout Reasons

Common logout reasons you can use:

- `"user_requested"` - User clicked logout button (default)
- `"session_timeout"` - Session expired
- `"user_inactive"` - User was inactive for too long
- `"token_expired"` - Authentication token expired
- `"auth_error"` - Authentication error occurred
- `"game_ended"` - Game session ended
- `"maintenance"` - System maintenance mode

## Error Handling

### Unity Side
```csharp
try
{
    ReactBridge.SendGameMessage(new LogoutMessage("user_requested"));
}
catch (Exception ex)
{
    Debug.LogError($"Failed to send logout message: {ex.Message}");
}
```

### React Side
The React handler includes error handling that:
- Logs errors to console
- Calls `options.events?.onError` if provided
- Prevents crashes from breaking the message handler

## Testing

### In Unity Editor
When testing in the Unity Editor, the logout message will be logged but not sent to React:
```
[ReactBridge] Mock: SendMessageToReact({"messageType":"logout","reason":"user_requested","timestamp":1234567890})
```

### In WebGL Build
When running in a WebGL build with React, you'll see:
```
[Unity Bridge] User logout requested from Unity {reason: "user_requested", timestamp: 1234567890}
[Unity Bridge] User successfully logged out
```

## Best Practices

1. **Check Bridge Availability**: Always check `ReactBridge.IsAvailable` before sending messages
2. **Use Descriptive Reasons**: Provide meaningful logout reasons for debugging and analytics
3. **Handle Errors**: Implement proper error handling for network issues
4. **Prevent Multiple Clicks**: Disable logout buttons after clicking to prevent double-logout
5. **Log Events**: Log logout events for debugging and user behavior analysis

## Integration with Authentication System

The logout functionality integrates seamlessly with the existing NextAuth authentication system:

- Uses NextAuth's `signOut` function for proper session cleanup
- Redirects to the home page after logout
- Clears all authentication tokens and session data
- Works with Google OAuth and other authentication providers

## Redirect Behavior

The logout system preserves user context by implementing smart redirect behavior:

### During Logout
1. **Current URL Capture**: The current URL (including query parameters) is captured
2. **Direct Redirect**: User is redirected to login page with the original URL as a query parameter
3. **URL Preservation**: The original URL is passed directly via `/login?redirectUrl=...`

### During Login
1. **URL Parameter**: The login page reads the `redirectUrl` query parameter
2. **Callback URL**: Uses the redirect URL as the callback after successful authentication
3. **Fallback**: Defaults to home page `/` if no redirect URL is provided

### Examples

**Scenario 1: Direct logout from Unity**
```
User is on: /templates/PR97-1ce668c
User logs out via Unity → Redirected to /login?redirectUrl=/templates/PR97-1ce668c
User logs in → Redirected to /templates/PR97-1ce668c
```

**Scenario 2: Middleware redirect**
```
User navigates to: /templates/PR97-1ce668c (while logged out)
Middleware redirects to: /login?redirectUrl=/templates/PR97-1ce668c
User logs in → Redirected to /templates/PR97-1ce668c
```

This approach is cleaner than using localStorage and ensures users don't lose their context when logging out from within the Unity game.

## Example Component

See `Examples/LogOutButton.cs` for a complete implementation example that shows:
- How to attach the script to a UI button
- Proper event handling and cleanup
- Error handling and availability checks
- Multiple ways to trigger logout

This implementation ensures a smooth logout experience for users regardless of whether they initiate logout from Unity or React components. 