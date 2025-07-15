namespace SubwaySurfers.Assets.Scripts.Tracks
{
    /// <summary>
    /// Interface for providing track runner configuration values.
    /// This allows the difficulty system to control track speed parameters
    /// without TrackManager being directly coupled to the difficulty system.
    /// </summary>
    public interface ITrackRunnerConfigProvider
    {
        static ITrackRunnerConfigProvider Instance { get; set; }
        /// <summary>
        /// Minimum speed at which the character starts moving
        /// </summary>
        float MinSpeed { get; }
        
        /// <summary>
        /// Maximum speed that the character can reach
        /// </summary>
        float MaxSpeed { get; }
        
        /// <summary>
        /// Rate at which speed increases over time (units per second)
        /// </summary>
        float Acceleration { get; }
        
        /// <summary>
        /// Step value used for score multiplier calculations
        /// Typically remains constant across difficulty levels
        /// </summary>
        int SpeedStep { get; }

        /// <summary>
        /// Density of obstacles on the track
        /// </summary>
        float ObstacleDensity { get; }

        /// <summary>
        /// Sets the obstacle density override. 
        /// Pass null to restore default density from difficulty config.
        /// </summary>
        /// <param name="density">Obstacle density (0.0 to 1.0) or null to restore default</param>
        void SetObstacleDensityOverride(float? density);

        /// <summary>
        /// Sets the acceleration override.
        /// Pass null to restore default acceleration from difficulty config.
        /// </summary>
        /// <param name="acceleration">Acceleration value or null to restore default</param>
        void SetAccelerationOverride(float? acceleration);

        // ─── New properties for animation multipliers ────────────────────
        /// <summary>
        /// Base multiplier to compute jump animation speed 
        /// (used in CharacterInputController.Jump logic).
        /// </summary>
        float JumpAnimSpeedRatio { get; }

        /// <summary>
        /// Base multiplier to compute slide animation speed.
        /// </summary>
        float SlideAnimSpeedRatio { get; }

        /// <summary>
        /// Gets the spawn frequency for a specific consumable type based on difficulty
        /// </summary>
        /// <param name="consumableType">The type of consumable</param>
        /// <returns>Frequency multiplier for the consumable type</returns>
        float GetConsumableSpawnFrequency(Consumable.ConsumableType consumableType);
    }
}
