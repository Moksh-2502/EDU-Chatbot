import type { BuildConfig } from '@/lib/types/analytics';

/**
 * Build Configuration Service (Functional)
 * Provides build configuration from CI/CD environment variables
 * Mirrors Unity's build trackability logic without singleton pattern
 */

let cachedBuildConfig: BuildConfig | null = null;

/**
 * Get build configuration from environment variables
 * Uses Unity's environment logic: dev/staging/production
 */
export function getBuildConfig(): BuildConfig {
    if (cachedBuildConfig) {
        return cachedBuildConfig;
    }

    // Use Sentry environment as single source of truth (matches Unity)
    const environment = process.env.NEXT_PUBLIC_SENTRY_ENVIRONMENT || 'dev';

    cachedBuildConfig = {
        environment,
        buildId: process.env.NEXT_PUBLIC_BUILD_ID || 'dev',
        isDevelopment: environment !== 'production', // dev and staging are development environments
        isStaging: environment === 'staging',
        isProduction: environment === 'production',
    };

    return cachedBuildConfig;
}

/**
 * Check if this build should be tracked based on environment and build ID
 * Mirrors Unity's IsBuildTrackable() logic exactly
 */
export function isBuildTrackable(
    trackableEnvironments?: string[] | null,
    trackableBuildIds?: string[] | null,
): boolean {
    const config = getBuildConfig();

    // If no restrictions specified, track everything
    if (!trackableEnvironments && !trackableBuildIds) {
        return true;
    }

    // Check environment restrictions
    if (trackableEnvironments && trackableEnvironments.length > 0) {
        if (!trackableEnvironments.includes(config.environment)) {
            return false;
        }
    }

    // Check build ID restrictions
    if (trackableBuildIds && trackableBuildIds.length > 0) {
        const buildId = config.buildId;
        const isTrackable = trackableBuildIds.some(pattern => {
            if (pattern.endsWith('*')) {
                // Wildcard matching (e.g., "main-*")
                const prefix = pattern.slice(0, -1);
                return buildId.startsWith(prefix);
            }
            // Exact matching
            return buildId === pattern;
        });

        if (!isTrackable) {
            return false;
        }
    }

    return true;
}

/**
 * Get deployment info for debugging
 */
export function getDeploymentInfo(): Record<string, string> {
    const config = getBuildConfig();
    return {
        environment: config.environment,
        buildId: config.buildId,
        timestamp: new Date().toISOString(),
    };
} 