import type { IGameStorageService } from './game-storage-service';
import { S3GameStorageService } from './s3-game-storage-service';
import { LocalGameStorageService } from './local-game-storage-service';
import path from 'node:path';

/**
 * Factory for creating game storage service instances
 */
export class GameStorageFactory {
  /**
   * Creates an S3-based game storage service
   * @param bucketName S3 bucket name for game storage
   * @param cdnUrl CloudFront distribution URL
   * @returns S3GameStorageService instance
   */
  static createS3Storage(
    bucketName: string,
    cdnUrl?: string,
  ): IGameStorageService {
    return new S3GameStorageService(bucketName, cdnUrl);
  }

  /**
   * Creates a local filesystem-based game storage service
   * @param storagePath Base directory path for game storage
   * @returns LocalGameStorageService instance
   */
  static createLocalStorage(storagePath?: string): IGameStorageService {
    // If no path provided, use 'games' relative to server's running directory
    const basePath = storagePath || path.join(process.cwd(), 'games');
    return new LocalGameStorageService(basePath);
  }

  /**
   * Creates the default game storage service based on environment configuration
   * @returns The configured game storage service
   */
  static createDefaultStorage(): IGameStorageService {
    const bucketName = process.env.STORAGE_BUCKET;
    const cdnUrl = process.env.STORAGE_CDN_URL;
    const useLocalStorage = process.env.USE_LOCAL_STORAGE === 'true';

    if (useLocalStorage) {
      const localPath = process.env.LOCAL_GAME_STORAGE_PATH;
      return GameStorageFactory.createLocalStorage(localPath);
    }

    if (!bucketName) {
      console.warn(
        'S3 bucket name not provided, falling back to local storage',
      );
      return GameStorageFactory.createLocalStorage();
    }

    return GameStorageFactory.createS3Storage(bucketName, cdnUrl);
  }
}
