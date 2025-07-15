'use client';

import {
  type UnityBridgeWithStorageOptions,
  useUnityBridge,
} from '@/hooks/useUnityBridge';
import type { UnityBridgeOptions } from '@/lib/types/unity-bridge';
import { ApiReactStorageService } from '@/lib/types/unity-storage';
import { Unity } from 'react-unity-webgl';
import { useState, useEffect, useMemo, useRef } from 'react';
import { useSession, signOut } from 'next-auth/react';
import { createUserDataFromSession, validateUserAuthentication } from '@/lib/unity-session-utils';
import * as analytics from '@/lib/services/analytics';
import { ErrorBoundary } from '@/components/error-boundary';

interface UnityGameWrapperProps {
  gameConfig: UnityBridgeOptions['gameConfig'];
  className?: string;
  onGameLoaded?: () => void;
  onMessage?: (message: any) => void;
  onError?: (error: Error) => void;
  enableLogging?: boolean;
}

export function UnityGameWrapper({
  gameConfig,
  className = 'size-full border-0',
  onGameLoaded,
  onMessage,
  onError,
  enableLogging = process.env.NODE_ENV === 'development',
}: UnityGameWrapperProps) {
  // Get session data automatically
  const { data: session, status } = useSession();
  const gameLoadStartTime = useRef<number | null>(null);
  const hasTrackedLoadingStart = useRef(false);
  const hasTrackedLoadingSuccess = useRef(false);
  const hasTrackedLoadingFailure = useRef(false);

  // Dynamic device pixel ratio for crisp rendering
  const [devicePixelRatio, setDevicePixelRatio] = useState(
    typeof window !== 'undefined' ? window.devicePixelRatio : 1,
  );

  // Memoize event handlers to prevent unnecessary re-initialization
  const events = useMemo(() => ({
    onGameLoaded: () => {
      if (hasTrackedLoadingSuccess.current) return;
      hasTrackedLoadingSuccess.current = true;

      console.log('[Unity Game] Game loaded');

      // Track successful game loading
      const loadingTime = gameLoadStartTime.current
        ? Date.now() - gameLoadStartTime.current
        : undefined;

      analytics.trackEvent({
        eventName: 'game_loading_success',
        gameType: 'unity',
        gamePath: gameConfig.loaderUrl || gameConfig.dataUrl || 'unknown',
        loadingTimeMs: loadingTime || 0,
      });

      onGameLoaded?.();
    },
    onMessage: (message: any) => {
      console.log('[Unity Game] Message received:', message);
      onMessage?.(message);
    },
    onError: (error: Error) => {
      // Track Unity-specific errors
      analytics.trackError(error, {
        component: 'UnityGameWrapper',
        gameType: 'unity',
        gamePath: gameConfig.loaderUrl || gameConfig.dataUrl,
      });

      analytics.trackEvent({
        eventName: 'unity_error',
        errorType: 'runtime_error',
        errorMessage: error.message,
        errorStack: error.stack,
        gamePath: gameConfig.loaderUrl || gameConfig.dataUrl || 'unknown',
      });

      onError?.(error);
    },
  }), [onGameLoaded, onMessage, onError, gameConfig]);

  // Memoize bridgeOptions to prevent unnecessary re-initialization
  const bridgeOptions: UnityBridgeWithStorageOptions = useMemo(() => ({
    gameConfig,
    events,
    enableLogging,
    storageService: new ApiReactStorageService(),
    gracefulDegradation: true,
  }), [gameConfig, events, enableLogging]);

  const { unityProvider, bridgeState, sendSessionData } =
    useUnityBridge(bridgeOptions);

  // Track game loading start
  useEffect(() => {
    if (bridgeState.isLoading && !hasTrackedLoadingStart.current) {
      gameLoadStartTime.current = Date.now();
      hasTrackedLoadingStart.current = true;

      analytics.trackEvent({
        eventName: 'game_loading_started',
        gameType: 'unity',
        gamePath: gameConfig.loaderUrl || gameConfig.dataUrl || 'unknown',
      });
    }
  }, [bridgeState.isLoading, gameConfig]);

  // Track game loading failure
  useEffect(() => {
    if (bridgeState.error && !hasTrackedLoadingFailure.current) {
      hasTrackedLoadingFailure.current = true;

      const loadingTime = gameLoadStartTime.current
        ? Date.now() - gameLoadStartTime.current
        : undefined;

      analytics.trackEvent({
        eventName: 'game_loading_failure',
        gameType: 'unity',
        gamePath: gameConfig.loaderUrl || gameConfig.dataUrl || 'unknown',
        errorMessage: bridgeState.error,
        loadingTimeMs: loadingTime,
      });
    }
  }, [bridgeState.error, gameConfig]);

  // Automatically send session data when Unity is ready
  useEffect(() => {
    // Wait for session loading to complete
    if (status === 'loading') {
      console.log('[Unity Game] Waiting for session to load...');
      return;
    }

    if (!bridgeState.isUnityReady) {
      return;
    }

    // Try to get user data from session
    const userData = createUserDataFromSession(session);

    if (userData) {
      // Validate the user data before sending to Unity
      const validation = validateUserAuthentication(userData);

      if (validation.isValid) {
        console.log('[Unity Game] Unity is ready - sending authenticated session data');
        console.log('[Unity Game] Session object:', {
          hasCognitoIdToken: !!session?.cognitoIdToken,
          sessionStatus: status,
          userEmail: session?.user?.email
        });
        console.log('[Unity Game] UserData object:', {
          hasCognitoIdToken: !!userData.cognitoIdToken,
          userId: userData.id,
          userType: userData.type
        });
        sendSessionData(userData);
      } else {
        console.error('[Unity Game] User data validation failed:', validation.reason);
        console.error('[Unity Game] Cannot send session data to Unity. User may need to re-authenticate.');

        // Optionally trigger re-authentication or show an error to the user
        if (validation.reason?.includes('Cognito token expired')) {
          console.log('[Unity Game] Cognito ID token expired. User should re-authenticate.');
          // You could trigger a sign-out and redirect to login here
          // signOut({ callbackUrl: '/login' });
        }
      }
    } else {
      console.warn('[Unity Game] Unity is ready but no authenticated session and guest mode disabled');
    }
  }, [bridgeState.isUnityReady, session, status, sendSessionData]);

  // Handle refresh token errors
  useEffect(() => {
    if (session?.error === 'RefreshTokenError') {
      console.log('[Unity Game] Refresh token error detected. Signing out user...');
      signOut({ callbackUrl: '/login' });
    }
  }, [session]);

  useEffect(() => {
    if (typeof window === 'undefined') return;

    const updateDevicePixelRatio = () => {
      setDevicePixelRatio(window.devicePixelRatio);
    };

    const mediaMatcher = window.matchMedia(
      `screen and (resolution: ${devicePixelRatio}dppx)`,
    );

    mediaMatcher.addEventListener('change', updateDevicePixelRatio);

    return () => {
      mediaMatcher.removeEventListener('change', updateDevicePixelRatio);
    };
  }, [devicePixelRatio]);

  return (
    <ErrorBoundary
      context={{
        component: 'UnityGameWrapper',
        gameType: 'unity',
        gamePath: gameConfig.loaderUrl || gameConfig.dataUrl
      }}
    >
      <div className="flex flex-col h-full relative">
        {bridgeState.isLoading && (
          <div className="absolute inset-0 flex items-center justify-center z-10 bg-background/80">
            <div className="flex flex-col items-center gap-4 w-full max-w-sm px-6">
              <div className="text-lg font-semibold">Loading Game</div>
              <div className="w-full bg-secondary rounded-full h-2 overflow-hidden">
                <div
                  className="h-full bg-primary transition-all duration-300 ease-out"
                  style={{ width: `${bridgeState.loadingProgress}%` }}
                />
              </div>
              <div className="text-sm text-muted-foreground">
                {bridgeState.loadingProgress}%
              </div>
            </div>
          </div>
        )}

        {bridgeState.error && (
          <div className="absolute inset-0 flex items-center justify-center z-10 bg-background/80">
            <div className="flex flex-col items-center gap-4 text-center max-w-md p-6">
              <div className="text-destructive text-lg font-semibold">
                Failed to Load Game
              </div>
              <div className="text-sm text-muted-foreground">
                {bridgeState.error}
              </div>
              <button
                type="button"
                onClick={() => window.location.reload()}
                className="px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90"
              >
                Retry
              </button>
            </div>
          </div>
        )}

        <Unity
          unityProvider={unityProvider}
          className={className}
          style={{
            display: bridgeState.isLoaded ? 'block' : 'none',
          }}
          devicePixelRatio={devicePixelRatio}
          tabIndex={-1}
        />
      </div>
    </ErrorBoundary>
  );
}
