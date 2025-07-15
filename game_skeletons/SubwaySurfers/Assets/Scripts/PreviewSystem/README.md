# Generic Preview System

## Overview

The Generic Preview System is a production-quality, modular architecture designed to handle previewing of various game objects (Characters, Items, Themes, etc.) in a consistent and efficient manner. It replaces the inefficient character preview implementation in LoadoutState and provides a solid foundation for future preview needs.

## Architecture

### Core Components

#### 1. **Interfaces** (`IPreviewSystem.cs`)
- `IPreviewable` - Marker interface for previewable objects
- `IPreviewData<T>` - Generic data wrapper interface
- `IPreviewController<T>` - Generic controller interface  
- `IPreviewUI<T>` - Generic UI interface

#### 2. **Base Classes**
- `BasePreviewController<T>` - Common asset loading, cleanup, animation logic
- `BasePreviewUI<T>` - Common UI animations, display logic
- `PreviewAssetManager` - Centralized Addressable asset management

#### 3. **Specialized Implementations**

**Character Preview:**
- `CharacterPreviewData` - Character-specific preview data wrapper
- `CharacterPreviewController` - Character preview controller with accessory support
- `CharacterPreviewUI` - Character-specific UI with accessory navigation

**Item Preview:**
- `ItemPreviewData` - Item-specific preview data wrapper  
- `RewardPreviewController` - Refactored to inherit from base system
- `RewardPreviewUI` - Refactored to inherit from base system

## Key Features

### ✅ **Separation of Concerns**
- LoadoutState now focuses purely on loadout logic
- Preview logic is completely separated and reusable

### ✅ **Efficient Asset Management**
- Centralized Addressable asset loading/cleanup
- Proper memory management with async/await patterns
- No more manual asset handle management scattered throughout code

### ✅ **Performance Optimizations**
- Character rotation moved to dedicated Update loop in preview controller
- T-pose flash prevention built into the system
- Optimized asset lifecycle management

### ✅ **Reusability & Extensibility**
- Generic base classes can be extended for any preview type
- Easy to add new preview types (themes, powerups, etc.)
- Consistent API across all preview implementations

### ✅ **Production Quality**
- Proper error handling and edge case management
- Comprehensive logging for debugging
- Clean async/await patterns throughout
- Follows Unity and C# best practices

## Usage Examples

### Character Preview

```csharp
// Basic character preview
var characterData = CharacterPreviewData.CreateFromCharacterName("Trash Cat");
await characterPreviewController.ShowCharacterPreviewAsync(characterData);

// Character preview with accessory and positioning
var characterData = CharacterPreviewData.Create(
    character: myCharacter,
    position: Vector3.zero,
    accessoryIndex: 2
);
await characterPreviewController.ShowPreviewAsync(characterData);

// Change accessory without reloading character
characterPreviewController.ChangeAccessory(1);
```

### Item Preview

```csharp
// Basic item preview
var itemData = ItemPreviewData.Create(myItemData);
await rewardPreviewController.ShowPreviewAsync(itemData);

// Custom item preview with text
var itemData = ItemPreviewData.Create(myItemData)
    .WithCustomText("Custom Title!", "Custom description text");
await rewardPreviewController.ShowPreviewAsync(itemData);
```

## LoadoutState Integration

The LoadoutState has been updated to use the new preview system while maintaining backward compatibility:

- **Preview System Mode**: Uses `CharacterPreviewController` for efficient character loading
- **Legacy Mode**: Falls back to original implementation if preview controller is not assigned
- **Seamless Integration**: Existing accessory UI continues to work with both modes

## Migration Guide

### For Existing Characters:
1. Add `CharacterPreviewController` component to LoadoutState
2. Assign the `previewHolder` transform in the inspector
3. Set `_usePreviewSystem = true` (default)

### For New Preview Types:
1. Create a data wrapper class implementing `IPreviewData<T>`
2. Create a specialized controller inheriting from `BasePreviewController<T>`
3. Create a specialized UI inheriting from `BasePreviewUI<T>`
4. Implement the abstract methods for your specific preview needs

## Benefits Over Previous Implementation

### Before:
- ❌ Mixed responsibilities in LoadoutState
- ❌ Manual Addressable asset management 
- ❌ Inefficient character rotation in Tick()
- ❌ Poor error handling
- ❌ Not reusable elsewhere
- ❌ Complex async logic mixed with UI logic

### After:
- ✅ Clean separation of concerns
- ✅ Centralized asset management
- ✅ Optimized performance
- ✅ Comprehensive error handling  
- ✅ Reusable across the entire codebase
- ✅ Production-quality architecture
- ✅ Easy to test and maintain
- ✅ Extensible for future needs

## File Structure

```
Assets/Scripts/UI/PreviewSystem/
├── IPreviewSystem.cs                    # Core interfaces
├── PreviewAssetManager.cs               # Asset management
├── BasePreviewController.cs             # Base controller
├── BasePreviewUI.cs                     # Base UI
├── Character/
│   ├── CharacterPreviewData.cs          # Character data wrapper
│   ├── CharacterPreviewController.cs    # Character controller
│   └── CharacterPreviewUI.cs            # Character UI
├── Item/
│   └── ItemPreviewData.cs               # Item data wrapper
└── README.md                            # This documentation
```

## Future Enhancements

The system is designed to easily support:
- **Theme Preview System** - Preview environment themes
- **Powerup Preview System** - Preview consumable effects
- **Outfit Preview System** - Preview complete character outfits
- **Vehicle Preview System** - Preview vehicles or boards
- **Weapon Preview System** - Preview weapons and tools

Each new preview type only requires implementing the three core classes (Data, Controller, UI) and inheriting from the base system. 