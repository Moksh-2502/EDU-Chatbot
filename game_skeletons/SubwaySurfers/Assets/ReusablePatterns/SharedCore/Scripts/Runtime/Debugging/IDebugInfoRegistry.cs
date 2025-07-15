using System.Collections.Generic;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.Debugging
{
    public interface IDebugInfoRegistry
    {
        static IDebugInfoRegistry Instance { get; private set; } = new DebugInfoProviderRegistry();
        void RegisterProvider(IDebugInfoProvider provider);
        void UnregisterProvider(IDebugInfoProvider provider);
        
        IReadOnlyCollection<IDebugInfoProvider> GetProviders();
    }
}