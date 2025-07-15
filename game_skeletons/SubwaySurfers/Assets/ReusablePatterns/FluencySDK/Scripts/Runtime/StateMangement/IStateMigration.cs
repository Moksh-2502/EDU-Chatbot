namespace FluencySDK.Migrations
{
    public interface IStateMigration
    {
        /// <summary>
        /// Source version this migration handles
        /// </summary>
        int FromVersion { get; }

        /// <summary>
        /// Target version this migration produces
        /// </summary>
        int ToVersion { get; }
    }
}