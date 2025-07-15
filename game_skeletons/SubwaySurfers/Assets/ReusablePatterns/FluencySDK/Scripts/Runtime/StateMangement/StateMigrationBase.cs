using FluencySDK.Versioning;
using UnityEngine;
using UnityEngine.Scripting;

namespace FluencySDK.Migrations
{
    /// <summary>
    /// Base class for all state migrations providing common functionality
    /// </summary>
    [Preserve]
    public abstract class StateMigrationBase<TFrom, TTo> : IStateMigrationGeneric<TFrom, TTo>
        where TFrom : IStudentStateVersion
        where TTo : IStudentStateVersion
    {
        public abstract int FromVersion { get; }
        public abstract int ToVersion { get; }

        public virtual bool CanMigrate(TFrom source)
        {
            if (source == null)
            {
                Debug.LogWarning($"[FluencyMigration] Cannot migrate null source state");
                return false;
            }

            if (source.Version != FromVersion)
            {
                Debug.LogWarning($"[FluencyMigration] Version mismatch: expected {FromVersion}, got {source.Version}");
                return false;
            }

            return true;
        }

        public TTo Migrate(TFrom source)
        {
            if (!CanMigrate(source))
            {
                Debug.LogError($"[FluencyMigration] Cannot migrate state: {source?.GetStateSummary() ?? "null"}");
                return default;
            }
            ;
            Debug.Log($"[FluencyMigration] Source: {source.GetStateSummary()}");

            var result = PerformMigration(source);

            if (result != null)
            {
                Debug.Log($"[FluencyMigration] Migration successful: {result.GetStateSummary()}");
                return result;
            }
            else
            {
                Debug.LogError($"[FluencyMigration] Migration failed or produced invalid result");
                return default;
            }
        }

        /// <summary>
        /// Implement the actual migration logic in derived classes
        /// </summary>
        protected abstract TTo PerformMigration(TFrom source);
    }
}