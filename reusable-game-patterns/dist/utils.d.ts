import { Question, StudentState } from './types';
/**
 * Calculates the response time in milliseconds
 * @param startTime The start time in milliseconds
 * @returns The response time in milliseconds
 */
export declare function calculateResponseTime(startTime: number): number;
/**
 * Gets a formatted display of a question
 * @param question The question object
 * @returns A formatted string (e.g., "3 × 4 = ?")
 */
export declare function formatQuestion(question: Question): string;
/**
 * Calculates accuracy statistics from a student state
 * @param state The student state
 * @returns An object with accuracy statistics
 */
export declare function calculateAccuracy(state: StudentState): {
    totalCorrect: number;
    totalIncorrect: number;
    accuracyPercentage: number;
};
/**
 * Gets the current learning focus based on state
 * @param state The student state
 * @param sequence The sequence array
 * @returns The current focus number
 */
export declare function getCurrentFocus(state: StudentState, sequence: number[]): number;
/**
 * Custom storage adapter interface for platforms without localStorage
 */
export interface StorageAdapter {
    getItem(key: string): string | null;
    setItem(key: string, value: string): void;
    removeItem(key: string): void;
}
/**
 * Creates a custom storage adapter for platforms that don't have localStorage
 * @param getItemFn Function to get an item
 * @param setItemFn Function to set an item
 * @param removeItemFn Function to remove an item
 * @returns A storage adapter
 */
export declare function createStorageAdapter(getItemFn: (key: string) => string | null, setItemFn: (key: string, value: string) => void, removeItemFn: (key: string) => void): StorageAdapter;
