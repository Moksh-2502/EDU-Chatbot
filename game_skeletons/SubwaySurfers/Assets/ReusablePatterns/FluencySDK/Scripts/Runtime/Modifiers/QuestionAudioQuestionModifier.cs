
using System.Collections.Generic;
using FluencySDK.Unity.Data;
using FluencySDK;
public class QuestionAudioQuestionModifier : IQuestionModifier
{
    private FluencyAudioConfig _fluencyAudioConfig;
    public QuestionAudioQuestionModifier(FluencyAudioConfig fluencyAudioConfig)
    {
        _fluencyAudioConfig = fluencyAudioConfig;
    }
    public void ModifyQuestion(IQuestion question)
    {
        if(_fluencyAudioConfig == null)
        {
            return;
        }

        var audioForQuestion = _fluencyAudioConfig.GetQuestionStartedAudioClip(StringUtilities.ExtractFactorsFromQuestionText(question.Text));
        if(audioForQuestion == null)
        {
            return;
        }

        question.TimeToAnswer += audioForQuestion.length;
    }
}

public class QuestionAudioQuestionModifierFactory : IQuestionModifierFactory
{
    private HashSet<IQuestionModifier> _modifiers = new HashSet<IQuestionModifier>();

    public void RegisterModifier(IQuestionModifier modifier)
    {
        _modifiers.Add(modifier);
    }

    public void UnregisterModifier(IQuestionModifier modifier)
    {
        _modifiers.Remove(modifier);
    }

    public void ModifyQuestion(IQuestion question)
    {
        foreach(var modifier in _modifiers)
        {
            if(modifier != null)
            {
                modifier.ModifyQuestion(question);
            }
        }
    }
}