using UnityEngine;

namespace SharedCore
{
    [CreateAssetMenu(fileName = "GameBuildConfig", menuName = "ReusablePatterns/GameBuildConfig")]
    public class GameBuildConfig : ScriptableObject
    {
        public const string EditorEnvironment = "editor";
        public const string DevelopmentEnvironment = "dev";
        public const string StagingEnvironment = "staging";
        public const string ProductionEnvironment = "production";


        [field: SerializeField] public string BuildId {get;
        
        #if !UNITY_EDITOR
         private
         #endif
          set;}
        [field: SerializeField] public string Environment {get;
        #if !UNITY_EDITOR
         private 
         #endif
         set;}
        [field: SerializeField] public string BuildDate {get;
         #if !UNITY_EDITOR
         private
         #endif
          set;
         }

public string BuildVersion
{
    get=> Application.version;
}
        public override string ToString()
        {
            return $"GameBuildConfig - Environment: {Environment}, BuildId: {BuildId}, BuildDate: {BuildDate}, BuildVersion: {BuildVersion}";
        }
    }
}