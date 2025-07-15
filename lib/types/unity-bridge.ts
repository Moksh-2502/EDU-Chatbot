export interface UnityGameConfig {
  loaderUrl: string;
  dataUrl: string;
  frameworkUrl: string;
  codeUrl: string;
  streamingAssetsUrl?: string;
}

/**
 * Base message format for React-Unity communication
 * All messages use this format with messageType inside the payload
 */
export interface ReactGameMessage {
  messageType: string;
  timestamp: number;
  [key: string]: any; // Allow additional properties for specific message types
}

/**
 * Unity Ready Message - sent when Unity finishes loading
 */
export interface UnityReadyMessage extends ReactGameMessage {
  messageType: 'UnityReady';
  status: string;
}

/**
 * Session Data Message - contains user authentication information
 */
export interface SessionDataMessage extends ReactGameMessage {
  messageType: 'SessionData';
  user: any;
  sessionId: string;
}

/**
 * Give Feedback Message - sent when player wants to give feedback
 */
export interface GiveFeedbackMessage extends ReactGameMessage {
  messageType: 'giveFeedback';
  data: FeedbackData;
}

export interface FeedbackData {
  source?: string;
  origin?: string;
  metadata?: Record<string, any> | null;
}

/**
 * Logout Message - sent when player wants to logout from the application
 */
export interface LogoutMessage extends ReactGameMessage {
  messageType: 'logout';
  reason?: string;
}

/**
 * Storage Request Message - for Unity requesting storage operations
 */
export interface StorageRequestMessage extends ReactGameMessage {
  messageType: 'StorageRequest';
  operation: 'load' | 'save' | 'delete' | 'exists';
  correlationId: string;
  userId: string;
  key: string;
  jsonData?: string;
}

/**
 * Storage Response Message - React's response to storage requests
 */
export interface StorageResponseMessage extends ReactGameMessage {
  messageType: 'StorageResponse';
  correlationId: string;
  success: boolean;
  jsonData?: string;
  error?: string;
}

export interface UnityBridgeEvents {
  onGameLoaded?: () => void;
  onMessage?: (message: ReactGameMessage) => void;
  onError?: (error: Error) => void;
  onProgress?: (progress: number) => void;
  onFeedback?: (message: GiveFeedbackMessage) => void;
}

export interface UnityBridgeOptions {
  gameConfig: UnityGameConfig;
  events?: UnityBridgeEvents;
  enableLogging?: boolean;
  gracefulDegradation?: boolean;
}

export interface UnityBridgeState {
  isLoaded: boolean;
  isLoading: boolean;
  loadingProgress: number;
  error: string | null;
  isUnityGame: boolean;
  isUnityReady?: boolean;
}

export interface UnityMessagePayload {
  gameObjectName: string;
  methodName: string;
  parameter?: string | number | boolean;
} 