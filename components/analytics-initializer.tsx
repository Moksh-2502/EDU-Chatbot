'use client';

import { useAnalyticsSession } from '@/context/analytics-context';

/**
 * Client-side analytics initialization component
 * This component handles the analytics session setup and must run on the client
 */
export function AnalyticsInitializer() {
    useAnalyticsSession();
    return null;
} 