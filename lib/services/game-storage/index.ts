// Export the public interface
export type { IGameStorageService, StreamResult } from './game-storage-service';

// Export the implementations
export { S3GameStorageService } from './s3-game-storage-service';
export { LocalGameStorageService } from './local-game-storage-service';

// Export the factory as the main way to get instances
export { GameStorageFactory } from './game-storage-factory';
