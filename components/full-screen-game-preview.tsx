'use client';

import { useEffect, useRef, useState } from 'react';
import Image from 'next/image';
import { UnityGameWrapper } from './unity-game-wrapper';
import { detectUnityGameFromHtml, } from '@/lib/unity-communication';
import { giveFeedback, handleFeedbackMessage } from '@/lib/feedback-service';
import * as analytics from '@/lib/services/analytics';
import { ErrorBoundary } from '@/components/error-boundary';

type FullScreenGamePreviewProps =
  | {
    gameId: string;
  }
  | {
    skeleton: string;
  };

export function FullScreenGamePreview(props: FullScreenGamePreviewProps) {
  const [isLoading, setIsLoading] = useState(true);
  const [isUnityGame, setIsUnityGame] = useState(false);
  const [unityConfig, setUnityConfig] = useState<any>(null);
  const [containerHeight, setContainerHeight] = useState<string>('100svh');
  const [isUnityGameReady, setIsUnityGameReady] = useState(false);
  const iframeRef = useRef<HTMLIFrameElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const gameLoadStartTime = useRef<number | null>(null);
  const hasTrackedGameDetection = useRef(false);

  let gamePath = '';

  if ('gameId' in props) {
    gamePath = `/api/generated-games/${props.gameId}/index.html`;
  } else {
    gamePath = `/api/game-skeletons/${props.skeleton}/index.html`;
  }

  // Handle dynamic viewport height with best available method
  useEffect(() => {
    const updateViewportHeight = () => {
      // 1. Use window.visualViewport.height if available
      if (typeof window !== 'undefined' && window.visualViewport) {
        setContainerHeight(`${window.visualViewport.height}px`);
        return;
      }
      // 2. Use 100svh if supported
      const testDiv = document.createElement('div');
      testDiv.style.height = '100svh';
      if (testDiv.style.height === '100svh') {
        setContainerHeight('100svh');
        return;
      }
      // 3. Fallback to window.innerHeight
      setContainerHeight(`${window.innerHeight}px`);
    };

    updateViewportHeight();

    // Listen for viewport changes
    const handleResize = () => {
      updateViewportHeight();
    };
    const handleOrientationChange = () => {
      setTimeout(updateViewportHeight, 100);
    };

    window.addEventListener('resize', handleResize);
    window.addEventListener('orientationchange', handleOrientationChange);
    if (window.visualViewport) {
      window.visualViewport.addEventListener('resize', updateViewportHeight);
    }

    return () => {
      window.removeEventListener('resize', handleResize);
      window.removeEventListener('orientationchange', handleOrientationChange);
      if (window.visualViewport) {
        window.visualViewport.removeEventListener('resize', updateViewportHeight);
      }
    };
  }, []);

  // Check if this is a Unity game by analyzing the HTML content
  useEffect(() => {
    if (hasTrackedGameDetection.current) return;

    const checkForUnityGame = async () => {
      try {
        console.log('[Game Preview] Checking if game is Unity:', gamePath);
        hasTrackedGameDetection.current = true;
        gameLoadStartTime.current = Date.now();
        setIsUnityGameReady(false); // Reset Unity game ready state

        // Track game type detection (not loading - Unity wrapper will handle that)
        analytics.trackEvent({
          eventName: 'game_detection_started',
          gamePath: gamePath,
        });

        const detection = await detectUnityGameFromHtml(gamePath);

        if (detection.isUnity) {
          console.log('[Game Preview] Unity game detected with config:', detection.config);
          setIsUnityGame(true);
          setUnityConfig(detection.config);

          analytics.trackEvent({
            eventName: 'game_detection_success',
            gameType: 'unity',
            gamePath: gamePath,
          });
        } else {
          console.log('[Game Preview] Not a Unity game, falling back to iframe');
          setIsUnityGame(false);
          setUnityConfig(null);

          analytics.trackEvent({
            eventName: 'game_detection_success',
            gameType: 'iframe',
            gamePath: gamePath,
          });
        }
      } catch (error) {
        console.error('[Game Preview] Error detecting Unity game:', error);
        console.log('[Game Preview] Falling back to iframe due to error');

        // Track the error
        analytics.trackError(error as Error, {
          component: 'FullScreenGamePreview',
          operation: 'unity_detection',
          gamePath: gamePath,
        });

        analytics.trackEvent({
          eventName: 'game_detection_failure',
          gamePath: gamePath,
          errorMessage: (error as Error).message,
        });

        setIsUnityGame(false);
        setUnityConfig(null);
      }
    };

    checkForUnityGame();
  }, [gamePath]);

  // Handle iframe load for non-Unity games
  useEffect(() => {
    if (isUnityGame) return;

    setIsLoading(true);
    const iframeElement = iframeRef.current;

    const handleIframeLoad = () => {
      setIsLoading(false);

      // Track successful iframe loading
      const loadingTime = gameLoadStartTime.current
        ? Date.now() - gameLoadStartTime.current
        : 0;

      analytics.trackEvent({
        eventName: 'game_loading_success',
        gameType: 'iframe',
        gamePath: gamePath,
        loadingTimeMs: loadingTime,
      });
    };

    const handleIframeError = () => {
      setIsLoading(false);

      // Track iframe loading failure
      const loadingTime = gameLoadStartTime.current
        ? Date.now() - gameLoadStartTime.current
        : 0;

      analytics.trackEvent({
        eventName: 'iframe_error',
        errorType: 'loading_error',
        errorMessage: 'Failed to load iframe game',
        gamePath: gamePath,
      });

      analytics.trackEvent({
        eventName: 'game_loading_failure',
        gameType: 'iframe',
        gamePath: gamePath,
        errorMessage: 'Failed to load iframe game',
        loadingTimeMs: loadingTime,
      });
    };

    if (iframeElement) {
      iframeElement.addEventListener('load', handleIframeLoad);
      iframeElement.addEventListener('error', handleIframeError);
    }

    return () => {
      if (iframeElement) {
        iframeElement.removeEventListener('load', handleIframeLoad);
        iframeElement.removeEventListener('error', handleIframeError);
      }
    };
  }, [gamePath, isUnityGame]);

  const handleUnityGameLoaded = () => {
    setIsLoading(false);
    setIsUnityGameReady(true);
    console.log('[Game Preview] Unity game loaded successfully');
  };

  const handleUnityGameEvent = (message: any) => {
    console.log('[Game Preview] Unity message:', message);

    // Handle feedback messages from Unity
    if (message.messageType === 'giveFeedback') {
      handleFeedbackMessage(message);
    }

    // You can add custom handling for other message types here
  };

  const handleUnityError = (error: Error) => {
    console.error('[Game Preview] Unity game error:', error);
    // Optionally fall back to iframe on Unity error
    setIsUnityGame(false);
    setUnityConfig(null);
  };

  const containerStyle = {
    height: containerHeight,
    // Add safe area padding for devices with notches
    paddingTop: 'env(safe-area-inset-top, 0px)',
    paddingBottom: 'env(safe-area-inset-bottom, 0px)',
  };

  if (isUnityGame && unityConfig) {
    return (
      <ErrorBoundary
        context={{
          component: 'FullScreenGamePreview-Unity',
          gameType: 'unity',
          gamePath: gamePath
        }}
      >
        <div
          ref={containerRef}
          className="flex flex-col"
          style={containerStyle}
        >
          <div className="flex-1 overflow-hidden relative">
            <UnityGameWrapper
              gameConfig={unityConfig}
              onGameLoaded={handleUnityGameLoaded}
              onMessage={handleUnityGameEvent}
              onError={handleUnityError}
              enableLogging={process.env.NODE_ENV === 'development'}
            />

            {/* React Feedback Button - Hidden when Unity game is ready (Unity has its own) */}
            {!isUnityGameReady && (
              <div className="absolute right-4 bottom-4 z-20">
                <button
                  type="button"
                  onClick={() => giveFeedback({ source: 'react', origin: 'ui_button' })}
                  className="hover:scale-105 transition-all duration-200 hover:drop-shadow-lg"
                  title="Give Feedback"
                >
                  <Image
                    src="/images/feedback_button.png"
                    alt="Feedback"
                    width={80}
                    height={80}
                    className="w-20 h-auto"
                  />
                </button>
              </div>
            )}
          </div>
        </div>
      </ErrorBoundary>
    );
  }

  // Fallback to iframe for non-Unity games
  return (
    <ErrorBoundary
      context={{
        component: 'FullScreenGamePreview-Iframe',
        gameType: 'iframe',
        gamePath: gamePath
      }}
    >
      <div
        ref={containerRef}
        className="flex flex-col"
        style={containerStyle}
      >
        <div className="flex-1 overflow-hidden relative">
          {isLoading && (
            <div className="absolute inset-0 flex items-center justify-center z-10">
              <div className="flex flex-col items-center gap-4">
                <div className="animate-spin size-8 border-3 border-primary border-t-transparent rounded-full" />
                <div className="text-sm text-muted-foreground">Loading Game...</div>
              </div>
            </div>
          )}
          <iframe
            ref={iframeRef}
            src={gamePath}
            className="size-full border-0"
            title="Game Playground"
            sandbox="allow-scripts allow-same-origin allow-forms"
            allow="accelerometer; camera; encrypted-media; geolocation; gyroscope; microphone; midi; payment; xr-spatial-tracking"
          />

          {/* React Feedback Button for iframe games */}
          <div className="absolute right-4 bottom-4 z-20">
            <button
              type="button"
              onClick={() => giveFeedback({ source: 'react', origin: 'ui_button' })}
              className="hover:scale-105 transition-all duration-200 hover:drop-shadow-lg"
              title="Give Feedback"
            >
              <Image
                src="/images/feedback_button.png"
                alt="Feedback"
                width={80}
                height={80}
                className="w-20 h-auto"
              />
            </button>
          </div>
        </div>
      </div>
    </ErrorBoundary>
  );
}