using System;
using UnityEngine.Scripting;

namespace FluencySDK.Versioning
{
    /// <summary>
    /// Base interface for all versioned student state representations
    /// </summary>
    [Preserve]
    public interface IStudentStateVersion
    {
        /// <summary>
        /// Version number of this state representation
        /// </summary>
        int Version { get; }

        /// <summary>
        /// When this state was created (used for migration debugging)
        /// </summary>
        DateTime CreatedAt { get; }

        /// <summary>
        /// Gets a summary of this state for logging/debugging
        /// </summary>
        string GetStateSummary();
    }
} 