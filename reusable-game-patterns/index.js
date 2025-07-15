/**
 * Reusable Game Patterns
 * Exports all reusable patterns for game templates to use
 */

// Re-export patterns
export * as fluencySdk from './dist/fluency-sdk/index.esm.js';

// Export utility functions
export function loadPattern(patternName) {
  try {
    // Dynamic import of the pattern
    return import(`./dist/${patternName}/index.esm.js`);
  } catch (err) {
    console.error(`Failed to load pattern: ${patternName}`, err);
    throw err;
  }
} 