using UnityEngine;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.UI
{
    /// <summary>
    /// Interface for fact set row items that can display data and be managed by pooling systems
    /// </summary>
    /// <typeparam name="TData">The type of data this row can display</typeparam>
    public interface IFactSetRowItem<in TData>
    {
        /// <summary>
        /// Updates the row's display with the provided data
        /// </summary>
        /// <param name="data">The data to display in this row</param>
        void Repaint(TData data);
        
        /// <summary>
        /// Gets the GameObject associated with this row for pooling operations
        /// </summary>
        GameObject GameObject { get; }
        
        /// <summary>
        /// Gets the Transform associated with this row for hierarchy operations
        /// </summary>
        Transform Transform { get; }
    }
} 