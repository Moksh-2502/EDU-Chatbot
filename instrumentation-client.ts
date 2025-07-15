// This file configures CLIENT-SPECIFIC Sentry features for Next.js
// https://docs.sentry.io/platforms/javascript/guides/nextjs/

import * as Sentry from "@sentry/nextjs";
import { getBuildConfig } from '@/lib/services/build-config';

// Get build configuration
const buildConfig = getBuildConfig();

// Environment variables
const SENTRY_DSN = process.env.NEXT_PUBLIC_SENTRY_DSN;
// Check both SENTRY_ENABLED and NEXT_PUBLIC_SENTRY_ENABLED for local development flexibility
const SENTRY_ENABLED = process.env.SENTRY_ENABLED !== 'false' && process.env.NEXT_PUBLIC_SENTRY_ENABLED !== 'false';

Sentry.init({
  dsn: SENTRY_DSN,
  environment: buildConfig.environment,
  enabled: SENTRY_ENABLED && !!SENTRY_DSN,
  debug: SENTRY_ENABLED && buildConfig.isDevelopment,

  // Performance monitoring
  tracesSampleRate: buildConfig.isProduction ? 0.05 : 1.0,

  // Session replay settings
  replaysSessionSampleRate: buildConfig.isProduction ? 0.01 : 0.1,
  replaysOnErrorSampleRate: 1.0,

  // Client-specific integrations
  integrations: [
    Sentry.replayIntegration({
      // Don't record input events for privacy
      maskAllInputs: true,
      blockAllMedia: true,
    }),
  ],

  // Initial scope for client-side
  initialScope: {
    tags: {
      environment: buildConfig.environment,
      build_id: buildConfig.buildId,
      platform: 'browser',
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

// Export router transition capturing
export const onRouterTransitionStart = Sentry.captureRouterTransitionStart;