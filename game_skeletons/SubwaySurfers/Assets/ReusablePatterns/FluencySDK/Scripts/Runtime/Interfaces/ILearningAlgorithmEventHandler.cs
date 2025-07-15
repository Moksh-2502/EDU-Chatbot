namespace FluencySDK
{
    /// <summary>
    /// Interface for handling learning algorithm events from service classes.
    /// This allows services like PromotionEngine to notify the main algorithm
    /// about important events like individual fact progressions.
    /// </summary>
    public interface ILearningAlgorithmEventHandler
    {
        void OnIndividualFactProgression(IndividualFactProgressionInfo eventInfo);
        void OnBulkPromotion(BulkPromotionInfo eventInfo);
    }
} 