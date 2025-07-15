
import * as Sentry from "@sentry/nextjs";

export const dynamic = "force-dynamic";

class SentryTestBackendError extends Error {
    constructor(message: string | undefined) {
        super(message);
        this.name = "SentryTestBackendError";
    }
}

// A faulty API route to test Sentry's error monitoring
export function GET() {
    console.log('[Sentry Test API] Backend error test triggered');

    // Add some context to the error
    Sentry.setContext('test_context', {
        api_endpoint: '/api/sentry-test-api',
        test_type: 'backend_error_test',
        timestamp: new Date().toISOString(),
    });

    // Throw a test error
    throw new SentryTestBackendError("Test backend error from Sentry test API - this should appear in your Sentry dashboard");
} 