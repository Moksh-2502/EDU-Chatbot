# Analytics Configuration Guide

This guide explains how to easily configure analytics enablement for different environments.

## Quick Setup

### Method 1: Centralized Configuration (Recommended)

The easiest way to configure analytics is by editing the `ANALYTICS_CONFIG` object in `lib/services/analytics.ts`:

```typescript
const ANALYTICS_CONFIG = {
    // Environment-specific settings
    environments: {
        production: { enabled: true, debug: false },
        staging: { enabled: true, debug: true },
        dev: { enabled: false, debug: true }, // Usually disabled in dev
        
        // Add custom environments as needed
        preview: { enabled: true, debug: true },
        testing: { enabled: false, debug: true },
    },
    
    // Global override
    globalEnabled: process.env.NEXT_PUBLIC_ANALYTICS_ENABLED !== 'false',
    
    // Trackable environments (defaults to production and staging)
    trackableEnvironments: process.env.NEXT_PUBLIC_TRACKABLE_ENVIRONMENTS?.split(',') || ['production', 'staging'],
    
    // Trackable build IDs (optional)
    trackableBuildIds: process.env.NEXT_PUBLIC_TRACKABLE_BUILD_IDS?.split(','),
};
```

### Method 2: Environment Variables

You can also control analytics using environment variables:

```bash
# Global toggle
NEXT_PUBLIC_ANALYTICS_ENABLED=true

# Specific environments to track
NEXT_PUBLIC_TRACKABLE_ENVIRONMENTS=production,staging,preview

# Specific build IDs to track (supports wildcards)
NEXT_PUBLIC_TRACKABLE_BUILD_IDS=main-*,release-*
```

## Common Configurations

### Enable Analytics for Production Only
```typescript
environments: {
    production: { enabled: true, debug: false },
    staging: { enabled: false, debug: true },
    dev: { enabled: false, debug: true },
}
```

### Enable Analytics for Production and Staging
```typescript
environments: {
    production: { enabled: true, debug: false },
    staging: { enabled: true, debug: true },
    dev: { enabled: false, debug: true },
}
```

### Enable Analytics for All Environments (Development Mode)
```typescript
environments: {
    production: { enabled: true, debug: false },
    staging: { enabled: true, debug: true },
    dev: { enabled: true, debug: true },
}
```

## Helper Functions

Use these functions to check analytics status programmatically:

```typescript
import { 
    isAnalyticsEnabled, 
    isAnalyticsEnabledForEnv, 
    getAnalyticsConfig 
} from '@/lib/services/analytics';

// Check if analytics is enabled for current environment
if (isAnalyticsEnabled()) {
    console.log('Analytics is enabled');
}

// Check if analytics is enabled for a specific environment
if (isAnalyticsEnabledForEnv('production')) {
    console.log('Analytics is enabled for production');
}

// Get full configuration for debugging
console.log(getAnalyticsConfig());
```

## Environment Priority

Analytics is enabled when ALL of these conditions are met:

1. **Global enabled** - `NEXT_PUBLIC_ANALYTICS_ENABLED` is not set to `'false'`
2. **Environment enabled** - The current environment has `enabled: true` in the config
3. **Build trackable** - The current environment/build ID is in the trackable lists

## Debugging

To debug analytics configuration, use:

```typescript
import { getAnalyticsConfig } from '@/lib/services/analytics';

console.log('Analytics Config:', getAnalyticsConfig());
```

This will show you:
- Current environment settings
- Global enabled status
- Trackable environments and build IDs
- Whether analytics is currently enabled

## Best Practices

1. **Production**: Always enable analytics with `debug: false`
2. **Staging**: Enable analytics with `debug: true` for testing
3. **Development**: Usually disable analytics to avoid noise, but enable `debug: true` when testing
4. **Use environment variables** for CI/CD overrides
5. **Test thoroughly** in staging before deploying to production

## Migration from Old System

If you're migrating from the old environment variable-only system:

1. The new system is backward compatible
2. Environment variables still work as overrides
3. You can gradually move to the centralized config
4. Old behavior is preserved if you don't change anything 