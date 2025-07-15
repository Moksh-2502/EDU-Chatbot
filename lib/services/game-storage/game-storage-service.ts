import type { ReadableStream } from 'node:stream/web';

export type StreamResult =
  | {
      stream: ReadableStream<Uint8Array>;
      contentType: string;
      contentLength?: number;
    }
  | {
      redirectUrl: string;
      contentType: string;
    };

export interface GameSkeleton {
  template: string;
  created: string;
  description: string;
  modifiable_file_names: string[];
  directoryPath: string;
}

export interface IGameStorageService {
  /**
   * Uploads an entire directory to storage as a game instance
   * @param sourceDirectory Local directory to upload
   * @param gameId Unique identifier for the game
   * @param excludeFiles Optional files to exclude
   * @returns Promise that resolves when upload is complete
   */
  uploadGameDirectory(
    sourceDirectory: string,
    gameId: string,
    excludeFiles?: string[],
  ): Promise<void>;

  /**
   * Serves a file from a game instance - either as a stream or redirect URL
   * @param gameId Unique identifier for the game
   * @param filePath Path to the file within the game directory
   * @returns Stream result containing either file data or redirect URL
   */
  serveGameFile(gameId: string, filePath: string): Promise<StreamResult>;

  /**
   * Checks if a game file exists in storage
   * @param gameId Unique identifier for the game
   * @param filePath Path to the file within the game directory
   * @returns Promise that resolves to true if file exists, false otherwise
   */
  gameFileExists(gameId: string, filePath: string): Promise<boolean>;

  /**
   * Serves a file from a skeleton - either as a stream or redirect URL
   * @param templateName Name of the skeleton template
   * @param filePath Path to the file within the skeleton directory
   * @returns Stream result containing either file data or redirect URL
   */
  serveSkeletonFile(
    templateName: string,
    filePath: string,
  ): Promise<StreamResult>;

  /**
   * Checks if a skeleton file exists
   * @param templateName Name of the skeleton template
   * @param filePath Path to the file within the skeleton directory
   * @returns Promise that resolves to true if file exists, false otherwise
   */
  skeletonFileExists(templateName: string, filePath: string): Promise<boolean>;

  /**
   * Creates a new game instance by copying files from a skeleton
   * @param templateName Name of the skeleton template
   * @param gameId Unique identifier for the new game
   * @returns Promise that resolves when the new game is created
   */
  createGameFromSkeleton(templateName: string, gameId: string): Promise<void>;

  /**
   * Get all available game skeletons
   * @returns An array of game skeleton metadata
   */
  getGameSkeletons(): Promise<GameSkeleton[]>;

  /**
   * Get a specific game skeleton by template name
   * @param templateName The template name to retrieve
   * @returns The game skeleton data if found, otherwise null
   */
  getGameSkeletonByTemplate(templateName: string): Promise<GameSkeleton | null>;
}
