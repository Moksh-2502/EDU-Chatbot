import type { StorageRequestMessage, StorageResponseMessage } from './unity-bridge';

/**
 * Storage operation types that Unity can request
 */
export type StorageOperation = 'load' | 'save' | 'delete' | 'exists';

/**
 * Interface that React-side storage implementations should follow
 */
export interface IReactStorageService {
  /**
   * Load data for a user and key
   */
  load(userId: string, key: string): Promise<string>;

  /**
   * Save data for a user and key
   */
  save(userId: string, key: string, jsonData: string): Promise<void>;

  /**
   * Delete data for a user and key
   */
  delete(userId: string, key: string): Promise<void>;

  /**
   * Check if data exists for a user and key
   */
  exists(userId: string, key: string): Promise<boolean>;
}

/**
 * Local storage implementation of IReactStorageService
 */
export class LocalReactStorageService implements IReactStorageService {
  private getStorageKey(userId: string, key: string): string {
    return `game_storage_${userId}_${key}`;
  }

  async load(userId: string, key: string): Promise<string> {
    try {
      const storageKey = this.getStorageKey(userId, key);
      const data = localStorage.getItem(storageKey);
      return data || '';
    } catch (error) {
      console.error(`Error loading data for key ${key}:`, error);
      return '';
    }
  }

  async save(userId: string, key: string, data: string): Promise<void> {
    try {
      const storageKey = this.getStorageKey(userId, key);
      localStorage.setItem(storageKey, data);
    } catch (error) {
      console.error(`Error saving data for key ${key}:`, error);
      throw error;
    }
  }

  async delete(userId: string, key: string): Promise<void> {
    try {
      const storageKey = this.getStorageKey(userId, key);
      localStorage.removeItem(storageKey);
    } catch (error) {
      console.error(`Error deleting data for key ${key}:`, error);
      throw error;
    }
  }

  async exists(userId: string, key: string): Promise<boolean> {
    try {
      const storageKey = this.getStorageKey(userId, key);
      return localStorage.getItem(storageKey) !== null;
    } catch (error) {
      console.error(`Error checking existence for key ${key}:`, error);
      return false;
    }
  }
}

/**
 * API-based storage implementation that calls server endpoints
 */
export class ApiReactStorageService implements IReactStorageService {
  constructor(private readonly baseUrl = '/api/user-data') {}

  async load(userId: string, key: string): Promise<string> {
    try {
      const response = await fetch(`${this.baseUrl}/${key}`);

      if (response.status === 404) {
        return '';
      }

      if (!response.ok) {
        throw new Error(`Failed to load data: ${response.statusText}`);
      }

      const result = await response.json();
      return typeof result.data === 'string' ? result.data : JSON.stringify(result.data);
    } catch (error) {
      console.error(`Error loading data for ${userId}/${key}:`, error);
      throw error;
    }
  }

  async save(userId: string, key: string, data: string): Promise<void> {
    try {
      const response = await fetch(`${this.baseUrl}/${key}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: data,
      });

      if (!response.ok) {
        throw new Error(`Failed to save data: ${response.statusText}`);
      }
    } catch (error) {
      console.error(`Error saving data for ${userId}/${key}:`, error);
      throw error;
    }
  }

  async delete(userId: string, key: string): Promise<void> {
    try {
      const response = await fetch(`${this.baseUrl}/${key}`, {
        method: 'DELETE',
      });

      if (!response.ok) {
        throw new Error(`Failed to delete data: ${response.statusText}`);
      }
    } catch (error) {
      console.error(`Error deleting data for ${userId}/${key}:`, error);
      throw error;
    }
  }

  async exists(userId: string, key: string): Promise<boolean> {
    try {
      const response = await fetch(`${this.baseUrl}/${key}`, {
        method: 'HEAD',
      });

      return response.status === 200;
    } catch (error) {
      console.error(`Error checking existence for ${userId}/${key}:`, error);
      return false;
    }
  }
}

/**
 * Storage handler that processes storage messages from Unity
 */
export class UnityStorageHandler {
  constructor(private storageService: IReactStorageService) {}

  /**
   * Process a storage request message from Unity
   */
  async handleStorageRequest(request: StorageRequestMessage): Promise<StorageResponseMessage> {
    console.log('[UnityStorageHandler] Processing request:', request);

    const response: StorageResponseMessage = {
      messageType: 'StorageResponse',
      correlationId: request.correlationId,
      success: false,
      jsonData: '',
      error: '',
      timestamp: Date.now()
    };

    try {
      const { operation, userId, key, jsonData } = request;

      // Validate required fields
      if (!operation || !userId || !key) {
        const error = 'Missing required fields in storage request';
        console.error('[UnityStorageHandler]', error);
        response.success = false;
        response.error = error;
        return response;
      }

      switch (operation) {
        case 'load': {
          const loadedData = await this.storageService.load(userId, key);
          response.success = true;
          response.jsonData = loadedData;
          break;
        }

        case 'save':
          if (!jsonData) {
            const error = 'Save operation requires jsonData';
            console.error('[UnityStorageHandler]', error);
            response.success = false;
            response.error = error;
            return response;
          }
          await this.storageService.save(userId, key, jsonData);
          response.success = true;
          break;

        case 'delete':
          await this.storageService.delete(userId, key);
          response.success = true;
          break;

        case 'exists': {
          const exists = await this.storageService.exists(userId, key);
          response.success = true;
          response.jsonData = JSON.stringify({ exists });
          break;
        }

        default: {
          const error = `Unknown storage operation: ${operation}`;
          console.error('[UnityStorageHandler]', error);
          response.success = false;
          response.error = error;
          break;
        }
      }

      console.log('[UnityStorageHandler] Response:', response);
      return response;

    } catch (error) {
      console.error('[UnityStorageHandler] Error processing request:', error);
      response.success = false;
      response.error = error instanceof Error ? error.message : 'Unknown error';
      return response;
    }
  }
}
