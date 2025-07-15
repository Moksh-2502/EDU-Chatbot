# Student State Migration System

This document describes the versioned student state system and migration framework for the Learning SDK.

## Overview

The Learning SDK uses a versioned approach to handle evolution of the student state structure. This allows us to:

- Safely upgrade existing student data when models change
- Maintain backward compatibility across different versions
- Test migrations thoroughly before deployment
- Rollback to previous versions if needed

## Migration Flow

```
JSON → Version Detection → Deserialize to Correct Version → Migration Chain → Current StudentState
```

The system automatically detects the version of incoming data and applies the necessary migration chain to bring it to the current version.

## Creating a New Version

When the student state structure needs to change, follow this process:

### Step 1: Create Version Class (Representing Version to Migrate FROM)

Create a frozen representation of the current version before making changes:

```csharp
[Serializable]
[Preserve]
public class StudentStateVX : IStudentStateVersion
{
    public int Version { get; set; } = X;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Copy ALL current properties exactly as they are
    public string SomeProperty { get; set; }
    public List<SomeClass> SomeList { get; set; } = new();

    // Business logic methods from current version
    public void SomeMethod() { /* ... */ }

    #region VX Frozen Data Structures
    /// <summary>
    /// VX enum - FROZEN, do not modify
    /// </summary>
    public enum SomeEnumVX { /* exact copy */ }

    /// <summary>
    /// VX class - FROZEN, do not modify
    /// </summary>
    [Serializable]
    public class SomeClassVX { /* exact copy */ }
    #endregion
}
```

**Critical Rules:**

- Include ALL dependent classes/enums locally in the version class
- Copy exact property names, types, and values from current version
- Mark all classes with `[Serializable]` and `[Preserve]`
- Never modify a frozen version class after creation

### Step 2: Update Current StudentState

Modify the current `StudentState.cs` with your new structure:

```csharp
public class StudentState : IStudentStateVersion
{
    public const int LatestVersion = X+1; // Increment version
    public int Version { get; set; } = LatestVersion;

    // Make your structural changes here
    public string NewProperty { get; set; }
    public List<NewClass> NewList { get; set; } = new();
}
```

### Step 3: Create Migration Class

```csharp
[Preserve]
public class StudentStateMigrationVXToVY : StateMigrationBase<StudentStateVX, StudentState>
{
    public override int FromVersion => X;
    public override int ToVersion => Y;

    protected override StudentState PerformMigration(StudentStateVX source)
    {
        var newState = new StudentState
        {
            Version = ToVersion,
            // Map old properties to new structure
            NewProperty = ConvertOldProperty(source.OldProperty),
            NewList = ConvertOldList(source.OldList)
        };

        return newState;
    }

    private string ConvertOldProperty(string oldValue)
    {
        // Conversion logic
        return transformedValue;
    }
}
```

### Step 4: Register Migration

Add the migration to `MigrationsRegistry.InitializeStudentStatesMigrations()`:

```csharp
IMigrationsRegistry.Instance.RegisterMigration(new StudentStateMigrationVXToVY());
IMigrationsRegistry.Instance.RegisterVersion(new StudentStateVX());
```

## Best Practices

### Creating Frozen Versions

**✅ Do:**

- Include ALL dependent classes/enums locally in version class
- Copy exact property names and types from original
- Add comprehensive business logic methods that existed
- Use descriptive version-specific class names

**❌ Don't:**

- Reference current/shared enums or classes (they can change)
- Modify frozen version classes after creation
- Skip any properties or methods that existed in original

### Writing Migrations

**✅ Do:**

- Inherit from `StateMigrationBase<TFrom, TTo>`
- Handle all enum/class conversions explicitly
- Test with real production data
- Validate migration results

**❌ Don't:**

- Assume backward compatibility without testing
- Skip validation of migrated data
- Use reflection or dynamic typing
- Ignore edge cases or null values

### Testing Migrations

**Essential Tests:**

- Round-trip serialization (V1→V2→V3→VN)
- Data integrity validation (no data loss)
- Performance with large datasets
- Error handling for corrupted data
- Edge cases (empty lists, null values)

## Debugging Migration Issues

### Common Problems

**"Type not found" errors**

- Frozen version classes reference current types
- Solution: Create local copies of all dependent types

**"Property not found" errors**

- Property names changed between versions
- Solution: Map old property names to new ones in migration

**Version mismatch errors**

- Registry doesn't know about version
- Solution: Register version type in `MigrationsRegistry`

**Migration chain breaks**

- Missing migration between sequential versions
- Solution: Ensure complete V1→V2→V3→...→VN chain

### Debugging Tools

```csharp
// Enable migration logging
Debug.Log($"[Migration] {sourceState.GetStateSummary()}");

// Test specific migration paths
var path = registry.GetMigrationPath(fromVersion: 1, toVersion: 3);
```

## Architecture

The migration system consists of:

- **Version Classes**: Frozen representations of each version
- **Migration Classes**: Transform data from one version to the next
- **Registry**: Manages versions and migration chains
- **Base Classes**: `StateMigrationBase<TFrom, TTo>` provides common functionality

This ensures a robust, testable system for evolving the student state structure over time.
