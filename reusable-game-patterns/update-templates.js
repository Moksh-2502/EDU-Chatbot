/**
 * Script to update all templates with the latest versions of reusable patterns
 * Runs after the build process to ensure templates have the latest code
 */

const fs = require('node:fs');
const path = require('node:path');
const { execSync, spawnSync } = require('node:child_process');

// Colors for console output
const colors = {
  reset: '\x1b[0m',
  bright: '\x1b[1m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
  blue: '\x1b[34m',
  red: '\x1b[31m'
};

// Template pattern mappings - which templates use which patterns
const TEMPLATE_PATTERNS = {
  // Format: 'template-name': { 'pattern-name': ['destination-path-in-template', ...] }
  'mathmonsters': {
    'fluency-sdk': ['js/lib/fluency-sdk']
  }
  // Add more templates and patterns as needed
};

// Root directories
const ROOT_DIR = path.resolve(__dirname, '../..');
const TEMPLATES_DIR = path.join(ROOT_DIR, 'src/templates');
const PATTERNS_DIR = path.join(ROOT_DIR, 'src/reusable-game-patterns');

// Main function
async function updateTemplates() {
  console.log(`${colors.bright}${colors.blue}Updating templates with latest reusable patterns${colors.reset}`);
  
  // Make sure the patterns are built
  console.log(`${colors.yellow}Checking if patterns are built...${colors.reset}`);
  const distDir = path.join(PATTERNS_DIR, 'dist');
  if (!fs.existsSync(distDir)) {
    console.log(`${colors.yellow}Building patterns...${colors.reset}`);
    const result = spawnSync('C:\\Program Files\\nodejs\\node.exe', ['build.js'], { cwd: PATTERNS_DIR, stdio: 'inherit', encoding: 'utf-8' });

    if (result.error) {
      console.error(`${colors.red}Failed to start pattern build process: ${result.error.message}${colors.reset}`);
      throw result.error;
    }

    if (result.status !== 0) {
      console.error(`${colors.red}Pattern build process failed with status ${result.status}.${colors.reset}`);
      const buildError = new Error(`Command "node build.js" failed with exit code ${result.status}`);
      buildError.status = result.status;
      throw buildError;
    }
    console.log(`${colors.green}Patterns built successfully.${colors.reset}`);
  }
  
  // Update each template
  for (const [templateName, patterns] of Object.entries(TEMPLATE_PATTERNS)) {
    const templateDir = path.join(TEMPLATES_DIR, templateName);
    if (!fs.existsSync(templateDir)) {
      console.log(`${colors.yellow}Template ${templateName} not found, skipping...${colors.reset}`);
      continue;
    }
    
    console.log(`${colors.bright}${colors.green}Updating template: ${templateName}${colors.reset}`);
    
    // Copy each pattern to its destination in the template
    for (const [patternName, destinations] of Object.entries(patterns)) {
      const patternDistDir = path.join(PATTERNS_DIR, 'dist', patternName);
      if (!fs.existsSync(patternDistDir)) {
        console.log(`${colors.red}Pattern ${patternName} not built, skipping...${colors.reset}`);
        continue;
      }
      
      console.log(`${colors.yellow}Copying ${patternName} to ${templateName}...${colors.reset}`);
      
      // Copy to each destination
      for (const destination of destinations) {
        const destDir = path.join(templateDir, destination);
        
        // Create destination directory if it doesn't exist
        if (!fs.existsSync(destDir)) {
          fs.mkdirSync(destDir, { recursive: true });
        }
        
        // Copy the pattern files
        copyFiles(patternDistDir, destDir, patternName, templateName);
      }
    }
  }
  
  console.log(`${colors.bright}${colors.green}Templates updated successfully!${colors.reset}`);
}

// Helper function to copy files
function copyFiles(sourceDir, destDir, patternName, templateName) {
  // Copy the main bundle files (index.js, index.esm.js, etc.)
  try {
    const files = fs.readdirSync(sourceDir)
      .filter(file => file.endsWith('.js') || file.endsWith('.map'));
    
    for (const file of files) {
      const sourcePath = path.join(sourceDir, file);
      const destPath = path.join(destDir, file);
      
      // Only copy if the file is a file, not a directory
      if (fs.statSync(sourcePath).isFile()) {
        fs.copyFileSync(sourcePath, destPath);
        console.log(`  ${colors.green}Copied ${file} to ${templateName}/${path.relative(TEMPLATES_DIR, destDir)}${colors.reset}`);
      }
    }
  } catch (err) {
    console.error(`${colors.red}Error copying ${patternName} to ${templateName}: ${err.message}${colors.reset}`);
  }
}

// Run the update process
updateTemplates().catch(err => {
  console.error(`${colors.red}Error updating templates: ${err.message}${colors.reset}`);
  process.exit(1);
}); 