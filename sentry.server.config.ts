// This file configures the initialization of Sentry on the server side
// https://docs.sentry.io/platforms/javascript/guides/nextjs/

import * as Sentry from '@sentry/nextjs';
import { getBuildConfig } from '@/lib/services/build-config';

// Get build configuration
const buildConfig = getBuildConfig();

// Environment variables (server-side can access any env vars)
const SENTRY_DSN = process.env.NEXT_PUBLIC_SENTRY_DSN || process.env.SENTRY_DSN;
// Check both SENTRY_ENABLED and NEXT_PUBLIC_SENTRY_ENABLED for local development flexibility
const SENTRY_ENABLED = process.env.SENTRY_ENABLED !== 'false' && process.env.NEXT_PUBLIC_SENTRY_ENABLED !== 'false';

Sentry.init({
    dsn: SENTRY_DSN,
    environment: buildConfig.environment,
    enabled: SENTRY_ENABLED && !!SENTRY_DSN,

    debug: SENTRY_ENABLED && buildConfig.isDevelopment,

    // Performance monitoring
    tracesSampleRate: buildConfig.isProduction ? 0.05 : 1.0,

    // Initial scope for server-side
    initialScope: {
        tags: {
            environment: buildConfig.environment,
            build_id: buildConfig.buildId,
            platform: 'server',
        },
        contexts: {
            build: {
                environment: buildConfig.environment,
                buildId: buildConfig.buildId,
                isDevelopment: buildConfig.isDevelopment,
                isStaging: buildConfig.isStaging,
                isProduction: buildConfig.isProduction,
            },
        },
    },
}); 