# Fluency SDK

This is a local copy of the Fluency SDK for use in the Math Monsters game template.

## Source

This file is built from the source code in `src/reusable-game-patterns/fluency-sdk/`.

## Updating

To update this copy with the latest version of the SDK:

1. Build the SDK:
   ```
   cd src/reusable-game-patterns
   node build.js
   ```

2. Copy the built file:
   ```
   cp src/reusable-game-patterns/dist/fluency-sdk/index.esm.js src/templates/mathmonsters/js/lib/fluency-sdk/
   ```

## Why a Local Copy?

A local copy ensures:

1. The template is self-contained and works even if the reusable patterns directory is not built
2. Path resolution issues are avoided
3. The template can be used as a standalone example without dependencies

For development, you can still use the centralized version from `src/reusable-game-patterns/dist/fluency-sdk/index.esm.js` by changing the import path in MathProvider.js. 