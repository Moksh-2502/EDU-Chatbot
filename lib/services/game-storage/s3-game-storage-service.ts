import { S3 } from '@aws-sdk/client-s3';
import { lookup } from 'mime-types';
import fs from 'node:fs/promises';
import path from 'node:path';
import { Readable } from 'node:stream';
import type {
  IGameStorageService,
  StreamResult,
  GameSkeleton,
} from './game-storage-service';

export class S3GameStorageService implements IGameStorageService {
  private readonly s3: S3;
  private readonly skeletonPrefix = 'skeletons';
  private readonly generatedGamesPrefix = 'generated-games';
  private readonly skeletonSourcePrefixes: Map<string, string> = new Map();

  constructor(
    private readonly bucketName: string,
    private readonly cdnBaseUrl?: string,
  ) {
    this.s3 = new S3();
  }

  async uploadGameDirectory(
    sourceDirectory: string,
    gameId: string,
    excludeFiles: string[] = ['skeleton-manifest.json'],
  ): Promise<void> {
    const s3GamePrefix = path
      .join(this.generatedGamesPrefix, gameId)
      .replace(/\\/g, '/');
    await this.uploadDirectory(sourceDirectory, s3GamePrefix, excludeFiles);
  }

  async serveGameFile(gameId: string, filePath: string): Promise<StreamResult> {
    const s3Key = path
      .join(this.generatedGamesPrefix, gameId, filePath)
      .replace(/\\/g, '/');

    if (this.cdnBaseUrl) {
      const exists = await this.fileExists(s3Key);
      if (!exists) {
        throw new Error('File not found');
      }

      const contentType = lookup(filePath) || 'application/octet-stream';
      return {
        redirectUrl: `${this.cdnBaseUrl}/${s3Key}`,
        contentType,
      };
    }

    return this.getFileAsStream(s3Key, filePath);
  }

  async gameFileExists(gameId: string, filePath: string): Promise<boolean> {
    const s3Key = path
      .join(this.generatedGamesPrefix, gameId, filePath)
      .replace(/\\/g, '/');
    return this.fileExists(s3Key);
  }

  async serveSkeletonFile(
    templateName: string,
    filePath: string,
  ): Promise<StreamResult> {
    const sourcePrefix = await this.getSkeletonSourcePrefix(templateName);
    const s3Key = path.join(sourcePrefix, filePath).replace(/\\/g, '/');

    if (this.cdnBaseUrl) {
      const exists = await this.fileExists(s3Key);
      if (!exists) {
        throw new Error('File not found');
      }

      const contentType = lookup(filePath) || 'application/octet-stream';
      return {
        redirectUrl: `${this.cdnBaseUrl}/${s3Key}`,
        contentType,
      };
    }

    return this.getFileAsStream(s3Key, filePath);
  }

  async skeletonFileExists(
    templateName: string,
    filePath: string,
  ): Promise<boolean> {
    const sourcePrefix = await this.getSkeletonSourcePrefix(templateName);
    const s3Key = path.join(sourcePrefix, filePath).replace(/\\/g, '/');
    return this.fileExists(s3Key);
  }

  async createGameFromSkeleton(
    templateName: string,
    gameId: string,
  ): Promise<void> {
    const sourcePrefix = await this.getSkeletonSourcePrefix(templateName);
    const targetPrefix = path
      .join(this.generatedGamesPrefix, gameId)
      .replace(/\\/g, '/');

    const listParams = {
      Bucket: this.bucketName,
      Prefix: sourcePrefix,
    };

    try {
      const listedObjects = await this.s3.listObjectsV2(listParams);

      if (!listedObjects.Contents || listedObjects.Contents.length === 0) {
        throw new Error(`No files found for skeleton ${templateName}`);
      }

      for (const object of listedObjects.Contents) {
        if (!object.Key) continue;

        const relativePath = object.Key.substring(sourcePrefix.length + 1);
        if (relativePath === 'skeleton-manifest.json') continue;

        const sourceKey = object.Key;
        const targetKey = path
          .join(targetPrefix, relativePath)
          .replace(/\\/g, '/');

        await this.s3.copyObject({
          Bucket: this.bucketName,
          CopySource: `${this.bucketName}/${sourceKey}`,
          Key: targetKey,
        });
      }
    } catch (error) {
      console.error(
        `Error copying skeleton ${templateName} to game ${gameId}:`,
        error,
      );
      throw error;
    }
  }

  async getGameSkeletons(): Promise<GameSkeleton[]> {
    try {
      const listedObjects = await this.s3.listObjectsV2({
        Bucket: this.bucketName,
        Prefix: `${this.skeletonPrefix}/`,
        Delimiter: '/',
      });

      if (!listedObjects.CommonPrefixes?.length) {
        console.error('No skeletons found in S3', {
          Bucket: this.bucketName,
          Prefix: `${this.skeletonPrefix}/`,
          Delimiter: '/',
        });
        return [];
      }

      const skeletons: GameSkeleton[] = [];

      for (const prefix of listedObjects.CommonPrefixes) {
        if (!prefix.Prefix) continue;

        const templateName = prefix.Prefix.substring(
          this.skeletonPrefix.length + 1,
        ).replace('/', '');
        const manifestKey = path
          .join(this.skeletonPrefix, templateName, 'skeleton-manifest.json')
          .replace(/\\/g, '/');

        try {
          const manifestResponse = await this.s3.getObject({
            Bucket: this.bucketName,
            Key: manifestKey,
          });

          if (manifestResponse.Body) {
            const manifestContent =
              await manifestResponse.Body.transformToString();
            const manifest = JSON.parse(manifestContent);

            skeletons.push({
              ...manifest,
              directoryPath: templateName,
            });
          }
        } catch (error) {
          console.error(`Error reading manifest for ${templateName}:`, error);
        }
      }

      return skeletons;
    } catch (error) {
      console.error('Error getting game skeletons from S3:', error);
      return [];
    }
  }

