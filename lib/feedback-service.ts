/**
 * Feedback service for handling user feedback functionality
 * Provides methods to open feedback forms and handle feedback events
 */

import type { GiveFeedbackMessage, FeedbackData } from '@/lib/types/unity-bridge';

const FEEDBACK_URL = 'https://docs.google.com/forms/d/e/1FAIpQLScLNB8uP_wrdDLBJ2J2E28BtxbK1oYyfBOWPu-ubOcuqgVvPQ/viewform?usp=header';

/**
 * Opens the feedback form in a new tab
 * @param data Optional feedback data for tracking/analytics
 */
export function giveFeedback(data: FeedbackData = {}) {
  console.log('[FeedbackService] Give feedback called with data:', data);
  
  // Open feedback URL in new tab
  window.open(FEEDBACK_URL, '_blank', 'noopener,noreferrer');
}

/**
 * Handles feedback messages from Unity games
 * @param message The feedback message from Unity
 */
export function handleFeedbackMessage(message: GiveFeedbackMessage) {
  console.log('[FeedbackService] Handling Unity feedback message:', message);
  
  // Call giveFeedback with the message data
  giveFeedback(message.data || {});
}

/**
 * @deprecated Use handleFeedbackMessage instead
 * Legacy function for backward compatibility
 */
export function handleFeedbackEvent(eventData: any) {
  console.log('[FeedbackService] Handling legacy Unity feedback event:', eventData);
  
  // Convert legacy event data to new message format if needed
  const data = eventData?.data || {};
  giveFeedback(data);
} 