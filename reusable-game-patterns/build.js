/**
 * Build script for reusable-game-patterns
 * Installs dependencies and builds all reusable patterns
 */

const { execSync } = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');

// Colors for console output
const colors = {
  reset: '\x1b[0m',
  bright: '\x1b[1m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
  blue: '\x1b[34m',
  red: '\x1b[31m'
};

console.log(`${colors.bright}${colors.blue}Building Reusable Game Patterns${colors.reset}`);

// Install root dependencies
console.log(`\n${colors.yellow}Installing root dependencies...${colors.reset}`);
try {
  execSync('npm install', { stdio: 'inherit' });
} catch (err) {
  console.error(`${colors.red}Failed to install root dependencies${colors.reset}`);
  process.exit(1);
}

// Get all pattern directories
const patterns = fs.readdirSync(__dirname)
  .filter(dir => {
    try {
      return fs.statSync(path.join(__dirname, dir)).isDirectory() &&
        fs.existsSync(path.join(__dirname, dir, 'package.json')) &&
        dir !== 'node_modules' && 
        dir !== 'dist';
    } catch (err) {
      return false;
    }
  });

// Install dependencies and build each pattern
for (const pattern of patterns) {
  console.log(`\n${colors.bright}${colors.green}Processing pattern: ${pattern}${colors.reset}`);
  
  const patternDir = path.join(__dirname, pattern);
  
  // Install pattern dependencies
  console.log(`${colors.yellow}Installing dependencies for ${pattern}...${colors.reset}`);
  try {
    execSync('npm install', { cwd: patternDir, stdio: 'inherit' });
  } catch (err) {
    console.warn(`${colors.yellow}Warning: Failed to install dependencies for ${pattern}${colors.reset}`);
    // Continue to next pattern
    continue;
  }
  
  // Build pattern (if it has a build script)
  const packageJson = JSON.parse(fs.readFileSync(path.join(patternDir, 'package.json'), 'utf-8'));
  if (packageJson.scripts?.build) {
    console.log(`${colors.yellow}Building ${pattern}...${colors.reset}`);
    try {
      execSync('npm run build', { cwd: patternDir, stdio: 'inherit' });
    } catch (err) {
      console.error(`${colors.red}Failed to build ${pattern}${colors.reset}`);
      // Continue to next pattern
      continue;
    }
  }
}

// Build all patterns together with rollup
console.log(`\n${colors.bright}${colors.green}Building all patterns with rollup...${colors.reset}`);
try {
  execSync('npm run build', { stdio: 'inherit' });
  console.log(`\n${colors.bright}${colors.green}Build completed successfully!${colors.reset}`);
} catch (err) {
  console.error(`${colors.red}Failed to build patterns with rollup${colors.reset}`);
  process.exit(1);
}

// Update all templates with the latest pattern builds
console.log(`\n${colors.bright}${colors.green}Updating templates with latest patterns...${colors.reset}`);
try {
  execSync('node update-templates.js', { stdio: 'inherit' });
} catch (err) {
  console.error(`${colors.red}Failed to update templates: ${err.message}${colors.reset}`);
  // Don't exit, as the build itself was successful
} 