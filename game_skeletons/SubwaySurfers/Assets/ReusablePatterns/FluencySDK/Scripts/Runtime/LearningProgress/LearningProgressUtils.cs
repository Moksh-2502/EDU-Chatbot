namespace ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress
{
    public class LearningProgressUtils
    {
        private const string k_FactSetRewardClaimKey = "FactSetRewardClaim";
        
        
        public static string GetFactSetRewardClaimKey(string factSetId)
        {
            return $"{k_FactSetRewardClaimKey}_{factSetId}";
        }
    }
}