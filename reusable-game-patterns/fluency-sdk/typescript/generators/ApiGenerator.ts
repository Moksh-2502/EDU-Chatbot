import type { 
  FluencyGenerator, 
  Question, 
  StudentState 
} from '../types';

/**
 * ApiGenerator implements the FluencyGenerator interface using API calls
 * to a remote server for all operations.
 */
export class ApiGenerator implements FluencyGenerator {
  private apiBaseUrl: string;
  private apiKey?: string;
  private sessionId?: string;
  private cachedState?: StudentState;
  
  /**
   * Creates a new ApiGenerator
   * @param apiBaseUrl The base URL for the API
   * @param apiKey Optional API key for authentication
   */
  constructor(apiBaseUrl: string, apiKey?: string) {
    this.apiBaseUrl = apiBaseUrl.endsWith('/') ? apiBaseUrl : `${apiBaseUrl}/`;
    this.apiKey = apiKey;
  }
  
  /**
   * Makes an authenticated API request
   * @param endpoint The API endpoint to call
   * @param method The HTTP method to use
   * @param data The request body data
   * @returns Promise that resolves to the response data
   */
  private async apiRequest<T>(endpoint: string, method: string, data?: any): Promise<T> {
    const url = `${this.apiBaseUrl}${endpoint}`;
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };
    
    if (this.apiKey) {
      headers.Authorization = `Bearer ${this.apiKey}`;
    }
    
    if (this.sessionId) {
      headers['X-Session-ID'] = this.sessionId;
    }
    
    const response = await fetch(url, {
      method,
      headers,
      body: data ? JSON.stringify(data) : undefined,
    });
    
    if (!response.ok) {
      throw new Error(`API request failed: ${response.status} ${response.statusText}`);
    }
    
    return await response.json();
  }
  
  /**
   * Gets the next block of questions from the API
   * @returns Promise that resolves to an array of Question objects
   */
  public async getNextQuestionBlock(): Promise<Question[]> {
    const response = await this.apiRequest<{
      questions: Question[];
      sessionId?: string;
    }>('questions', 'GET');
    
    if (response.sessionId) {
      this.sessionId = response.sessionId;
    }
    
    return response.questions;
  }
  
  /**
   * Submits an answer to the API
   * @param questionId The ID of the question being answered
   * @param answer The user's answer
   * @param responseTimeMs The response time in milliseconds
   * @returns Promise that resolves to an object indicating if the answer was correct and the correct answer
   */
  public async submitAnswer(
    questionId: string, 
    answer: number, 
    responseTimeMs: number
  ): Promise<{ isCorrect: boolean; correctAnswer?: number }> {
    return this.apiRequest<{ isCorrect: boolean; correctAnswer?: number }>(
      'answers', 
      'POST', 
      {
        questionId,
        answer,
        responseTimeMs
      }
    );
  }
  
  /**
   * Gets the current state from the API
   * @returns Promise that resolves to the current StudentState
   */
  public async getState(): Promise<StudentState> {
    // Use cached state if available to reduce API calls
    if (this.cachedState) {
      return { ...this.cachedState };
    }
    
    const state = await this.apiRequest<StudentState>('state', 'GET');
    this.cachedState = state;
    return { ...state };
  }
  
  /**
   * Sets the state via the API
   * @param state The new state
   * @returns Promise that resolves when the state has been set
   */
  public async setState(state: StudentState): Promise<void> {
    await this.apiRequest<void>('state', 'PUT', state);
    this.cachedState = { ...state };
  }
  
  /**
   * Resets the state via the API
   * @returns Promise that resolves when the state has been reset
   */
  public async reset(): Promise<void> {
    await this.apiRequest<void>('reset', 'POST');
    this.cachedState = undefined;
  }
  
  /**
   * Sets the learning mode via the API
   * @param mode The mode to set
   * @returns Promise that resolves when the mode has been set
   */
  public async setMode(mode: 'learning' | 'placement' | 'reinforcement'): Promise<void> {
    await this.apiRequest<void>('mode', 'PUT', { mode });
    
    // Update cached state if available
    if (this.cachedState) {
      this.cachedState.mode = mode;
    }
  }
} 