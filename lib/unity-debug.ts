/**
 * Simple Unity Bridge logging utility
 */

/**
 * Simple logging function with console.warn for important messages
 */
export const logUnityBridge = (message: string) => {
  const logMessage = `[Unity Bridge] ${message}`;
  console.log(logMessage);
};

// Keep these minimal functions for backward compatibility
export const initializeUnityBridgeDebug = () => {
  // No-op for now
  console.log('[Unity Bridge] Debug initialized');
};

export const logUnityBridgeState = () => {
  // No-op for now
  console.log('[Unity Bridge] State logged');
}; 