using System.Collections.Generic;
using FluencySDK;

namespace FluencySDK.Tests.Mocks
{
    public class MockEventHandler : ILearningAlgorithmEventHandler
    {
        public List<IndividualFactProgressionInfo> ReceivedEvents { get; } = new List<IndividualFactProgressionInfo>();
        public List<BulkPromotionInfo> ReceivedBulkPromotions { get; } = new List<BulkPromotionInfo>();

        public void OnIndividualFactProgression(IndividualFactProgressionInfo eventInfo)
        {
            ReceivedEvents.Add(eventInfo);
        }

        public void OnBulkPromotion(BulkPromotionInfo eventInfo)
        {
            ReceivedBulkPromotions.Add(eventInfo);
        }

        public void Clear()
        {
            ReceivedEvents.Clear();
            ReceivedBulkPromotions.Clear();
        }
    }
} 