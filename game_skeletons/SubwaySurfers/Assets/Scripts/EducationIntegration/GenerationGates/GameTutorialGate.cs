using FluencySDK;
using FluencySDK.Unity;
using ReusablePatterns.FluencySDK.Scripts.Interfaces;
using SubwaySurfers.Tutorial.Core;
using UnityEngine;

namespace EducationIntegration.GenerationGates
{
    public class GameTutorialGate : MonoBehaviour, IQuestionGenerationGate
    {
        public string GateIdentifier => "tutorial_gate";

        public bool CanGenerateNextQuestion =>
            ITutorialManager.Instance == null || ITutorialManager.Instance.IsActive == false;
        
        private IQuestionGenerationGateRegistry _registry;

        private void Awake()
        {
            _registry = BaseQuestionProvider.Instance;
        }

        private void OnEnable()
        {
            _registry?.RegisterQuestionGenerationGate(this);
        }

        private void OnDisable()
        {
            _registry?.UnregisterQuestionGenerationGate(this);
        }
    }
}