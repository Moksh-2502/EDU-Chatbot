import * as Sentry from '@sentry/nextjs';
import type { AnalyticsEvent, AnalyticsUser } from '@/lib/types/analytics';
import { getBuildConfig, isBuildTrackable } from './build-config';

// Centralized analytics configuration
const ANALYTICS_CONFIG = {
    // Environment-specific settings
    environments: {
        production: { enabled: true, debug: false },
        staging: { enabled: true, debug: true },
        dev: { enabled: false, debug: true }, // Usually disabled in dev
    },

    // Global override (can be set via environment variable)
    globalEnabled: process.env.NEXT_PUBLIC_ANALYTICS_ENABLED !== 'false',

    // Trackable environments (fallback to environment variable)
    trackableEnvironments: process.env.NEXT_PUBLIC_TRACKABLE_ENVIRONMENTS?.split(',') || ['production', 'staging'],

    // Trackable build IDs (fallback to environment variable)
    trackableBuildIds: process.env.NEXT_PUBLIC_TRACKABLE_BUILD_IDS?.split(','),
};

// Dynamic import for mixpanel to avoid SSR issues
let mixpanel: any = null;

// Simple module-level config (created once on import)
const buildConfig = getBuildConfig();

// Determine if analytics is enabled for current environment
const isAnalyticsEnabledForEnvironment = (): boolean => {
    const envConfig = ANALYTICS_CONFIG.environments[buildConfig.environment as keyof typeof ANALYTICS_CONFIG.environments];
    return envConfig?.enabled ?? false;
};

const isEnabled = ANALYTICS_CONFIG.globalEnabled &&
    isAnalyticsEnabledForEnvironment() &&
    isBuildTrackable(
        ANALYTICS_CONFIG.trackableEnvironments,
        ANALYTICS_CONFIG.trackableBuildIds
    );

// Initialize only on client side
if (isEnabled && typeof window !== 'undefined') {
    initializeServices();
}

/**
 * Track analytics event
 */
export function trackEvent(event: AnalyticsEvent): void {
    if (!isEnabled || typeof window === 'undefined') return;

    // Track in Mixpanel
    if (mixpanel) {
        mixpanel.track(event.eventName, {
            ...event,
            timestamp: event.timestamp || Date.now(),
        });
    }

    // Add breadcrumb to Sentry
    Sentry.addBreadcrumb({
        message: event.eventName,
        level: 'info',
        data: event,
        category: 'analytics',
    });
}

/**
 * Track error with context
 */
export function trackError(error: Error, context?: Record<string, any>): void {
    if (!isEnabled) return;

    // Track in Sentry
    Sentry.captureException(error, {
        extra: context,
        tags: {
            environment: buildConfig.environment,
            build_id: buildConfig.buildId,
        },
    });

    // Track as event in Mixpanel (only on client side)
    if (mixpanel && typeof window !== 'undefined') {
        mixpanel.track('error_occurred', {
            error_message: error.message,
            error_stack: error.stack,
            ...context,
        });
    }
}

/**
 * Identify user
 */
export function identifyUser(user: AnalyticsUser): void {
    if (!isEnabled || typeof window === 'undefined') return;

    // Set user in Sentry
    Sentry.setUser({
        id: user.id,
        email: user.email,
        username: user.name,
    });

    // Identify in Mixpanel
    if (mixpanel) {
        mixpanel.identify(user.id);
        mixpanel.people.set({
            $email: user.email,
            $name: user.name,
            user_type: user.type,
        });
    }
}

/**
 * Set global context for all future events
 */
export function setGlobalContext(context: Record<string, any>): void {
    if (!isEnabled) return;

    // Set in Sentry
    Sentry.setContext('global', context);

    // Set in Mixpanel as super properties (only on client side)
    if (mixpanel && typeof window !== 'undefined') {
        mixpanel.register(context);
    }
}

/**
 * Check if analytics is enabled for the current environment
 */
export function isAnalyticsEnabled(): boolean {
    return isEnabled;
}

/**
 * Check if analytics is enabled for a specific environment
 */
export function isAnalyticsEnabledForEnv(environment: string): boolean {
    const envConfig = ANALYTICS_CONFIG.environments[environment as keyof typeof ANALYTICS_CONFIG.environments];
    return ANALYTICS_CONFIG.globalEnabled && (envConfig?.enabled ?? false);
}

/**
 * Get current analytics configuration (for debugging)
 */
export function getAnalyticsConfig(): typeof ANALYTICS_CONFIG & { currentlyEnabled: boolean } {
    return {
        ...ANALYTICS_CONFIG,
        currentlyEnabled: isEnabled,
    };
}

// Internal initialization
async function initializeServices(): Promise<void> {
    // Only initialize on client side
    if (typeof window === 'undefined') return;

    const token = process.env.NEXT_PUBLIC_MIXPANEL_TOKEN;

    if (token) {
        // Dynamic import to avoid SSR issues
        const mixpanelModule = await import('mixpanel-browser');
        mixpanel = mixpanelModule.default || mixpanelModule;

        // Get debug setting from environment config
        const envConfig = ANALYTICS_CONFIG.environments[buildConfig.environment as keyof typeof ANALYTICS_CONFIG.environments];
        const debugMode = envConfig?.debug ?? buildConfig.isDevelopment;

        mixpanel.init(token, {
            debug: debugMode,
        });

        // Set basic super properties
        mixpanel.register({
            environment: buildConfig.environment,
            build_id: buildConfig.buildId,
        });
    }

    if (buildConfig.isDevelopment) {
        console.log('[Analytics] Initialized for', buildConfig.environment);
    }
} 