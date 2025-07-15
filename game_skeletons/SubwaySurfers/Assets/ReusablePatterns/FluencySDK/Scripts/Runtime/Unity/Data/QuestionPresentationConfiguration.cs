using UnityEngine;

namespace FluencySDK.Data
{
    /// <summary>
    /// Configuration settings for question presentation
    /// </summary>
    [CreateAssetMenu(fileName = "QuestionPresentationConfiguration", menuName = "FluencySDK/Question Presentation Configuration")]
    public class QuestionPresentationConfiguration : ScriptableObject
    {
        [field: SerializeField] public float PostAnswerPresentationTime { get; private set; } = 2f;
    }
} 