using mixpanel;
using UnityEngine;
using System.Linq;
using AIEduChatbot.UnityReactBridge.Handlers;
using AIEduChatbot.UnityReactBridge.Data;

namespace SharedCore.Analytics
{
    /// <summary>
    /// MixPanel implementation of the analytics service.
    /// Singleton class that handles sending analytics events to MixPanel service.
    /// </summary>
    public class MixPanelAnalyticsService : IAnalyticsService, IReactGameMessageHandler
    {
        private readonly AnalyticsConfig _config;
        private readonly GameBuildConfig _gameBuildConfig;

        public MixPanelAnalyticsService(AnalyticsConfig config, GameBuildConfig gameBuildConfig)
        {
            _config = config;
            _gameBuildConfig = gameBuildConfig;

            IReactGameMessageHandlerCollection.Instance.RegisterHandler(this);
        }

        public void OnGameEventReceived(ReactGameMessage gameMessage)
        {
            if (gameMessage is SessionDataReactGameMessage sessionData
                && sessionData.user != null
                && !string.IsNullOrEmpty(sessionData.user.id))
            {
                Mixpanel.Identify(sessionData.user.id);

                var userProps = new Value();
                if (!string.IsNullOrEmpty(sessionData.user.email))
                    userProps["email"] = sessionData.user.email;
                if (!string.IsNullOrEmpty(sessionData.user.name))
                    userProps["name"] = sessionData.user.name;
                if (!string.IsNullOrEmpty(sessionData.user.type))
                    userProps["user_type"] = sessionData.user.type;

                Mixpanel.People.Set(userProps);

                Debug.Log($"[MixPanel] Identified user: {sessionData.user.id}");
            }
        }

        private bool IsBuildTrackable()
        {
            // If trackable environments is configured and current environment is in it, it's trackable
            if (_config.TrackableEnvironments != null &&
                _config.TrackableEnvironments.Contains(_gameBuildConfig.Environment))
            {
                return true;
            }

            // If trackable build IDs is configured and current build ID is in it, it's trackable
            if (_config.TrackableBuildIds != null && _config.TrackableBuildIds.Contains(_gameBuildConfig.BuildId))
            {
                return true;
            }

            // If neither is configured, default to trackable
            if (_config.TrackableEnvironments == null && _config.TrackableBuildIds == null)
            {
                return true;
            }

            // If both are configured but neither contains current values, not trackable
            return false;
        }

        /// <summary>
        /// Tracks an analytics event.
        /// </summary>
        /// <param name="analyticsEvent">The event to track</param>
        public void TrackEvent(IAnalyticsEvent analyticsEvent)
        {
            if (!_config.TrackingEnabled)
            {
                return;
            }

            if (!IsBuildTrackable())
            {
                return;
            }

            if (IGameSessionProvider.Instance == null)
            {
                analyticsEvent.SessionId = IGameSessionProvider.UNSET_STRING;
            }
            else
            {
                analyticsEvent.SessionId = IGameSessionProvider.Instance.SessionId;
            }

            var eventProperties = analyticsEvent.GetProperties();
            if (eventProperties == null || string.IsNullOrWhiteSpace(analyticsEvent.EventName))
            {
                return;
            }

            var propertiesValue = new Value();
            foreach (var kvp in eventProperties)
            {
                if (kvp.Value == null)
                {
                    Debug.LogError(
                        $"Key '{kvp.Key}: Value is null in event properties for event '{analyticsEvent.EventName}'. This will be skipped.");
                    propertiesValue[kvp.Key] = "null";
                }
                else
                {
                    propertiesValue[kvp.Key] = kvp.Value.ToString().ToLower();
                }
            }

            Mixpanel.Track(analyticsEvent.EventName, propertiesValue);
            Debug.Log($"Tracked event: {analyticsEvent.EventName} with properties: {propertiesValue}");
        }
    }
}