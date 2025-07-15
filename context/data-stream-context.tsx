'use client';

import { createContext, useContext, useState, useCallback, type ReactNode, useEffect, useRef } from 'react';
import { useChat } from '@ai-sdk/react';

interface DataStreamEvent {
  type: string;
  content: any;
}

interface DataStreamContextProps {
  createListener: (callback: (data: any) => void) => string;
  removeListener: (id: string) => void;
}

const DataStreamContext = createContext<DataStreamContextProps | undefined>(undefined);

export function DataStreamProvider({ children, chatId }: { children: ReactNode; chatId: string }) {
  const [listeners, setListeners] = useState<Record<string, (data: any) => void>>({});
  const { data: dataStream } = useChat({ id: chatId });
  const processedLength = useRef<number>(0);
  
  // Process incoming data stream and notify all listeners
  useEffect(() => {
    if (dataStream && dataStream.length > 0 && dataStream.length > processedLength.current) {
      // Only process new data that we haven't seen before
      const newDataItems = dataStream.slice(processedLength.current);
      
      // Update the processed length reference
      processedLength.current = dataStream.length;
      
      // Process only the new items
      if (newDataItems.length > 0) {
        const latestData = newDataItems[newDataItems.length - 1];
        
        // Notify all listeners about the new data
        Object.values(listeners).forEach(callback => {
          callback(latestData);
        });
      }
    }
  }, [dataStream, listeners]);
  
  // Register a new listener and return its ID
  const createListener = useCallback((callback: (data: any) => void) => {
    const listenerId = Math.random().toString(36).substring(2, 9);
    setListeners(prev => ({
      ...prev,
      [listenerId]: callback
    }));
    return listenerId;
  }, []);
  
  // Remove a listener by its ID
  const removeListener = useCallback((id: string) => {
    setListeners(prev => {
      const newListeners = { ...prev };
      delete newListeners[id];
      return newListeners;
    });
  }, []);
  
  return (
    <DataStreamContext.Provider value={{ createListener, removeListener }}>
      {children}
    </DataStreamContext.Provider>
  );
}

export function useDataStreamContext() {
  const context = useContext(DataStreamContext);
  
  if (context === undefined) {
    throw new Error('useDataStreamContext must be used within a DataStreamProvider');
  }
  
  return context;
} 