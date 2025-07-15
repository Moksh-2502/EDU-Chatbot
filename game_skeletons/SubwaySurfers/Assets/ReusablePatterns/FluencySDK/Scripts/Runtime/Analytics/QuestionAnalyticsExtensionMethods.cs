using System.Linq;
using System.Collections.Generic;
using System;
using SharedCore.Analytics;
namespace FluencySDK.Analytics
{
    public static class QuestionAnalyticsExtensionMethods
    {
        public static IDictionary<string, object> ToAnalyticsProperties(this IQuestion question)
        {
            var result = new Dictionary<string, object>
            {
                { "question_id", question.Id },
                { "fact_id", question.FactId },
                { "fact_set_id", question.FactSetId },
                { "question_text", question.Text },
                { "time_to_answer", question.TimeToAnswer },
                { "choice_count", question.Choices?.Length ?? 0 },
            };

            if(question.TimeStarted.HasValue)
            {
                result["time_started"] = TimeStampToDateTimeString(question.TimeStarted.Value);
            }

            if(question.TimeEnded.HasValue)
            {
                result["time_ended"] = TimeStampToDateTimeString(question.TimeEnded.Value);
            }
            else
            {
                result["time_ended"] = AnalyticsConstants.EmptyPropertyValue;
            }

            if(question.Choices != null)
            {
                result["choices"] = string.Join(",", question.Choices.Select(c => c.Value.ToString()).ToArray());
            }

            if(question.TimeToAnswer.HasValue)
            {
                result["time_to_answer"] = question.TimeToAnswer.Value;
            }
            else
            {
                result["time_to_answer"] = AnalyticsConstants.EmptyPropertyValue;
            }

            return result;
        }

        private static string TimeStampToDateTimeString(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }
    }
}
