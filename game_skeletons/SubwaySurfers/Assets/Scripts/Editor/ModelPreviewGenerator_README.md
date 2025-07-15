# Model Preview Generator Tool

A Unity Editor tool for generating preview images from selected 3D models, prefabs, and GameObjects.

## Features

- **Simple and Fast**: Uses Unity's built-in AssetPreview system for reliable preview generation
- **Customizable Settings**: Preview size and output location
- **Batch Processing**: Generate previews for multiple objects at once
- **Folder Support**: Recursively process entire folder structures
- **Format Support**: Works with .fbx, .obj, .blend, .dae, .3ds, .max files and prefabs

## How to Use

### Method 1: Through Tools Menu
1. Go to `Tools > Model Preview Generator` in the Unity menu bar
2. The Preview Generator window will open
3. Select your models/prefabs/folders in the Project window
4. Click "Refresh" in the tool window to load selected objects
5. Adjust settings as needed
6. Click "Generate Previews"

### Method 2: Right-Click Context Menu
1. Select one or more models/prefabs/folders in the Project window
2. Right-click and select "Generate Preview Images"
3. The Preview Generator window will open with your selection already loaded
4. Adjust settings and click "Generate Previews"

### Method 3: Folder Selection (Batch Processing)
1. Select one or more folders in the Project window
2. The tool will automatically find all models within those folders
3. Use "Include Subfolders" setting to control recursive search
4. Perfect for processing entire model libraries at once

## Settings

### Basic Settings
- **Output Folder**: Where preview images will be saved (default: `Assets/GeneratedPreviews`)
- **Preview Size**: Resolution of generated images (64-2048 pixels)

### Folder Processing
- **Include Subfolders**: When folders are selected, recursively search all subfolders for models
- **Show in Project Window**: Automatically select the output folder after generation

## Supported File Types

- Unity Prefabs (`.prefab`)
- FBX Models (`.fbx`)
- OBJ Models (`.obj`)
- Blender Files (`.blend`)
- Collada Files (`.dae`)
- 3DS Max Files (`.3ds`, `.max`)

## Output

Generated images are saved as PNG files with the naming convention: `{ObjectName}_preview.png`

If a file with the same name already exists, Unity will automatically append a number to make it unique.

## Tips

1. **Batch Processing**: Select multiple objects to generate all previews at once
2. **Folder Processing**: Select entire folders to process all models within them automatically
3. **Recursive Search**: Enable "Include Subfolders" to process nested folder structures
4. **Preview Size**: Higher sizes give better quality but take longer to process
5. **Unity Previews**: The tool uses Unity's built-in preview system, so it matches what you see in the Project window

## Troubleshooting

- **No Preview Generated**: Ensure the selected object is a valid 3D model or prefab
- **Poor Quality**: Try increasing the preview size setting
- **Preview Not Loading**: Unity sometimes needs time to generate asset previews - the tool now waits up to 2 seconds for better quality previews
- **Empty Folders**: Make sure the selected folders contain valid 3D model files
- **Still Low Quality**: Try selecting the model in the Project window first to let Unity generate a proper preview, then run the tool

## Technical Notes

- Uses `SubwaySurfers.Editor` namespace following project conventions
- Leverages Unity's built-in `AssetPreview.GetAssetPreview()` system for reliable results
- Follows Unity Editor best practices for progress bars and UI
- Thread-safe and handles exceptions gracefully
- Simple, efficient, and maintains consistency with Unity's Project window previews 