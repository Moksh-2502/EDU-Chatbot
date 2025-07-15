using System;

namespace FluencySDK.Events
{
    public static class FluencySDKEventBus
    {
        public struct FluencySDKReadyEventArgs
        {
            
        }
        public static event Action<FluencySDKReadyEventArgs> OnFluencySDKReady;
        
        public static void RaiseFluencySDKReady()
        {
            OnFluencySDKReady?.Invoke(new FluencySDKReadyEventArgs());
        }
    }
}