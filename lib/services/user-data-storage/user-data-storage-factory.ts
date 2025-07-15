import type { IUserDataStorageService } from './user-data-storage-service';
import { S3UserDataStorageService } from './s3-user-data-storage-service';
import { LocalUserDataStorageService } from './local-user-data-storage-service';
import path from 'node:path';

/**
 * Factory for creating user data storage service instances
 */
export class UserDataStorageFactory {
  /**
   * Creates an S3-based user data storage service
   * @param bucketName S3 bucket name for user data storage
   * @returns S3UserDataStorageService instance
   */
  static createS3Storage(bucketName: string): IUserDataStorageService {
    return new S3UserDataStorageService(bucketName);
  }

  /**
   * Creates a local filesystem-based user data storage service
   * @param storagePath Base directory path for user data storage
   * @returns LocalUserDataStorageService instance
   */
  static createLocalStorage(storagePath?: string): IUserDataStorageService {
    // If no path provided, use 'data' relative to server's running directory
    const basePath = storagePath || path.join(process.cwd(), 'data');
    return new LocalUserDataStorageService(basePath);
  }

  /**
   * Creates the default user data storage service based on environment configuration
   * @returns The configured user data storage service
   */
  static createDefaultStorage(): IUserDataStorageService {
    const bucketName = process.env.STORAGE_BUCKET;
    const useLocalStorage = process.env.USE_LOCAL_STORAGE === 'true';

    if (useLocalStorage) {
      const localPath = process.env.LOCAL_USER_DATA_STORAGE_PATH;
      return UserDataStorageFactory.createLocalStorage(localPath);
    }

    if (!bucketName) {
      console.warn(
        'S3 bucket name not provided for user data, falling back to local storage',
      );
      return UserDataStorageFactory.createLocalStorage();
    }

    return UserDataStorageFactory.createS3Storage(bucketName);
  }
}
