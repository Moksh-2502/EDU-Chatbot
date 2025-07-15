'use client';

import { useCallback, useEffect, useState, useRef } from 'react';
import { useUnityContext } from 'react-unity-webgl';
import { useRouter } from "next/navigation";
import type {
  UnityBridgeOptions,
  UnityBridgeState,
  ReactGameMessage,
  GiveFeedbackMessage,
  StorageRequestMessage,
  StorageResponseMessage
} from '@/lib/types/unity-bridge';
import type { UserData } from '@/lib/types/user-data';
import type { IReactStorageService } from '@/lib/types/unity-storage';
import { LocalReactStorageService, UnityStorageHandler } from '@/lib/types/unity-storage';
import {
  createSessionDataMessage,
  createReactGameMessage,
  safeStringify,
  logCommunication,
} from '@/lib/unity-communication';
import { handleFeedbackMessage } from '@/lib/feedback-service';
import { logUnityBridge } from '@/lib/unity-debug';

export interface UnityBridgeWithStorageOptions extends UnityBridgeOptions {
  storageService?: IReactStorageService;
  initialUserData?: UserData; // Add initial session data
}

interface QueuedMessage {
  message: ReactGameMessage;
  timestamp: number;
}

export function useUnityBridge(options: UnityBridgeWithStorageOptions) {
  const [bridgeState, setBridgeState] = useState<UnityBridgeState>({
    isLoaded: false,
    isLoading: true,
    loadingProgress: 0,
    error: null,
    isUnityGame: true,
  });

  const [isUnityReady, setIsUnityReady] = useState(false);
  const messageQueue = useRef<QueuedMessage[]>([]);
  const dispatcherSetup = useRef(false);
  const router = useRouter(); // Move useRouter to top level

  // Initialize storage handler
  const storageService = useRef(options.storageService || new LocalReactStorageService());
  const storageHandler = useRef(new UnityStorageHandler(storageService.current));

  const {
    unityProvider,
    sendMessage,
    addEventListener,
    removeEventListener,
    loadingProgression,
    isLoaded,
  } = useUnityContext({
    loaderUrl: options.gameConfig.loaderUrl,
    dataUrl: options.gameConfig.dataUrl,
    frameworkUrl: options.gameConfig.frameworkUrl,
    codeUrl: options.gameConfig.codeUrl,
    streamingAssetsUrl: options.gameConfig.streamingAssetsUrl,
  });

  // Send queued messages to Unity - stable reference with minimal dependencies
  const processMessageQueue = useCallback(() => {
    if (messageQueue.current.length === 0) {
      logUnityBridge('Message queue is empty, skipping processing');
      return;
    }

    logUnityBridge(`Processing ${messageQueue.current.length} queued messages`);

    const messages = [...messageQueue.current]; // Create a copy
    messageQueue.current = []; // Clear the queue

    messages.forEach((queuedMessage) => {
      try {
        sendMessage('ReactBridgeManager', 'ReceiveMessage', safeStringify(queuedMessage.message));
        logCommunication('to-unity', queuedMessage.message, options.enableLogging);
        logUnityBridge(`Sent queued message: ${queuedMessage.message.messageType}`);
      } catch (error) {
        console.error('[Unity Bridge] Error processing queued message:', error);
        // Re-queue the message on error
        messageQueue.current.unshift(queuedMessage);
      }
    });
  }, [sendMessage, options.enableLogging]);

  // Queue a message for Unity
  const enqueueMessage = useCallback((message: ReactGameMessage) => {
    messageQueue.current.push({
      message,
      timestamp: Date.now(),
    });
    logUnityBridge(`Message queued: ${message.messageType} (queue size: ${messageQueue.current.length})`);

    // Check if we can process the queue immediately
    if (isUnityReady && isLoaded) {
      processMessageQueue();
    }
  }, [isUnityReady, isLoaded, processMessageQueue]);

  // Send a message to Unity (always queued)
  const sendMessageToUnity = useCallback(
    (message: ReactGameMessage) => {
      if (!isLoaded) {
        console.warn('[Unity Bridge] Cannot send message: Unity not loaded');
        return false;
      }

      try {
        // Always queue the message, never send directly
        enqueueMessage(message);
        return true;
      } catch (error) {
        console.error('[Unity Bridge] Error queueing message:', error);
        if (options.events?.onError) {
          options.events.onError(error as Error);
        }
        return false;
      }
    },
    [isLoaded, enqueueMessage, options.events]
  );

  // Send session data to Unity (always queued)
  const sendSessionData = useCallback(
    (userData: UserData) => {
      const message = createSessionDataMessage(userData);
      return sendMessageToUnity(message);
    },
    [sendMessageToUnity]
  );

  // Send a custom message to Unity (always queued)
  const sendCustomMessage = useCallback(
    (messageType: string, additionalProperties?: Record<string, any>) => {
      const message = createReactGameMessage(messageType, additionalProperties);
      return sendMessageToUnity(message);
    },
    [sendMessageToUnity]
  );

  // Setup Unity message dispatcher
  useEffect(() => {
    // Prevent multiple setups
    if (dispatcherSetup.current) {
      logUnityBridge('Message dispatcher already set up, skipping');
      return;
    }

    if (typeof window !== 'undefined') {
      logUnityBridge('Setting up message dispatcher');
      dispatcherSetup.current = true;

      // Handle all messages from Unity
      (window as any).dispatchReactUnityMessage = async (messageJson: string) => {
        try {
          const message: ReactGameMessage = JSON.parse(messageJson);

          logCommunication('from-unity', message, options.enableLogging);

          // Handle specific message types
          switch (message.messageType) {
            case 'UnityReady':
              logUnityBridge('Unity is ready to receive messages - processing queue');
              setIsUnityReady(true);
              // Don't process queue here - it will be handled by the useEffect
              break;

            case 'StorageRequest': {
              logUnityBridge('Processing storage request (will queue response)');
              const storageRequest = message as StorageRequestMessage;
              try {
                const response = await storageHandler.current.handleStorageRequest(storageRequest);
                // Use sendMessageToUnity instead of enqueueMessage directly
                sendMessageToUnity(response);
              } catch (error) {
                console.error('[Unity Bridge] Error processing storage request:', error);
                const errorResponse: StorageResponseMessage = {
                  messageType: 'StorageResponse',
                  correlationId: storageRequest.correlationId,
                  success: false,
                  error: error instanceof Error ? error.message : 'Unknown error',
                  timestamp: Date.now()
                };
                // Use sendMessageToUnity instead of enqueueMessage directly
                sendMessageToUnity(errorResponse);
              }
              break;
            }

            case 'giveFeedback': {
              logUnityBridge('Processing feedback message');
              const feedbackMessage = message as GiveFeedbackMessage;
              handleFeedbackMessage(feedbackMessage);
              if (options.events?.onFeedback) {
                options.events.onFeedback(feedbackMessage);
              }
              break;
            }

            case 'logout': {
              logUnityBridge('Processing logout message from Unity');
              const clientId = process.env.NEXT_PUBLIC_COGNITO_CLIENT_ID;
              const logoutUri = (process.env.NEXT_PUBLIC_APP_URL ? `${process.env.NEXT_PUBLIC_APP_URL}/login` : `${window.location.origin}/login`);
              const cognitoDomain = process.env.NEXT_PUBLIC_COGNITO_DOMAIN;

              const fullLogoutUri = `${cognitoDomain}/logout?client_id=${clientId}&logout_uri=${encodeURIComponent(logoutUri)}`;
              console.log('[Unity Bridge] Full logout URI:', fullLogoutUri);
              window.location.href = fullLogoutUri;
              break;
            }

            default:
              logUnityBridge(`Unknown message type: ${message.messageType}`);
              if (options.events?.onMessage) {
                options.events.onMessage(message);
              }
              break;
          }
        } catch (error) {
          console.error('[Unity Bridge] Error handling Unity message:', error);
          if (options.events?.onError) {
            options.events.onError(error as Error);
          }
        }
      };

      logUnityBridge('Message dispatcher is now available');

      return () => {
        logUnityBridge('Cleaning up message dispatcher');
        (window as any).dispatchReactUnityMessage = undefined;
        dispatcherSetup.current = false;
      };
    } else {
      logUnityBridge('Window object not available, message dispatcher setup skipped');
    }
  }, [options.events, options.enableLogging, sendMessageToUnity, router]);

  // Process queued messages when Unity becomes ready or loaded
  useEffect(() => {
    if (isUnityReady && isLoaded) {
      logUnityBridge('Unity is ready and loaded - processing message queue');
      processMessageQueue();
    }
  }, [isUnityReady, isLoaded, processMessageQueue]);

  // Queue initial session data when bridge is initialized
  useEffect(() => {
    if (options.initialUserData) {
      logUnityBridge('Queueing initial session data');
      // Use sendSessionData instead of enqueueMessage directly
      sendSessionData(options.initialUserData);
    }
  }, [options.initialUserData, sendSessionData]);

  // Update bridge state based on Unity context
  useEffect(() => {
    setBridgeState(prevState => ({
      ...prevState,
      isLoaded,
      isLoading: !isLoaded,
      loadingProgress: Math.round(loadingProgression * 100),
      isUnityReady,
    }));
  }, [isLoaded, loadingProgression, isUnityReady]);

  // Handle progress updates
  useEffect(() => {
    if (options.events?.onProgress) {
      options.events.onProgress(loadingProgression);
    }
  }, [loadingProgression, options.events]);

  // Handle game loaded event
  useEffect(() => {
    if (isLoaded && options.events?.onGameLoaded) {
      options.events.onGameLoaded();
    }
  }, [isLoaded, options.events]);

  return {
    unityProvider,
    bridgeState: {
      ...bridgeState,
      isUnityReady,
    },
    sendSessionData,
    sendCustomMessage,
    sendMessageToUnity,
  };
} 