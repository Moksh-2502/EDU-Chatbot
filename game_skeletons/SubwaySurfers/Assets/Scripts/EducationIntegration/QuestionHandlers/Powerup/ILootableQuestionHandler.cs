namespace EducationIntegration.QuestionHandlers
{
    public interface ILootableQuestionHandler<TLootable> where TLootable : UnityEngine.Component
    {
        void HandleLootableCollection(TLootable lootable);
        bool CanProcessLootables();
    }
}