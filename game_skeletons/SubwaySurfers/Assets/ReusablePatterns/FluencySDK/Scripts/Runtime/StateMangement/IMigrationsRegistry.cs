using System.Collections.Generic;
using FluencySDK.Versioning;

namespace FluencySDK.Migrations
{
    public interface IMigrationsRegistry
    {
        static IMigrationsRegistry Instance { get; set; } = new MigrationsRegistry();
        void RegisterMigration(IStateMigration migration);
        void RegisterVersion(IStudentStateVersion version);

        bool TryGetVersionType(int version, out System.Type versionRuntimeType);
    }

    public interface IStudentStateMigrationService
    {
        static IStudentStateMigrationService Instance { get; set; }
        StudentState CreateNewState();
        StudentState MigrateToLatest(IStudentStateVersion source);
        IList<IStateMigration> GetMigrationPath(int fromVersion, int toVersion);
    }
}