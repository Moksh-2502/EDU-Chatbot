import { FluencyGenerator, FluencyGeneratorConfig, Question, StudentState } from '../types';
import { StorageAdapter } from '../utils';
export declare class SimpleGenerator implements FluencyGenerator {
    private storageKey;
    private config;
    private state;
    private defaultSequence;
    private questions;
    private storage;
    /**
     * Creates a new SimpleGenerator with optional configuration
     * @param config Optional configuration for the generator
     * @param storageKey Key to use for storage (defaults to 'fluencyState')
     * @param storageAdapter Custom storage adapter for platforms without localStorage
     */
    constructor(config?: FluencyGeneratorConfig, storageKey?: string, storageAdapter?: StorageAdapter);
    private resetState;
    private generateQuestion;
    /**
     * Gets the next block of questions based on the current state
     * @returns Promise that resolves to an array of Question objects
     */
    getNextQuestionBlock(): Promise<Question[]>;
    /**
     * Submits an answer for a given question and updates the state accordingly
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
     * Saves the current state to storage
     * @returns Promise that resolves when the state has been saved
     */
    private saveState;
    /**
     * Loads the state from storage
     * @returns Promise that resolves when the state has been loaded
     */
    private loadState;
    /**
     * Gets the current state
     * @returns Promise that resolves to a copy of the current state
     */
    getState(): Promise<StudentState>;
    /**
     * Sets the state
     * @param state The new state
     * @returns Promise that resolves when the state has been set
     */
    setState(state: StudentState): Promise<void>;
    /**
     * Resets the state to the initial values
     * @returns Promise that resolves when the state has been reset
     */
    reset(): Promise<void>;
    /**
     * Sets the learning mode
     * @param mode The mode to set
     * @returns Promise that resolves when the mode has been set
     */
    setMode(mode: 'learning' | 'placement' | 'reinforcement'): Promise<void>;
}
