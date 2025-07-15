using System;
using System.Collections.Generic;
using System.Linq;

namespace FluencySDK
{
    /// <summary>
    /// Represents a single fact within a specific learning pool
    /// </summary>
    public class FactPoolItem
    {
        public string FactId { get; set; }
        public DateTime? LastAskedTime { get; set; }  // null = never asked (highest priority)
        public int ConsecutiveCorrect { get; set; }   // per-pool counter (resets between pools)
        public int ConsecutiveIncorrect { get; set; } // per-pool counter (resets between pools)

        public FactPoolItem()
        {
        }

        public FactPoolItem(string factId)
        {
            FactId = factId;
        }
    }

    /// <summary>
    /// Manages facts within a specific learning mode pool (Assessment, Mastery, or Fluency)
    /// </summary>
    public class FactPool
    {
        public List<FactPoolItem> Items { get; set; } = new List<FactPoolItem>();
        public int ConsecutiveCorrect { get; set; }
        public int ConsecutiveIncorrect { get; set; }

        /// <summary>
        /// Gets an existing pool item or creates a new one if it doesn't exist
        /// </summary>
        public FactPoolItem GetOrCreateItem(string factId)
        {
            var item = Items.FirstOrDefault(x => x.FactId == factId);
            if (item == null)
            {
                item = new FactPoolItem(factId);
                Items.Add(item);
            }
            return item;
        }

        /// <summary>
        /// Removes a fact from this pool
        /// </summary>
        public bool RemoveItem(string factId)
        {
            var item = Items.FirstOrDefault(x => x.FactId == factId);
            if (item != null)
            {
                Items.Remove(item);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if a fact exists in this pool
        /// </summary>
        public bool ContainsFact(string factId)
        {
            return Items.Any(x => x.FactId == factId);
        }

        /// <summary>
        /// Gets all fact IDs in this pool
        /// </summary>
        public IEnumerable<string> GetFactIds()
        {
            return Items.Select(x => x.FactId);
        }

        /// <summary>
        /// Updates pool-level streak counters based on answer correctness
        /// </summary>
        public void UpdatePoolStreak(string factId, bool isCorrect)
        {
            var item = GetOrCreateItem(factId);
            
            if (isCorrect)
            {
                ConsecutiveCorrect++;
                ConsecutiveIncorrect = 0;
                item.ConsecutiveCorrect++;
                item.ConsecutiveIncorrect = 0;
            }
            else
            {
                ConsecutiveIncorrect++;
                ConsecutiveCorrect = 0;
                item.ConsecutiveIncorrect++;
                item.ConsecutiveCorrect = 0;
            }
        }

        /// <summary>
        /// Resets pool-level streak counters
        /// </summary>
        public void ResetPoolStreak()
        {
            ConsecutiveCorrect = 0;
            ConsecutiveIncorrect = 0;
            
            foreach (var item in Items)
            {
                item.ConsecutiveCorrect = 0;
                item.ConsecutiveIncorrect = 0;
            }
        }
    }
} 