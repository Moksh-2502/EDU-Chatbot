mergeInto(LibraryManager.library, {
  
  // Check if React Unity WebGL bridge is available
  IsReactBridgeAvailable: function() {
    try {
      var available = typeof window !== 'undefined' && 
                     typeof window.dispatchReactUnityMessage === 'function';
      console.log('[ReactBridge] Bridge availability check - available:', available);
      return available;
    } catch (e) {
      console.warn('[ReactBridge] Error checking bridge availability:', e);
      return false;
    }
  },

  // Send a message to React using the new ReactGameMessage format
  SendMessageToReact: function(jsonDataPtr) {
    try {
      var jsonData = UTF8ToString(jsonDataPtr);
      
      if (typeof window !== 'undefined' && 
          typeof window.dispatchReactUnityMessage === 'function') {
        
        // Validate JSON before sending
        try {
          JSON.parse(jsonData); // Just validate, don't store
        } catch (e) {
          console.error('[ReactBridge] Failed to parse message JSON:', e, jsonData);
          return;
        }
        
        // Send the entire JSON string to React for processing
        window.dispatchReactUnityMessage(jsonData);
        console.log('[ReactBridge] Successfully sent message to React');
      } else {
        console.warn('[ReactBridge] React bridge not available, message not sent');
        console.log('[ReactBridge] window available:', typeof window !== 'undefined');
        console.log('[ReactBridge] dispatchReactUnityMessage available:', 
                   typeof window !== 'undefined' ? typeof window.dispatchReactUnityMessage : 'window unavailable');
      }
    } catch (e) {
      console.error('[ReactBridge] Error sending message:', e);
    }
  }
}); 