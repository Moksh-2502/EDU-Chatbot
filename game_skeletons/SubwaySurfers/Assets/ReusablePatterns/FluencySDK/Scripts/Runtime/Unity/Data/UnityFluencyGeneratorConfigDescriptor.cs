using UnityEngine;

namespace FluencySDK.Unity.Data
{
    [CreateAssetMenu(fileName = "LearningAlgorithmConfig", menuName = "FluencySDK/LearningAlgorithmConfig")]
    public class UnityFluencyGeneratorConfigDescriptor : ScriptableObject
    {
        [field: SerializeField] public LearningAlgorithmConfig.Mode ConfigMode { get; private set; } = LearningAlgorithmConfig.Mode.Normal;
        
        public LearningAlgorithmConfig GetConfig()
        {
            return ConfigMode switch
            {
                LearningAlgorithmConfig.Mode.Normal => LearningAlgorithmConfig.CreateNormal(),
                LearningAlgorithmConfig.Mode.SpeedRun => LearningAlgorithmConfig.CreateSpeedRun(),
                _ => LearningAlgorithmConfig.CreateNormal()
            };
        }
    }
}