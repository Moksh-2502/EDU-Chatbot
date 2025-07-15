# Using Reusable Game Patterns

This guide explains how to use the reusable game patterns in your game templates.

## Setup

Before using any patterns, make sure they've been built:

```bash
# From the root directory
cd src/reusable-game-patterns
node build.js
```

This will install all dependencies and build all patterns to JavaScript files that can be imported in any project.

## Importing Patterns in Your Game

### For JavaScript/HTML5 Games

Use script tags to import the UMD built version:

```html
<!-- In your HTML file -->
<script src="/src/reusable-game-patterns/dist/fluency-sdk/index.js"></script>
<script>
  // The pattern is available as a global variable
  const generator = new FluencySdk.SimpleGenerator({
    maxFactor: 10,
    questionsPerBlock: 5
  });
  
  // Use the pattern
  generator.getNextQuestionBlock().then(questions => {
    console.log(questions);
  });
</script>
```

### For JavaScript Modules (ES6)

Import the module directly:

```javascript
// In your JS file
import { SimpleGenerator } from '/src/reusable-game-patterns/dist/fluency-sdk/index.esm.js';

// Create an instance
const generator = new SimpleGenerator({
  maxFactor: 10,
  questionsPerBlock: 5
});

// Use the pattern
async function getQuestions() {
  const questions = await generator.getNextQuestionBlock();
  console.log(questions);
}
```

### For Game Engines (Like Phaser)

In your game file:

```javascript
import { SimpleGenerator } from '/src/reusable-game-patterns/dist/fluency-sdk/index.esm.js';

export default class MathProvider {
  constructor() {
    // Create simple generator instance
    this.fluencyGenerator = new SimpleGenerator({
      maxFactor: 10,
      questionsPerBlock: 1
    });
    
    // Game-specific state
    this.gameState = {
      coins: 0,
      highScore: 0
    };
  }
  
  // Implement your game-specific methods
  // that use the fluency-sdk functionality
}
```

## Available Patterns

### Fluency SDK

The Fluency SDK provides a framework for implementing math fluency practice in games.

```javascript
import { 
  SimpleGenerator, 
  ApiGenerator, 
  FluencyGeneratorFactory
} from '/src/reusable-game-patterns/dist/fluency-sdk/index.esm.js';

// Use SimpleGenerator for built-in math questions
const simpleGenerator = new SimpleGenerator({
  maxFactor: 10,
  questionsPerBlock: 5
});

// Or use the factory pattern
const generator = FluencyGeneratorFactory.create('simple', {
  config: {
    maxFactor: 10,
    questionsPerBlock: 5
  }
});
```

See the [Fluency SDK documentation](./fluency-sdk/README.md) for detailed usage instructions.

## Troubleshooting

If you encounter import errors:

1. Ensure the build script has been run
2. Check that your path to the built files is correct
3. For HTTP imports, make sure you're serving the files from a web server
4. If using relative paths, adjust according to your project structure

## Adding New Patterns

If you've created a new reusable pattern:

1. Create a directory under `src/reusable-game-patterns/`
2. Add a package.json with appropriate build scripts
3. Implement the pattern in TypeScript
4. Run the build script again to build all patterns

Your new pattern will be available at `/src/reusable-game-patterns/dist/your-pattern-name/index.js`. 