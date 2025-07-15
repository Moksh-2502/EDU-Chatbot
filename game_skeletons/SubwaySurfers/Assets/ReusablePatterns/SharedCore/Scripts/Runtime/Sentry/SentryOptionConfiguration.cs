using Sentry.Unity;
using UnityEngine;
namespace SharedCore.Sentry
{
    [CreateAssetMenu(fileName = "SentryOptionConfiguration", menuName = "Sentry/SentryOptionConfiguration")]
    public class SentryOptionConfiguration : SentryOptionsConfiguration
    {
        public override void Configure(SentryUnityOptions options)
        {
            // Load the build configuration and apply it to Sentry options
            var buildConfig = GameBuildConfigLoader.LoadBuildConfiguration();

            options.Environment = buildConfig.Environment;
            options.DefaultTags.Add("buildId", buildConfig.BuildId);
        }
    }
}