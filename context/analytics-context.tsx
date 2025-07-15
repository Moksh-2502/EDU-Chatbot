'use client';

import { useEffect } from 'react';
import { useSession } from 'next-auth/react';
import * as analytics from '@/lib/services/analytics';

/**
 * Simple hook to handle analytics session integration
 * Call this once in your app root to automatically handle user identification
 */
export function useAnalyticsSession() {
    const { data: session, status } = useSession();

    // Handle session changes
    useEffect(() => {
        if (status === 'loading') return;

        if (session?.user) {
            analytics.identifyUser({
                id: session.user.id || session.user.email || 'unknown',
                email: session.user.email || undefined,
                name: session.user.name || undefined,
                type: determineUserType(session.user),
            });
        }
    }, [session, status]);

    // Track initial session start
    useEffect(() => {
        analytics.trackEvent({
            eventName: 'user_session_start',
            referrer: typeof window !== 'undefined' ? document.referrer : undefined,
            userAgent: typeof window !== 'undefined' ? navigator.userAgent : undefined,
            viewportWidth: typeof window !== 'undefined' ? window.innerWidth : undefined,
            viewportHeight: typeof window !== 'undefined' ? window.innerHeight : undefined,
        });
    }, []);
}

function determineUserType(user: any): 'free' | 'premium' | 'admin' {
    if (user.role === 'admin') return 'admin';
    if (user.plan === 'premium') return 'premium';
    return 'free';
} 