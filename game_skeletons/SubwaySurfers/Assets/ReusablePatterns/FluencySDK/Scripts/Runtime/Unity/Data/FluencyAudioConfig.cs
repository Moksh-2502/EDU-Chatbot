using System;
using UnityEngine;

namespace FluencySDK.Unity.Data
{
    [CreateAssetMenu(fileName = "FluencyAudioConfig", menuName = "FluencySDK/FluencyAudioConfig")]
    public class FluencyAudioConfig : ScriptableObject
    {
        [Header("General Audio Clips")]
        [field: SerializeField] public AudioClip QuestionStartedClip { get; private set; }
        [field: SerializeField] public AudioClip AnswerCorrectClip { get; private set; }
        [field: SerializeField] public AudioClip AnswerWrongClip { get; private set; }

        [Header("Wrong Answer System")]
        [field: SerializeField] public AudioClip[] WrongAnswerPrefixClips { get; private set; } = Array.Empty<AudioClip>();
        [field: SerializeField] public AudioClip[] WrongNoCorrectionClips { get; private set; } = Array.Empty<AudioClip>();
        [field: SerializeField] public AudioClip[] CorrectionAudioClips { get; private set; } = Array.Empty<AudioClip>();
        [field: SerializeField][field: Range(0f, 5f)] public float CorrectionDelayTime { get; private set; } = 1f;

        [Header("Fluency Factors Audio Clips")]
        [field: SerializeField] public AudioClip[] FluencyFactorsAudioClips { get; private set; } = Array.Empty<AudioClip>();

        [Header("Success Audio Clips")]
        [field: SerializeField] public AudioClip[] SuccessAudioClips { get; private set; } = Array.Empty<AudioClip>();
        [field: SerializeField] public AudioClip[] StreakAudioClips { get; private set; } = Array.Empty<AudioClip>();

        public bool TryGetFactorsAudioClip(int[] factors, out AudioClip audioClip)
        {
            // audio clips are named in the form of 0x0 etc
            // we need to convert the factors into a string and then find the corresponding audio clip
            string factorsString = string.Join("x", factors);
            foreach (var clip in FluencyFactorsAudioClips)
            {
                if (clip.name == factorsString)
                {
                    audioClip = clip;
                    return true;
                }
            }
            audioClip = null;
            return false;
        }

        /// <summary>
        /// Gets a random wrong answer prefix audio clip.
        /// </summary>
        /// <returns>A random prefix audio clip, or null if none available</returns>
        public AudioClip GetRandomWrongAnswerPrefix()
        {
            if (WrongAnswerPrefixClips == null || WrongAnswerPrefixClips.Length == 0)
            {
                return null;
            }

            int randomIndex = UnityEngine.Random.Range(0, WrongAnswerPrefixClips.Length);
            return WrongAnswerPrefixClips[randomIndex];
        }

        /// <summary>
        /// Gets the correction audio clip for the given factors.
        /// Searches for clips with names like "correction-2x3" based on the factors.
        /// </summary>
        /// <param name="factors">The multiplication factors from the question</param>
        /// <returns>The correction audio clip, or null if none found</returns>
        public AudioClip GetCorrectionAudioClip(int[] factors)
        {
            if (CorrectionAudioClips == null || CorrectionAudioClips.Length == 0 || factors == null || factors.Length != 2)
            {
                return null;
            }

            string correctionName = $"correction-{factors[0]}x{factors[1]}";

            foreach (var clip in CorrectionAudioClips)
            {
                if (clip != null && clip.name.Equals(correctionName, StringComparison.OrdinalIgnoreCase))
                {
                    return clip;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a fallback correction audio clip for general wrong answers.
        /// Looks for clips like "correction-oops" or "correction-not-quite".
        /// </summary>
        /// <returns>A fallback correction audio clip, or null if none available</returns>
        public AudioClip GetFallbackCorrectionAudioClip()
        {
            if (CorrectionAudioClips == null || CorrectionAudioClips.Length == 0)
            {
                return null;
            }

            // Look for general correction clips
            string[] fallbackNames = { "correction-oops", "correction-not-quite" };

            foreach (string fallbackName in fallbackNames)
            {
                foreach (var clip in CorrectionAudioClips)
                {
                    if (clip != null && clip.name.Equals(fallbackName, StringComparison.OrdinalIgnoreCase))
                    {
                        return clip;
                    }
                }
            }

            // If no specific fallback found, return the first available correction clip
            return CorrectionAudioClips[0];
        }

        /// <summary>
        /// Gets the appropriate audio clip for when a question starts.
        /// First tries to find a factors-specific clip, then falls back to the general question started clip.
        /// </summary>
        /// <param name="factors">The multiplication factors from the question</param>
        /// <returns>The audio clip to play, or null if none available</returns>
        public AudioClip GetQuestionStartedAudioClip(int[] factors = null)
        {
            // Try to get factors-specific audio first
            if (factors != null && factors.Length == 2 && TryGetFactorsAudioClip(factors, out AudioClip factorsClip))
            {
                return factorsClip;
            }

            // Fallback to general question started clip
            return QuestionStartedClip;
        }

        /// <summary>
        /// Gets the audio clip for when a question is answered correctly.
        /// </summary>
        /// <returns>The correct answer audio clip</returns>
        public AudioClip GetAnswerCorrectAudioClip()
        {
            return AnswerCorrectClip;
        }

        /// <summary>
        /// Gets the audio clip for when a question is answered incorrectly.
        /// </summary>
        /// <returns>The wrong answer audio clip</returns>
        public AudioClip GetAnswerWrongAudioClip()
        {
            return AnswerWrongClip;
        }

        /// <summary>
        /// Gets a random success audio clip for regular correct answers.
        /// </summary>
        /// <returns>A random success audio clip, or null if none available</returns>
        public AudioClip GetRandomSuccessAudioClip()
        {
            if (SuccessAudioClips == null || SuccessAudioClips.Length == 0)
            {
                return null;
            }

            int randomIndex = UnityEngine.Random.Range(0, SuccessAudioClips.Length);
            return SuccessAudioClips[randomIndex];
        }

        /// <summary>
        /// Gets a random streak audio clip for streak bonuses.
        /// </summary>
        /// <returns>A random streak audio clip, or null if none available</returns>
        public AudioClip GetRandomStreakAudioClip()
        {
            if (StreakAudioClips == null || StreakAudioClips.Length == 0)
            {
                return null;
            }

            int randomIndex = UnityEngine.Random.Range(0, StreakAudioClips.Length);
            return StreakAudioClips[randomIndex];
        }

        /// <summary>
        /// Gets a random audio clip for answering incorrectly without correction.
        /// </summary>
        ///  <returns>A random wrong no correction audio clip, or null if none available</returns>
        public AudioClip GetRandomWrongNoCorrectionClip()
        {
            if (WrongNoCorrectionClips == null || WrongNoCorrectionClips.Length == 0)
            {
                return null;
            }

            int randomIndex = UnityEngine.Random.Range(0, WrongNoCorrectionClips.Length);
            return WrongNoCorrectionClips[randomIndex];
        }
    }
}