  async getGameSkeletonByTemplate(
    templateName: string,
  ): Promise<GameSkeleton | null> {
    try {
      const manifestKey = path
        .join(this.skeletonPrefix, templateName, 'skeleton-manifest.json')
        .replace(/\\/g, '/');

      const manifestResponse = await this.s3.getObject({
        Bucket: this.bucketName,
        Key: manifestKey,
      });

      if (manifestResponse.Body) {
        const manifestContent = await manifestResponse.Body.transformToString();
        const manifest = JSON.parse(manifestContent);

        return {
          ...manifest,
          directoryPath: templateName,
        };
      }

      return null;
    } catch (error: any) {
      if (error.name === 'NoSuchKey' || error.name === 'NotFound') {
        return null;
      }
      console.error(
        `Error getting game skeleton ${templateName} from S3:`,
        error,
      );
      return null;
    }
  }

  private async getFileAsStream(
    s3Key: string,
    filePath: string,
  ): Promise<StreamResult> {
    const s3Response = await this.s3.getObject({
      Bucket: this.bucketName,
      Key: s3Key,
    });

    if (!s3Response.Body) {
      throw new Error('S3 object body is empty or undefined.');
    }

    if (!(s3Response.Body instanceof Readable)) {
      throw new Error('S3 object body is not a Node.js Readable stream.');
    }

    const s3NodeStream = s3Response.Body;
    const webReadableStream = Readable.toWeb(
      s3NodeStream,
    ) as ReadableStream<Uint8Array>;
    const contentType = lookup(filePath) || 'application/octet-stream';

    return {
      stream: webReadableStream as any,
      contentType,
      contentLength: s3Response.ContentLength,
    };
  }

  private async fileExists(s3Key: string): Promise<boolean> {
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
      console.error(`Error checking file existence in S3: ${s3Key}`, error);
      throw error;
    }
  }

  private async uploadDirectory(
    sourceDirectory: string,
    s3KeyPrefix: string,
    excludeFiles: string[] = [],
  ) {
    const entries = await fs.readdir(sourceDirectory);

    for (const entryName of entries) {
      if (excludeFiles.includes(entryName)) {
        continue;
      }

      const s3Key = path.join(s3KeyPrefix, entryName).replace(/\\/g, '/');
      const localPath = path.join(sourceDirectory, entryName);
      const fileStat = await fs.stat(localPath);

      if (fileStat.isDirectory()) {
        await this.uploadDirectory(localPath, s3Key, excludeFiles);
      } else if (fileStat.isFile()) {
        try {
          const fileContent = await fs.readFile(localPath);
          await this.s3.putObject({
            Bucket: this.bucketName,
            Key: s3Key,
            Body: fileContent,
            ContentType: lookup(entryName) || 'application/octet-stream',
          });
        } catch (error) {
          console.error(`Failed to upload ${localPath} to S3:`, error);
          throw error;
        }
      }
    }
  }

  protected async getSkeletonSourcePrefix(
    templateName: string,
  ): Promise<string> {
    const cached = this.skeletonSourcePrefixes.get(templateName);
    if (cached) {
      return cached;
    }

    const basePrefix = path
      .join(this.skeletonPrefix, templateName)
      .replace(/\\/g, '/');
    let sourcePrefix = basePrefix;

    const manifestKey = path
      .join(this.skeletonPrefix, templateName, 'skeleton-manifest.json')
      .replace(/\\/g, '/');

    try {
      const manifestResponse = await this.s3.getObject({
        Bucket: this.bucketName,
        Key: manifestKey,
      });

      if (manifestResponse.Body) {
        const manifestContent = await manifestResponse.Body.transformToString();
        const manifest = JSON.parse(manifestContent);

        if (manifest.copy_directory) {
          const dirPrefix = path
            .join(basePrefix, manifest.copy_directory)
            .replace(/\\/g, '/');

          // Verify the directory exists by checking if there are any objects with this prefix
          const listResponse = await this.s3.listObjectsV2({
            Bucket: this.bucketName,
            Prefix: `${dirPrefix}/`,
            MaxKeys: 1,
          });

          if (listResponse.Contents && listResponse.Contents.length > 0) {
            sourcePrefix = dirPrefix;
          }
        }
      }
    } catch (error) {
      console.error(
        `Error parsing skeleton manifest for ${templateName}:`,
        error,
      );
    }

    this.skeletonSourcePrefixes.set(templateName, sourcePrefix);
    return sourcePrefix;
  }
}
