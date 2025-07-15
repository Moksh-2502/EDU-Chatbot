import resolve from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';
import typescript from '@rollup/plugin-typescript';
import path from 'node:path';
import fs from 'node:fs';

// Function to create pattern build configs
function createPatternConfig(patternName) {
  const patternDir = path.join(__dirname, patternName);
  const packageJson = JSON.parse(fs.readFileSync(path.join(patternDir, 'package.json'), 'utf-8'));
  
  return {
    input: path.join(patternDir, 'index.ts'),
    output: [
      {
        file: path.join('dist', patternName, 'index.js'),
        format: 'umd',
        name: packageJson.name.replace(/@[^/]+\/|[-/]/g, ''),
        sourcemap: true
      },
      {
        file: path.join('dist', patternName, 'index.esm.js'),
        format: 'es',
        sourcemap: true
      }
    ],
    plugins: [
      resolve(),
      commonjs(),
      typescript({
        tsconfig: 'tsconfig.json',
        declaration: true,
        declarationDir: path.join('dist', patternName),
        include: [`${patternName}/**/*.ts`]
      })
    ]
  };
}

// Get all pattern directories that have package.json files
const patterns = fs.readdirSync(__dirname)
  .filter(dir => {
    try {
      return fs.statSync(path.join(__dirname, dir)).isDirectory() &&
        fs.existsSync(path.join(__dirname, dir, 'package.json'));
    } catch (err) {
      return false;
    }
  });

// Create config for each pattern
export default patterns.map(createPatternConfig); 