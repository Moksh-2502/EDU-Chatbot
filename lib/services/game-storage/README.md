# Game Storage Service

This module provides a unified abstraction for game asset storage, handling both game instances and game skeletons with multiple implementations:

1. **S3GameStorageService**: Stores game assets and skeletons in Amazon S3
2. **LocalGameStorageService**: Stores game assets and skeletons in the local filesystem

## Usage

```typescript
import { GameStorageFactory } from "@/lib/services/game-storage/game-storage-factory";

// Get the default storage implementation (determined by environment variables)
const gameStorage = GameStorageFactory.createDefaultStorage();

// Or create a specific implementation
const s3Storage = GameStorageFactory.createS3Storage("my-bucket-name");
const localStorage = GameStorageFactory.createLocalStorage("/path/to/storage");

// Working with game instances
await gameStorage.uploadGameDirectory("/path/to/game/source", "game-id");
const { stream, contentType, contentLength } =
  await gameStorage.getGameFileAsStream("game-id", "assets/image.png");
const exists = await gameStorage.gameFileExists("game-id", "index.html");

// Working with game skeletons
const skeletons = await gameStorage.getGameSkeletons();
const skeleton = await gameStorage.getGameSkeletonByTemplate("SubwaySurfers");
const { stream } = await gameStorage.getSkeletonFileAsStream(
  "SubwaySurfers",
  "index.html"
);
const exists = await gameStorage.skeletonFileExists(
  "SubwaySurfers",
  "index.html"
);

// Create a new game from a skeleton
await gameStorage.createGameFromSkeleton("SubwaySurfers", "new-game-id");
```

## Environment Variables

- `STORAGE_BUCKET`: S3 bucket name for game storage
- `USE_LOCAL_STORAGE`: Set to "true" to use local storage instead of S3
- `LOCAL_GAME_STORAGE_PATH`: Path for local game instance storage (optional, defaults to 'games' in project root)
- `DEFAULT_GAME_SKELETON`: Default skeleton to load on the home page (defaults to 'SubwaySurfers')

## Storage Structure

### Local Storage

- Game instances: `{LOCAL_GAME_STORAGE_PATH}/{gameId}/`
- Game skeletons: `{project_root}/game_skeletons/{templateName}/`
  - Skeletons may have a `skeleton-manifest.json` that specifies a `copy_directory`

### S3 Storage

- Game instances: `{bucket}/{gameId}/`
- Game skeletons: `{bucket}/skeletons/{templateName}/`

## Implementing a New Storage Backend

1. Create a new class that implements the `IGameStorageService` interface
2. Implement all required methods for both game instances and skeletons
3. Add the implementation to `GameStorageFactory`
4. Update this README to document the new backend
