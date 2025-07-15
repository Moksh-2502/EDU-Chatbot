import { S3 } from '@aws-sdk/client-s3';
import path from 'node:path';
import type { IUserDataStorageService } from './user-data-storage-service';

export class S3UserDataStorageService implements IUserDataStorageService {
  private s3: S3;
  private bucketName: string;
  private userDataPrefix = 'user-data';

  constructor(bucketName: string) {
    if (!bucketName) {
      throw new Error('S3 bucket name must be provided');
    }
    this.s3 = new S3();
    this.bucketName = bucketName;
  }

  async load<T = any>(userId: string, key: string): Promise<T | null> {
    const s3Key = this.getS3Key(userId, key);

    try {
      const response = await this.s3.getObject({
        Bucket: this.bucketName,
        Key: s3Key,
      });

      if (!response.Body) {
        return null;
      }

      const content = await response.Body.transformToString();
      return JSON.parse(content) as T;
    } catch (error: any) {
      if (error.name === 'NoSuchKey' || error.name === 'NotFound') {
        return null;
      }
      console.error(`Error loading user data for ${userId}/${key}:`, error);
      throw error;
    }
  }

  async save<T = any>(userId: string, key: string, data: T): Promise<void> {
    const s3Key = this.getS3Key(userId, key);
    const jsonData = JSON.stringify(data, null, 2);

    try {
      await this.s3.putObject({
        Bucket: this.bucketName,
        Key: s3Key,
        Body: jsonData,
        ContentType: 'application/json',
      });
    } catch (error) {
      console.error(`Error saving user data for ${userId}/${key}:`, error);
      throw error;
    }
  }

  async delete(userId: string, key: string): Promise<void> {
    const s3Key = this.getS3Key(userId, key);

    try {
      await this.s3.deleteObject({
        Bucket: this.bucketName,
        Key: s3Key,
      });
    } catch (error) {
      console.error(`Error deleting user data for ${userId}/${key}:`, error);
      throw error;
    }
  }

  async exists(userId: string, key: string): Promise<boolean> {
    const s3Key = this.getS3Key(userId, key);

    try {
      await this.s3.headObject({
        Bucket: this.bucketName,
        Key: s3Key,
      });
      return true;
    } catch (error: any) {
      if (error.name === 'NoSuchKey' || error.name === 'NotFound') {
        return false;
      }
      console.error(
        `Error checking user data existence for ${userId}/${key}:`,
        error,
      );
      throw error;
    }
  }

  private getS3Key(userId: string, key: string): string {
    // Sanitize userId and key to prevent directory traversal
    const safeUserId = userId.replace(/[^a-zA-Z0-9-_]/g, '_');
    const safeKey = key.replace(/[^a-zA-Z0-9-_]/g, '_');

    return path
      .join(this.userDataPrefix, safeUserId, `${safeKey}.json`)
      .replace(/\\/g, '/');
  }
}
