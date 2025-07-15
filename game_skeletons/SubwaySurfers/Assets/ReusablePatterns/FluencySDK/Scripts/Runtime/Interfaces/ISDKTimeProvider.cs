using System;
using ReusablePatterns.FluencySDK.Enums;

namespace ReusablePatterns.FluencySDK.Scripts.Interfaces
{
    public interface ISDKTimeProvider
    {
        event Action OnTimeRestarted;
        QuestionGenerationMode QuestionMode { get; }
        double StartTime { get; }
    }
}