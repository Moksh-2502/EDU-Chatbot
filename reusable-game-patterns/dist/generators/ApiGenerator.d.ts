import { FluencyGenerator, Question, StudentState } from '../types';
/**
 * ApiGenerator implements the FluencyGenerator interface using API calls
 * to a remote server for all operations.
 */
export declare class ApiGenerator implements FluencyGenerator {
    private apiBaseUrl;
    private apiKey?;
    private sessionId?;
    private cachedState?;
    /**
     * Creates a new ApiGenerator
     * @param apiBaseUrl The base URL for the API
     * @param apiKey Optional API key for authentication
     */
    constructor(apiBaseUrl: string, apiKey?: string);
    /**
     * Makes an authenticated API request
     * @param endpoint The API endpoint to call
     * @param method The HTTP method to use
     * @param data The request body data
     * @returns Promise that resolves to the response data
     */
    private apiRequest;
    /**
     * Gets the next block of questions from the API
     * @returns Promise that resolves to an array of Question objects
     */
    getNextQuestionBlock(): Promise<Question[]>;
    /**
     * Submits an answer to the API
     * @param questionId The ID of the question being answered
     * @param answer The user's answer
     * @param responseTimeMs The response time in milliseconds
     * @returns Promise that resolves to an object indicating if the answer was correct and the correct answer
     */
    submitAnswer(questionId: string, answer: number, responseTimeMs: number): Promise<{
        isCorrect: boolean;
        correctAnswer?: number;
    }>;
    /**
     * Gets the current state from the API
     * @returns Promise that resolves to the current StudentState
     */
    getState(): Promise<StudentState>;
    /**
     * Sets the state via the API
     * @param state The new state
     * @returns Promise that resolves when the state has been set
     */
    setState(state: StudentState): Promise<void>;
    /**
     * Resets the state via the API
     * @returns Promise that resolves when the state has been reset
     */
    reset(): Promise<void>;
    /**
     * Sets the learning mode via the API
     * @param mode The mode to set
     * @returns Promise that resolves when the mode has been set
     */
    setMode(mode: 'learning' | 'placement' | 'reinforcement'): Promise<void>;
}
