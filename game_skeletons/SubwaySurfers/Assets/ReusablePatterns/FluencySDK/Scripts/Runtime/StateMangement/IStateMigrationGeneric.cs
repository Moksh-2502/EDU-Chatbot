using FluencySDK.Versioning;
using UnityEngine.Scripting;

namespace FluencySDK.Migrations
{
    /// <summary>
    /// Contract for migrating between specific versions of student state
    /// </summary>
    [Preserve]
    public interface IStateMigrationGeneric<TFrom, TTo> : IStateMigration
        where TFrom : IStudentStateVersion
        where TTo : IStudentStateVersion
    {
        /// <summary>
        /// Migrates from source version to target version
        /// This should be a pure function that doesn't modify the source
        /// </summary>
        /// <param name="source">Source state to migrate from</param>
        /// <returns>New state of target version</returns>
        TTo Migrate(TFrom source);

        /// <summary>
        /// Validates if the source state can be migrated
        /// </summary>
        /// <param name="source">Source state to validate</param>
        /// <returns>True if migration is possible</returns>
        bool CanMigrate(TFrom source);
    }
}