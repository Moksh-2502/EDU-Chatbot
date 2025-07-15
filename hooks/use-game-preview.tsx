'use client';

import { useState, useCallback } from 'react';

/**
 * Custom hook for managing game preview visibility
 */
export function useGamePreview() {
  const [isVisible, setIsVisible] = useState(false);
  
  const toggleVisibility = useCallback(() => {
    setIsVisible(prev => !prev);
  }, []);
  
  return {
    isVisible,
    setIsVisible,
    toggleVisibility
  };
} 