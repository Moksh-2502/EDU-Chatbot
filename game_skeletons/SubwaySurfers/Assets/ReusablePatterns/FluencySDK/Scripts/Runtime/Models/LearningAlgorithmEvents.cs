using System;
using System.Collections.Generic;

namespace FluencySDK
{
    /// <summary>
    /// Event data for when an individual fact moves between stages
    /// </summary>
    public class IndividualFactProgressionInfo : ILearningAlgorithmEvent
    {
        public string FactId { get; }
        public string FactSetId { get; }
        public string FromStageId { get; }
        public string ToStageId { get; } // null means completed/removed
        public AnswerType AnswerType { get; }
        public int ConsecutiveCount { get; }
        public DateTimeOffset Timestamp { get; }

        public string EventName => "individual_fact_progression";

        public IndividualFactProgressionInfo(string factId, string factSetId, string fromStageId, 
            string toStageId, AnswerType answerType, int consecutiveCount, DateTimeOffset timestamp)
        {
            FactId = factId;
            FactSetId = factSetId;
            FromStageId = fromStageId;
            ToStageId = toStageId;
            AnswerType = answerType;
            ConsecutiveCount = consecutiveCount;
            Timestamp = timestamp;
        }

        public Dictionary<string, object> ToAnalyticsData()
        {
            return new Dictionary<string, object>
            {
                ["fact_id"] = FactId,
                ["fact_set_id"] = FactSetId,
                ["from_stage"] = FromStageId ?? "unknown",
                ["to_stage"] = ToStageId ?? "completed",
                ["answer_type"] = AnswerType.ToString(),
                ["consecutive_count"] = ConsecutiveCount,
                ["timestamp"] = Timestamp.ToUnixTimeSeconds()
            };
        }
    }

    /// <summary>
    /// Event data for when a fact set reaches review stage - all facts at Review, Repetition, or Mastered
    /// </summary>
    public class FactSetReviewReadyInfo : ILearningAlgorithmEvent
    {
        public string FactSetId { get; }
        public string NextFactSetId { get; }
        public int TotalAnswerCount { get; }
        public int TotalFactsCount { get; }
        public DateTimeOffset Timestamp { get; }

        public string EventName => "fact_set_review_ready";

        public FactSetReviewReadyInfo(string factSetId, string nextFactSetId, int totalAnswerCount, int totalFactsCount, DateTimeOffset timestamp)
        {
            FactSetId = factSetId;
            NextFactSetId = nextFactSetId;
            TotalAnswerCount = totalAnswerCount;
            TotalFactsCount = totalFactsCount;
            Timestamp = timestamp;
        }

        public Dictionary<string, object> ToAnalyticsData()
        {
            return new Dictionary<string, object>
            {
                ["fact_set_id"] = FactSetId,
                ["next_fact_set_id"] = NextFactSetId,
                ["total_answer_count"] = TotalAnswerCount,
                ["total_facts_count"] = TotalFactsCount,
                ["timestamp"] = Timestamp.ToUnixTimeSeconds()
            };
        }
    }

    /// <summary>
    /// Event data for when a fact set is fully completed - all facts mastered
    /// </summary>
    public class FactSetCompletionInfo : ILearningAlgorithmEvent
    {
        public string FactSetId { get; }
        public string NextFactSetId { get; }
        public int TotalAnswerCount { get; }
        public int TotalFactsCount { get; }
        public DateTimeOffset Timestamp { get; }

        public string EventName => "fact_set_completion";

        public FactSetCompletionInfo(string factSetId, string nextFactSetId, int totalAnswerCount, int totalFactsCount, DateTimeOffset timestamp)
        {
            FactSetId = factSetId;
            NextFactSetId = nextFactSetId;
            TotalAnswerCount = totalAnswerCount;
            TotalFactsCount = totalFactsCount;
            Timestamp = timestamp;
        }

        public Dictionary<string, object> ToAnalyticsData()
        {
            return new Dictionary<string, object>
            {
                ["completed_fact_set_id"] = FactSetId,
                ["next_fact_set_id"] = NextFactSetId,
                ["total_answer_count"] = TotalAnswerCount,
                ["total_facts_count"] = TotalFactsCount,
                ["timestamp"] = Timestamp.ToUnixTimeSeconds()
            };
        }
    }

    public class BulkPromotionInfo : ILearningAlgorithmEvent
    {
        public string FactSetId { get; }
        public int PromotedFactsCount { get; }
        public int ConsecutiveCorrectCount { get; }
        public float CoveragePercentage { get; }
        public DateTimeOffset Timestamp { get; }

        public string EventName => "bulk_promotion";

        public BulkPromotionInfo(string factSetId, int promotedFactsCount, int consecutiveCorrectCount, float coveragePercentage, DateTimeOffset timestamp)
        {
            FactSetId = factSetId;
            PromotedFactsCount = promotedFactsCount;
            ConsecutiveCorrectCount = consecutiveCorrectCount;
            CoveragePercentage = coveragePercentage;
            Timestamp = timestamp;
        }

        public Dictionary<string, object> ToAnalyticsData()
        {
            return new Dictionary<string, object>
            {
                ["fact_set_id"] = FactSetId,
                ["promoted_facts_count"] = PromotedFactsCount,
                ["consecutive_correct_count"] = ConsecutiveCorrectCount,
                ["coverage_percentage"] = CoveragePercentage,
                ["timestamp"] = Timestamp.ToUnixTimeSeconds()
            };
        }
    }
} 