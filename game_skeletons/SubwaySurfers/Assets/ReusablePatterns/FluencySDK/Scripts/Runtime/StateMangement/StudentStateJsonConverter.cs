using System;
using FluencySDK.Migrations;
using FluencySDK.Versioning;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace FluencySDK.Serialization
{
    /// <summary>
    /// Custom JSON converter for StudentState that handles version detection and migration
    /// Based on the pattern from ReactGameMessageJsonConverter
    /// </summary>
    [Preserve]
    public class StudentStateJsonConverter : JsonConverter<StudentState>
    {
        public override bool CanWrite => false; // Only handle deserialization

        public override StudentState ReadJson(JsonReader reader, Type objectType, StudentState existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                var jObject = JObject.Load(reader);
                var result = DeserializeVersionedState(jObject, serializer);
                return result ?? IStudentStateMigrationService.Instance.CreateNewState();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FluencyMigration] Failed to deserialize StudentState: {ex.Message}");
                return IStudentStateMigrationService.Instance.CreateNewState();
            }
        }

        public override void WriteJson(JsonWriter writer, StudentState value, JsonSerializer serializer)
        {
            // Let default serialization handle writing
            throw new NotImplementedException("Use default serialization for writing");
        }

        private StudentState DeserializeVersionedState(JObject jObject, JsonSerializer serializer)
        {
            // Extract version (default to 0 if missing for legacy states)
            var versionToken = jObject[nameof(IStudentStateVersion.Version)];
            var version = versionToken?.Value<int>() ?? null;

            Debug.Log($"[FluencyMigration] Deserializing state with version: {version}");

            // object doesn't even have a version field, start fresh
            if (version.HasValue == false)
            {
                return null;
            }

            // Get the appropriate version type
            var registry = IMigrationsRegistry.Instance;
            if (registry.TryGetVersionType(version.Value, out var versionType) == false)
            {
                Debug.LogWarning($"[FluencyMigration] Unknown version {version}, creating fresh state");
                return null;
            }

            // Deserialize to the specific version type
            try
            {
                var versionedState = Activator.CreateInstance(versionType);
                serializer.Populate(jObject.CreateReader(), versionedState);
                return IStudentStateMigrationService.Instance.MigrateToLatest(versionedState as IStudentStateVersion);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FluencyMigration] Failed to deserialize version {version}: {ex.Message}");
                return null;
            }
        }
    }
}