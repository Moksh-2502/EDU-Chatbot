using FluencySDK;

public interface IQuestionModifier
{
    void ModifyQuestion(IQuestion question);
}
public interface IQuestionModifierFactory
{
    void RegisterModifier(IQuestionModifier modifier);
    void UnregisterModifier(IQuestionModifier modifier);

    void ModifyQuestion(IQuestion question);
}



