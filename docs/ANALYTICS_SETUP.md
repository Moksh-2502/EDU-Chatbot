# Analytics Setup - Simplified

Dead simple analytics tracking with Sentry + Mixpanel. No complex configuration, no state management, just works.

## 🚀 **How It Works**

1. **Import** → Analytics works immediately
2. **Call functions** → Events get tracked  
3. **That's it** → No setup, no initialization, no complexity

## 📋 **Environment Variables**

```bash
# Required
NEXT_PUBLIC_MIXPANEL_TOKEN=your_token_here
NEXT_PUBLIC_SENTRY_DSN=your_sentry_dsn

# Optional (defaults to enabled)
NEXT_PUBLIC_ANALYTICS_ENABLED=true
NEXT_PUBLIC_TRACKABLE_ENVIRONMENTS=dev,staging,production
NEXT_PUBLIC_TRACKABLE_BUILD_IDS=dev-*,staging,production
```

## 🎯 **Usage**

### Direct Function Calls
```typescript
import * as analytics from '@/lib/services/analytics';

// Track events
analytics.trackEvent({
  eventName: 'game_loading_started', 
  gameId: 'my-game',
  gameType: 'unity'
});

// Track errors
analytics.trackError(new Error('Something failed'), {
  context: 'game_loading'
});

// Identify users
analytics.identifyUser({
  id: 'user-123',
  email: 'user@example.com',
  type: 'premium'
});
```

### Session Integration
```typescript
import { useAnalyticsSession } from '@/context/analytics-context';

function MyApp() {
  useAnalyticsSession(); // Handles user identification automatically
  return <div>My App</div>;
}
```