import { lookup } from 'mime-types';
import fs from 'node:fs/promises';
import { existsSync } from 'node:fs';
import path from 'node:path';
import { Readable } from 'node:stream';
import { ReadableStream } from 'node:stream/web';
import type {
  IGameStorageService,
  StreamResult,
  GameSkeleton,
} from './game-storage-service';

export class LocalGameStorageService implements IGameStorageService {
  private storageBasePath: string;
  private skeletonsBasePath: string;
  private skeletonSourceDirs: Map<string, string> = new Map();

  constructor(storagePath: string) {
    if (!storagePath) {
      throw new Error('Local storage path must be provided');
    }
    this.storageBasePath = storagePath;
    this.skeletonsBasePath = path.join(process.cwd(), 'game_skeletons');
  }

  async uploadGameDirectory(
    sourceDirectory: string,
    gameId: string,
    excludeFiles: string[] = ['skeleton-manifest.json'],
  ): Promise<void> {
    const targetDir = path.join(this.storageBasePath, gameId);
    await fs.mkdir(targetDir, { recursive: true });
    await this.copyDirectory(sourceDirectory, targetDir, excludeFiles);
  }

  async serveGameFile(gameId: string, filePath: string): Promise<StreamResult> {
    const fullPath = gameId
      ? path.join(this.storageBasePath, gameId, filePath)
      : path.join(this.storageBasePath, filePath);

    return this.getFileAsStream(fullPath, filePath);
  }

  async gameFileExists(gameId: string, filePath: string): Promise<boolean> {
    const fullPath = gameId
      ? path.join(this.storageBasePath, gameId, filePath)
      : path.join(this.storageBasePath, filePath);
    return existsSync(fullPath);
  }

  async serveSkeletonFile(
    templateName: string,
    filePath: string,
  ): Promise<StreamResult> {
    const sourceDir = await this.getSkeletonSourceDirectory(templateName);
    const fullPath = path.join(sourceDir, filePath);
    console.log('fullPath', fullPath);
    return this.getFileAsStream(fullPath, filePath);
  }

  async skeletonFileExists(
    templateName: string,
    filePath: string,
  ): Promise<boolean> {
    try {
      const sourceDir = await this.getSkeletonSourceDirectory(templateName);
      const fullPath = path.join(sourceDir, filePath);
      return existsSync(fullPath);
    } catch {
      return false;
    }
  }

  async createGameFromSkeleton(
    templateName: string,
    gameId: string,
  ): Promise<void> {
    const sourceDir = await this.getSkeletonSourceDirectory(templateName);
    const targetDir = path.join(this.storageBasePath, gameId);

    await fs.mkdir(targetDir, { recursive: true });
    await this.copyDirectory(sourceDir, targetDir, ['skeleton-manifest.json']);
  }

  async getGameSkeletons(): Promise<GameSkeleton[]> {
    try {
      if (!existsSync(this.skeletonsBasePath)) {
        return [];
      }

      const entries = await fs.readdir(this.skeletonsBasePath, {
        withFileTypes: true,
      });
      const skeletons: GameSkeleton[] = [];

      for (const entry of entries) {
        if (!entry.isDirectory()) continue;

        const skeletonDir = path.join(this.skeletonsBasePath, entry.name);
        const manifestPath = path.join(skeletonDir, 'skeleton-manifest.json');

        if (!existsSync(manifestPath)) continue;

        try {
          const manifestContent = await fs.readFile(manifestPath, 'utf-8');
          const manifest = JSON.parse(manifestContent);

          skeletons.push({
            ...manifest,
            directoryPath: entry.name,
          });
        } catch (e) {
          console.error(`Error parsing manifest for ${entry.name}:`, e);
        }
      }

      return skeletons;
    } catch (error) {
      console.error('Error getting game skeletons:', error);
      return [];
    }
  }

  async getGameSkeletonByTemplate(
    templateName: string,
  ): Promise<GameSkeleton | null> {
    try {
      const skeletons = await this.getGameSkeletons();
      return (
        skeletons.find((skeleton) => skeleton.template === templateName) || null
      );
    } catch (error) {
      console.error(
        `Error getting game skeleton with template ${templateName}:`,
        error,
      );
      return null;
    }
  }

  private async getFileAsStream(
    fullPath: string,
    filePath: string,
  ): Promise<StreamResult> {
    try {
      await fs.access(fullPath);
      const stats = await fs.stat(fullPath);

      const fileStream = Readable.fromWeb(
        new ReadableStream({
          async start(controller) {
            try {
              const data = await fs.readFile(fullPath);
              controller.enqueue(data);
              controller.close();
            } catch (error) {
              controller.error(error);
            }
          },
        }),
      );

      const webStream = Readable.toWeb(
        fileStream,
      ) as ReadableStream<Uint8Array>;

      return {
        stream: webStream,
        contentType: lookup(filePath) || 'application/octet-stream',
        contentLength: stats.size,
      };
    } catch (error) {
      console.error(
        `Error reading file from local storage: ${fullPath}`,
        error,
      );
      throw new Error(`File not found: ${filePath}`);
    }
  }

  private async getSkeletonSourceDirectory(
    templateName: string,
  ): Promise<string> {
    const cached = this.skeletonSourceDirs.get(templateName);
    if (cached) {
      return cached;
    }

    const skeletonDir = path.join(this.skeletonsBasePath, templateName);

    if (!existsSync(skeletonDir)) {
      throw new Error(`Skeleton directory not found: ${skeletonDir}`);
    }

    let sourceDirToCopy = skeletonDir;
    const manifestPath = path.join(skeletonDir, 'skeleton-manifest.json');

    if (existsSync(manifestPath)) {
      try {
        const manifestContent = await fs.readFile(manifestPath, 'utf8');
        const manifest = JSON.parse(manifestContent);

        if (manifest.copy_directory) {
          const dirPath = path.join(skeletonDir, manifest.copy_directory);
          if (existsSync(dirPath) && (await fs.stat(dirPath)).isDirectory()) {
            sourceDirToCopy = dirPath;
          }
        }
      } catch (error) {
        console.error(
          `Error parsing skeleton manifest for ${templateName}:`,
          error,
        );
      }
    }

    this.skeletonSourceDirs.set(templateName, sourceDirToCopy);
    return sourceDirToCopy;
  }

  private async copyDirectory(
    source: string,
    target: string,
    excludeFiles: string[] = [],
  ) {
    const entries = await fs.readdir(source);

    for (const entryName of entries) {
      if (excludeFiles.includes(entryName)) {
        continue;
      }

      const sourcePath = path.join(source, entryName);
      const targetPath = path.join(target, entryName);
      const stats = await fs.stat(sourcePath);

      if (stats.isDirectory()) {
        await fs.mkdir(targetPath, { recursive: true });
        await this.copyDirectory(sourcePath, targetPath, excludeFiles);
      } else {
        await fs.copyFile(sourcePath, targetPath);
      }
    }
  }
}
