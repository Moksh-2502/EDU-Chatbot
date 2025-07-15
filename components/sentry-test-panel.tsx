'use client';

import { useEffect, useState } from 'react';
import * as Sentry from '@sentry/nextjs';
import * as analytics from '@/lib/services/analytics';

// Custom error classes for better error identification
class SentryTestFrontendError extends Error {
    constructor(message: string | undefined) {
        super(message);
        this.name = "SentryTestFrontendError";
    }
}

class SentryTestBackendError extends Error {
    constructor(message: string | undefined) {
        super(message);
        this.name = "SentryTestBackendError";
    }
}

export function SentryTestPanel() {
    const [isVisible, setIsVisible] = useState(false);
    const [sentryConfig, setSentryConfig] = useState<any>(null);
    const [isConnected, setIsConnected] = useState(true);
    const [testStatus, setTestStatus] = useState<string | null>(null);

    useEffect(() => {
        // Check if test_sentry=true is in URL
        const urlParams = new URLSearchParams(window.location.search);
        setIsVisible(urlParams.get('test_sentry') === 'true');

        // Get Sentry configuration info for debugging
        if (urlParams.get('test_sentry') === 'true') {
            setSentryConfig({
                dsn: process.env.NEXT_PUBLIC_SENTRY_DSN ? '✅ Set' : '❌ Missing',
                dsnValue: process.env.NEXT_PUBLIC_SENTRY_DSN ?
                    `${process.env.NEXT_PUBLIC_SENTRY_DSN.substring(0, 20)}...` : 'Not set',
                enabled: process.env.NEXT_PUBLIC_SENTRY_ENABLED !== 'false' ? '✅ Enabled' : '❌ Disabled',
                environment: process.env.NEXT_PUBLIC_SENTRY_ENVIRONMENT || 'dev',
                buildId: process.env.NEXT_PUBLIC_BUILD_ID || 'dev',
            });

            // Check Sentry connectivity
            checkSentryConnectivity();
        }
    }, []);

    const checkSentryConnectivity = async () => {
        try {
            const result = await Sentry.diagnoseSdkConnectivity();
            setIsConnected(result !== 'sentry-unreachable');
            console.log('[Sentry Test] Connectivity check:', result);
        } catch (error) {
            console.error('[Sentry Test] Connectivity check failed:', error);
            setIsConnected(false);
        }
    };

    const handleCapturedException = () => {
        try {
            console.log('[Sentry Test] Sending captured exception...');

            // Create a test error
            const testError = new Error('Test captured exception from Sentry test panel');
            testError.stack = `Error: Test captured exception from Sentry test panel
    at handleCapturedError (SentryTestPanel.tsx:23:31)
    at onClick (SentryTestPanel.tsx:45:15)`;

            // Capture it manually with Sentry
            const eventId = Sentry.captureException(testError, {
                tags: {
                    test_type: 'manual_capture',
                    component: 'SentryTestPanel',
                },
                extra: {
                    test_timestamp: new Date().toISOString(),
                    test_description: 'Manual exception capture test',
                },
            });

            console.log('[Sentry Test] Exception captured with ID:', eventId);

            // Also track in analytics
            analytics.trackError(testError, {
                component: 'SentryTestPanel',
                test_type: 'manual_capture',
            });

            alert(`✅ Captured exception sent to Sentry!\nEvent ID: ${eventId}\nCheck your Sentry dashboard.`);
        } catch (error) {
            console.error('Error sending captured exception:', error);
            alert(`❌ Error sending to Sentry: ${error}`);
        }
    };

    const handleUncaughtException = () => {
        console.log('[Sentry Test] Throwing uncaught exception...');
        // This will be caught by error boundaries and sent to Sentry
        setTimeout(() => {
            throw new SentryTestFrontendError('Test uncaught exception from Sentry test panel - this should be caught by error boundaries');
        }, 100);
    };

    const handleFrontendBackendTest = async () => {
        console.log('[Sentry Test] Running frontend/backend error test...');
        try {
            setTestStatus('🔄 Running frontend/backend test...');

            // Create a span to track the operation
            await Sentry.startSpan({
                name: 'SentryTestPanel Frontend/Backend Test',
                op: 'test'
            }, async () => {
                // First, test the backend API
                try {
                    const res = await fetch('/api/sentry-test-api');
                    if (!res.ok) {
                        console.log('[Sentry Test] Backend error sent successfully');
                    }
                } catch (error) {
                    console.log('[Sentry Test] Backend error caught:', error);
                }
            });

            // Then throw a frontend error
            throw new SentryTestFrontendError('Frontend error from combined frontend/backend test');

        } catch (error) {
            console.log('[Sentry Test] Frontend error sent successfully');
            setTestStatus('✅ Frontend/Backend test completed!');

            // Clear status after 3 seconds
            setTimeout(() => setTestStatus(null), 3000);
        }
    };

    const handleAnalyticsTest = () => {
        console.log('[Analytics Test] Sending test event...');
        // Test analytics tracking
        analytics.trackEvent({
            eventName: 'sentry_test_analytics',
            test_type: 'manual_analytics_test',
            timestamp: Date.now(),
        });

        alert('✅ Analytics test event sent! Check your Mixpanel dashboard.');
    };

    const handleSentryHealthCheck = () => {
        // Try to capture a simple message
        const eventId = Sentry.captureMessage('Sentry health check from test panel', 'info');
        console.log('[Sentry Test] Health check message sent with ID:', eventId);
        alert(`📋 Sentry health check sent!\nEvent ID: ${eventId}\nCheck console for details.`);
    };

    if (!isVisible) return null;

    return (
        <div className="fixed top-4 right-4 z-50 bg-red-100 dark:bg-red-900 border-2 border-red-500 rounded-lg p-4 max-w-sm">
            <div className="text-red-800 dark:text-red-200 font-bold text-sm mb-2">
                🧪 Sentry Test Panel
            </div>
            <div className="text-red-700 dark:text-red-300 text-xs mb-3">
                Remove ?test_sentry=true from URL to hide
            </div>

            {/* Configuration Status */}
            {sentryConfig && (
                <div className="bg-red-50 dark:bg-red-800 p-2 rounded text-xs mb-3">
                    <div className="font-semibold mb-1">🔧 Configuration:</div>
                    <div>DSN: {sentryConfig.dsn}</div>
                    <div>Enabled: {sentryConfig.enabled}</div>
                    <div>Env: {sentryConfig.environment}</div>
                    <div>Build: {sentryConfig.buildId}</div>
                    <div>Connected: {isConnected ? '✅ Yes' : '❌ No (blocked?)'}</div>
                    {sentryConfig.dsn === '✅ Set' && (
                        <div className="text-xs opacity-75 mt-1">DSN: {sentryConfig.dsnValue}</div>
                    )}
                </div>
            )}

            {/* Test Status */}
            {testStatus && (
                <div className="bg-blue-50 dark:bg-blue-800 p-2 rounded text-xs mb-3">
                    <div className="font-semibold">📊 Test Status:</div>
                    <div>{testStatus}</div>
                </div>
            )}

            {/* Connectivity Warning */}
            {!isConnected && (
                <div className="bg-yellow-50 dark:bg-yellow-800 p-2 rounded text-xs mb-3">
                    <div className="font-semibold text-yellow-800 dark:text-yellow-200">⚠️ Connectivity Issue:</div>
                    <div className="text-yellow-700 dark:text-yellow-300">
                        Network requests to Sentry are being blocked. Try disabling your ad-blocker.
                    </div>
                </div>
            )}

            <div className="space-y-2">
                <button
                    type="button"
                    onClick={checkSentryConnectivity}
                    className="w-full px-3 py-2 bg-green-500 text-white text-sm rounded hover:bg-green-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                    🔄 Check Connectivity
                </button>

                <button
                    type="button"
                    onClick={handleSentryHealthCheck}
                    disabled={!isConnected}
                    className="w-full px-3 py-2 bg-green-500 text-white text-sm rounded hover:bg-green-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                    🏥 Sentry Health Check
                </button>

                <button
                    type="button"
                    onClick={handleFrontendBackendTest}
                    disabled={!isConnected}
                    className="w-full px-3 py-2 bg-purple-500 text-white text-sm rounded hover:bg-purple-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                    🔀 Frontend/Backend Test
                </button>

                <button
                    type="button"
                    onClick={handleCapturedException}
                    disabled={!isConnected}
                    className="w-full px-3 py-2 bg-orange-500 text-white text-sm rounded hover:bg-orange-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                    📤 Send Captured Exception
                </button>

                <button
                    type="button"
                    onClick={handleUncaughtException}
                    disabled={!isConnected}
                    className="w-full px-3 py-2 bg-red-500 text-white text-sm rounded hover:bg-red-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                    💥 Throw Uncaught Exception
                </button>

                <button
                    type="button"
                    onClick={handleAnalyticsTest}
                    className="w-full px-3 py-2 bg-blue-500 text-white text-sm rounded hover:bg-blue-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                    📊 Test Analytics Event
                </button>
            </div>

            <div className="text-red-600 dark:text-red-400 text-xs mt-3">
                <div>Check browser console for debug logs</div>
            </div>
        </div>
    );
}
