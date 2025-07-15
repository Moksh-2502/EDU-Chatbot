using System;
using System.Collections.Generic;
using FluencySDK.Versioning;
using FluencySDK.Migrations;
using UnityEngine;
using UnityEngine.Scripting;

namespace FluencySDK.Migrations
{
    /// <summary>
    /// Registry for managing student state versions and their migrations
    /// </summary>
    [Preserve]
    public class MigrationsRegistry : IMigrationsRegistry, IStudentStateMigrationService
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializeStudentStatesMigrations()
        {
            var registry = new MigrationsRegistry();
            IMigrationsRegistry.Instance = registry;
            IStudentStateMigrationService.Instance = registry;

            IMigrationsRegistry.Instance.RegisterMigration(new StudentStateMigrationV1ToV2());
            IMigrationsRegistry.Instance.RegisterMigration(new StudentStateMigrationV2ToV3());
            IMigrationsRegistry.Instance.RegisterMigration(new StudentStateMigrationV3ToV4());

            IMigrationsRegistry.Instance.RegisterVersion(new StudentStateV1());
            IMigrationsRegistry.Instance.RegisterVersion(new StudentStateV2());
            IMigrationsRegistry.Instance.RegisterVersion(new StudentStateV3());
            IMigrationsRegistry.Instance.RegisterVersion(new StudentState());
        }


        private readonly IDictionary<(int from, int to), IStateMigration> _registeredMigrations =
            new Dictionary<(int from, int to), IStateMigration>();

        private readonly IDictionary<int, Type> _registeredVersions =
            new Dictionary<int, Type>();

        public StudentState CreateNewState() => new();

        /// <summary>
        /// Registers a migration between two versions
        /// </summary>
        public void RegisterMigration(IStateMigration migration)
        {
            var key = (migration.FromVersion, migration.ToVersion);
            if (_registeredMigrations.TryGetValue(key, out var existing))
            {
                Debug.LogError($"[FluencyMigration] Migration from {migration.FromVersion} to {migration.ToVersion} already exists.");
            }
            else
            {
                _registeredMigrations[key] = migration;
            }
        }

        public void RegisterVersion(IStudentStateVersion version)
        {
            if (_registeredVersions.TryGetValue(version.Version, out var type))
            {
                Debug.LogError($"[FluencyMigration] Version {version.Version} already registered as type {type}");
                return;
            }
            _registeredVersions[version.Version] = version.GetType();
            Debug.Log($"[FluencyMigration] Registered version {version.Version} as {_registeredVersions[version.Version]}");
        }

        public bool TryGetVersionType(int version, out Type versionRuntimeType)
        {
            return _registeredVersions.TryGetValue(version, out versionRuntimeType);
        }


        /// <summary>
        /// Gets the migration path from source version to target version
        /// </summary>
        public IList<IStateMigration> GetMigrationPath(int fromVersion, int toVersion)
        {
            if (fromVersion == toVersion)
                return Array.Empty<IStateMigration>();

            if (fromVersion > toVersion)
            {
                return null;
            }

            var path = new List<IStateMigration>();
            var current = fromVersion;

            while (current < toVersion)
            {
                var nextVersion = current + 1;
                var key = (current, nextVersion);
                if (_registeredMigrations.TryGetValue(key, out var migration) == false || migration == null)
                {
                    return null;
                }

                path.Add(migration);
                current = nextVersion;
            }

            return path;
        }

        private StudentState MigrateToLatestInternal(IStudentStateVersion source)
        {
            if (source == null)
            {
                return null;
            }

            if (source.Version == StudentState.LatestVersion)
            {
                // Already current version, convert if needed
                if (source is StudentState currentState)
                    return currentState;

                // This shouldn't happen if versions are managed correctly
                Debug.LogWarning($"[FluencyMigration] Version {StudentState.LatestVersion} state is not of type {nameof(StudentState)}");
                return null;
            }

            if (source.Version > StudentState.LatestVersion)
            {
                return null;
            }

            var migrationPath = GetMigrationPath(source.Version, StudentState.LatestVersion);
            if (migrationPath == null)
            {
                return null;
            }

            return ExecuteMigrationChain(source, migrationPath);
        }

        /// <summary>
        /// Migrates a state to the latest version
        /// </summary>
        public StudentState MigrateToLatest(IStudentStateVersion source)
        {
            return MigrateToLatestInternal(source) ?? CreateNewState();
        }

        private StudentState ExecuteMigrationChain(IStudentStateVersion source, IList<IStateMigration> migrationPath)
        {
            IStudentStateVersion current = source;

            foreach (var migration in migrationPath)
            {
                try
                {
                    // Use reflection to call the migration
                    var migrateMethod = migration.GetType().GetMethod("Migrate");
                    current = migrateMethod?.Invoke(migration, new object[] { current }) as IStudentStateVersion;

                    if (current == null)
                    {
                        Debug.LogError($"[FluencyMigration] Migration {migration.FromVersion} -> {migration.ToVersion} returned null");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FluencyMigration] Migration {migration.FromVersion} -> {migration.ToVersion} failed: {ex.Message}");
                    return null;
                }
            }

            return current as StudentState;
        }
    }
}