namespace ReusablePatterns.SharedCore.Scripts.Runtime.Debugging
{
    public interface IDebugInfoProvider
    {
        string DebugGroupName { get; }
        string DebugTitle { get; }
        string GetDebugInfo();
    }
}