using System.Collections.Generic;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.Debugging
{
    public class DebugInfoProviderRegistry : IDebugInfoRegistry
    {
        private readonly HashSet<IDebugInfoProvider> _registeredProviders = new HashSet<IDebugInfoProvider>();

        public IReadOnlyCollection<IDebugInfoProvider> GetProviders() => _registeredProviders;

        public void RegisterProvider(IDebugInfoProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            _registeredProviders.Add(provider);
        }
        
        public void UnregisterProvider(IDebugInfoProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            _registeredProviders.Remove(provider);
        }
    }
}