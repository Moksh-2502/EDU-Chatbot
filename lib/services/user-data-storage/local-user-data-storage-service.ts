import fs from 'node:fs/promises';
import path from 'node:path';
import type { IUserDataStorageService } from './user-data-storage-service';

export class LocalUserDataStorageService implements IUserDataStorageService {
  private storagePath: string;

  constructor(storagePath: string) {
    this.storagePath = storagePath;
  }

  async load<T = any>(userId: string, key: string): Promise<T | null> {
    const filePath = this.getFilePath(userId, key);

    try {
      const content = await fs.readFile(filePath, 'utf-8');
      return JSON.parse(content) as T;
    } catch (error: any) {
      if (error.code === 'ENOENT') {
        return null;
      }
      console.error(`Error loading user data for ${userId}/${key}:`, error);
      throw error;
    }
  }

  async save<T = any>(userId: string, key: string, data: T): Promise<void> {
    const filePath = this.getFilePath(userId, key);
    const dirPath = path.dirname(filePath);

    try {
      // Ensure directory exists
      await fs.mkdir(dirPath, { recursive: true });

      const jsonData = JSON.stringify(data, null, 2);
      await fs.writeFile(filePath, jsonData, 'utf-8');
    } catch (error) {
      console.error(`Error saving user data for ${userId}/${key}:`, error);
      throw error;
    }
  }

  async delete(userId: string, key: string): Promise<void> {
    const filePath = this.getFilePath(userId, key);

    try {
      await fs.unlink(filePath);
    } catch (error: any) {
      if (error.code === 'ENOENT') {
        // File doesn't exist, consider it already deleted
        return;
      }
      console.error(`Error deleting user data for ${userId}/${key}:`, error);
      throw error;
    }
  }

  async exists(userId: string, key: string): Promise<boolean> {
    const filePath = this.getFilePath(userId, key);

    try {
      await fs.access(filePath);
      return true;
    } catch (error: any) {
      if (error.code === 'ENOENT') {
        return false;
      }
      console.error(
        `Error checking user data existence for ${userId}/${key}:`,
        error,
      );
      throw error;
    }
  }

  private getFilePath(userId: string, key: string): string {
    // Sanitize userId and key to prevent directory traversal
    const safeUserId = userId.replace(/[^a-zA-Z0-9-_]/g, '_');
    const safeKey = key.replace(/[^a-zA-Z0-9-_]/g, '_');

    return path.join(
      this.storagePath,
      'user-data',
      safeUserId,
      `${safeKey}.json`,
    );
  }
}
