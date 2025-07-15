export interface IUserDataStorageService {
  /**
   * Load data for a user and key
   * @param userId Unique identifier for the user
   * @param key Storage key for the data
   * @returns The stored data or null if not found
   */
  load<T = any>(userId: string, key: string): Promise<T | null>;

  /**
   * Save data for a user and key
   * @param userId Unique identifier for the user
   * @param key Storage key for the data
   * @param data The data to store
   * @returns Promise that resolves when save is complete
   */
  save<T = any>(userId: string, key: string, data: T): Promise<void>;

  /**
   * Delete data for a user and key
   * @param userId Unique identifier for the user
   * @param key Storage key for the data
   * @returns Promise that resolves when delete is complete
   */
  delete(userId: string, key: string): Promise<void>;

  /**
   * Check if data exists for a user and key
   * @param userId Unique identifier for the user
   * @param key Storage key for the data
   * @returns Promise that resolves to true if data exists, false otherwise
   */
  exists(userId: string, key: string): Promise<boolean>;
}
