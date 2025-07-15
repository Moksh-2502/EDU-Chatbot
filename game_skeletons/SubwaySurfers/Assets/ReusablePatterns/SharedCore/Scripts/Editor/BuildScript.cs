using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using SharedCore;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline;
using UnityEngine;
#if UNITY_6000_0_OR_NEWER
using UnityEditor.Build.Profile;
#endif

namespace UnityBuilderAction
{
    public static class BuildScript
    {
        private static readonly string Eol = Environment.NewLine;

        private static readonly string[] Secrets =
            {"androidKeystorePass", "androidKeyaliasName", "androidKeyaliasPass"};

        // Unity Editor Menu Items
        [MenuItem("Build/Development Build (Current Platform)", priority = 100)]
        public static void BuildCurrentPlatformDev()
        {
            BuildFromEditor(EditorUserBuildSettings.activeBuildTarget, BuildOptions.Development);
        }

        [MenuItem("Build/Release Build (Current Platform)", priority = 101)]
        public static void BuildCurrentPlatformRelease()
        {
            BuildFromEditor(EditorUserBuildSettings.activeBuildTarget, BuildOptions.None);
        }

        [MenuItem("Build/Development Build (Windows)", priority = 200)]
        public static void BuildWindowsDev()
        {
            BuildFromEditor(BuildTarget.StandaloneWindows64, BuildOptions.Development);
        }

        [MenuItem("Build/Release Build (Windows)", priority = 201)]
        public static void BuildWindowsRelease()
        {
            BuildFromEditor(BuildTarget.StandaloneWindows64, BuildOptions.None);
        }

        [MenuItem("Build/Development Build (Android)", priority = 300)]
        public static void BuildAndroidDev()
        {
            BuildFromEditor(BuildTarget.Android, BuildOptions.Development);
        }

        [MenuItem("Build/Release Build (Android)", priority = 301)]
        public static void BuildAndroidRelease()
        {
            BuildFromEditor(BuildTarget.Android, BuildOptions.None);
        }

        [MenuItem("Build/Open Build Settings Window", priority = 400)]
        public static void OpenBuildSettingsWindow()
        {
            BuildSettingsWindow.ShowWindow();
        }

        /// <summary>
        /// Method to build from Unity Editor with specified target and options
        /// </summary>
        private static void BuildFromEditor(BuildTarget buildTarget, BuildOptions buildOptions)
        {
            // Create default build path
            string buildPath = GetDefaultBuildPath(buildTarget);
            
            // Ensure build directory exists
            string buildDir = Path.GetDirectoryName(buildPath);
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            // Configure platform-specific settings
            ConfigurePlatformSettings(buildTarget);

            // Execute build
            Debug.Log($"Starting {buildTarget} build with options: {buildOptions}");
            BuildPlayerDirectly(buildTarget, buildPath, buildOptions);
        }

        /// <summary>
        /// Get default build path for the specified target
        /// </summary>
        private static string GetDefaultBuildPath(BuildTarget buildTarget)
        {
            string projectPath = Application.dataPath.Replace("/Assets", "");
            string buildsFolder = Path.Combine(projectPath, "Builds");
            string platformFolder = buildTarget.ToString();
            string fileName = PlayerSettings.productName;

            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return Path.Combine(buildsFolder, platformFolder, $"{fileName}.exe");
                case BuildTarget.Android:
                    return Path.Combine(buildsFolder, platformFolder, $"{fileName}.apk");
                case BuildTarget.StandaloneOSX:
                    return Path.Combine(buildsFolder, platformFolder, $"{fileName}.app");
                case BuildTarget.StandaloneLinux64:
                    return Path.Combine(buildsFolder, platformFolder, fileName);
                default:
                    return Path.Combine(buildsFolder, platformFolder, fileName);
            }
        }

        /// <summary>
        /// Configure platform-specific settings
        /// </summary>
        private static void ConfigurePlatformSettings(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneOSX:
                    PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                    break;
                case BuildTarget.Android:
                    // Android settings will be handled by existing logic if needed
                    break;
            }
        }

        /// <summary>
        /// Direct build method that doesn't rely on command line arguments
        /// </summary>
        private static void BuildPlayerDirectly(BuildTarget buildTarget, string buildPath, BuildOptions buildOptions)
        {
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(s => s.path).ToArray();
            
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                target = buildTarget,
                locationPathName = buildPath,
                options = buildOptions
#if UNITY_2021_2_OR_NEWER
                , subtarget = 0  // Default subtarget
#endif
            };

            Debug.Log($"Building to: {buildPath}");
            Debug.Log($"Scenes included: {string.Join(", ", scenes)}");

            BuildSummary buildSummary = BuildPipeline.BuildPlayer(buildPlayerOptions).summary;
            ReportSummary(buildSummary);
            
            if (buildSummary.result == BuildResult.Succeeded)
            {
                // Open build folder on success
                EditorUtility.RevealInFinder(buildPath);
            }
        }

        // Original CI/CD Build method (unchanged)
        public static void Build()
        {
            // Gather values from args
            Dictionary<string, string> options = GetValidatedOptions();

            // Configure Sentry build settings
            if (options.TryGetValue("sentryEnvironment", out string sentryEnvironment) &&
                options.TryGetValue("buildId", out string buildId))
            {
                SetBuildConfiguration(sentryEnvironment, buildId);
                Debug.Log($"Configured Sentry with environment: {sentryEnvironment}, buildId: {buildId}");
            }

            // Build Addressables before building the player
            BuildAddressableContent();

            // Set version for this build
            if (options.TryGetValue("buildVersion", out string buildVersion) && buildVersion != "none")
            {
                PlayerSettings.bundleVersion = buildVersion;
                PlayerSettings.macOS.buildNumber = buildVersion;
            }
            if (options.TryGetValue("androidVersionCode", out string versionCode) && versionCode != "0")
            {
                PlayerSettings.Android.bundleVersionCode = int.Parse(options["androidVersionCode"]);
            }

            // Apply build target
            var buildTarget = (BuildTarget)Enum.Parse(typeof(BuildTarget), options["buildTarget"]);
            switch (buildTarget)
            {
                case BuildTarget.Android:
                    {
                        EditorUserBuildSettings.buildAppBundle = options["customBuildPath"].EndsWith(".aab");
                        if (options.TryGetValue("androidKeystoreName", out string keystoreName) &&
                            !string.IsNullOrEmpty(keystoreName))
                        {
                            PlayerSettings.Android.useCustomKeystore = true;
                            PlayerSettings.Android.keystoreName = keystoreName;
                        }
                        if (options.TryGetValue("androidKeystorePass", out string keystorePass) &&
                            !string.IsNullOrEmpty(keystorePass))
                            PlayerSettings.Android.keystorePass = keystorePass;
                        if (options.TryGetValue("androidKeyaliasName", out string keyaliasName) &&
                            !string.IsNullOrEmpty(keyaliasName))
                            PlayerSettings.Android.keyaliasName = keyaliasName;
                        if (options.TryGetValue("androidKeyaliasPass", out string keyaliasPass) &&
                            !string.IsNullOrEmpty(keyaliasPass))
                            PlayerSettings.Android.keyaliasPass = keyaliasPass;
                        if (options.TryGetValue("androidTargetSdkVersion", out string androidTargetSdkVersion) &&
                            !string.IsNullOrEmpty(androidTargetSdkVersion))
                        {
                            var targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
                            try
                            {
                                targetSdkVersion =
                                    (AndroidSdkVersions)Enum.Parse(typeof(AndroidSdkVersions), androidTargetSdkVersion);
                            }
                            catch
                            {
                                UnityEngine.Debug.Log("Failed to parse androidTargetSdkVersion! Fallback to AndroidApiLevelAuto");
                            }

                            PlayerSettings.Android.targetSdkVersion = targetSdkVersion;
                        }

                        break;
                    }
                case BuildTarget.StandaloneOSX:
                    PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                    break;
            }

            // Determine subtarget
            int buildSubtarget = 0;
#if UNITY_2021_2_OR_NEWER
            if (!options.TryGetValue("standaloneBuildSubtarget", out var subtargetValue) || !Enum.TryParse(subtargetValue, out StandaloneBuildSubtarget buildSubtargetValue)) {
                buildSubtargetValue = default;
            }
            buildSubtarget = (int) buildSubtargetValue;
#endif

            // Custom build
            Build(buildTarget, buildSubtarget, options["customBuildPath"]);
        }

#if UNITY_6000_0_OR_NEWER
        public static void BuildWithProfile()
        {
            // Gather values from args
            Dictionary<string, string> options = GetValidatedOptions();

            // Load build profile from Assets folder
            BuildProfile buildProfile = AssetDatabase.LoadAssetAtPath<BuildProfile>(options["customBuildProfile"]);

            // Set it as active
            BuildProfile.SetActiveBuildProfile(buildProfile);
// Get all buildOptions from options
      BuildOptions buildOptions = BuildOptions.None;
      foreach (string buildOptionString in Enum.GetNames(typeof(BuildOptions))) {
        if (options.ContainsKey(buildOptionString)) {
          BuildOptions buildOptionEnum = (BuildOptions) Enum.Parse(typeof(BuildOptions), buildOptionString);
          buildOptions |= buildOptionEnum;
        }
      }
            // Define BuildPlayerWithProfileOptions
            var buildPlayerWithProfileOptions = new BuildPlayerWithProfileOptions {
                buildProfile = buildProfile,
                locationPathName = options["customBuildPath"],
                options = buildOptions,
            };

            BuildSummary buildSummary = BuildPipeline.BuildPlayer(buildPlayerWithProfileOptions).summary;
            ReportSummary(buildSummary);
            ExitWithResult(buildSummary.result);
        }
#endif

        private static Dictionary<string, string> GetValidatedOptions()
        {
            ParseCommandLineArguments(out Dictionary<string, string> validatedOptions);

            if (!validatedOptions.TryGetValue("projectPath", out string _))
            {
                Console.WriteLine("Missing argument -projectPath");
                EditorApplication.Exit(110);
            }

            if (!validatedOptions.TryGetValue("buildTarget", out string buildTarget))
            {
                Console.WriteLine("Missing argument -buildTarget");
                EditorApplication.Exit(120);
            }

            if (!Enum.IsDefined(typeof(BuildTarget), buildTarget ?? string.Empty))
            {
                Console.WriteLine($"{buildTarget} is not a defined {nameof(BuildTarget)}");
                EditorApplication.Exit(121);
            }

            if (!validatedOptions.TryGetValue("customBuildPath", out string _))
            {
                Console.WriteLine("Missing argument -customBuildPath");
                EditorApplication.Exit(130);
            }

            const string defaultCustomBuildName = "TestBuild";
            if (!validatedOptions.TryGetValue("customBuildName", out string customBuildName))
            {
                Console.WriteLine($"Missing argument -customBuildName, defaulting to {defaultCustomBuildName}.");
                validatedOptions.Add("customBuildName", defaultCustomBuildName);
            }
            else if (customBuildName == "")
            {
                Console.WriteLine($"Invalid argument -customBuildName, defaulting to {defaultCustomBuildName}.");
                validatedOptions.Add("customBuildName", defaultCustomBuildName);
            }

            return validatedOptions;
        }

        private static void ParseCommandLineArguments(out Dictionary<string, string> providedArguments)
        {
            providedArguments = new Dictionary<string, string>();
            string[] args = Environment.GetCommandLineArgs();

            Console.WriteLine(
                $"{Eol}" +
                $"###########################{Eol}" +
                $"#    Parsing settings     #{Eol}" +
                $"###########################{Eol}" +
                $"{Eol}"
            );

            // Extract flags with optional values
            for (int current = 0, next = 1; current < args.Length; current++, next++)
            {
                // Parse flag
                bool isFlag = args[current].StartsWith("-");
                if (!isFlag) continue;
                string flag = args[current].TrimStart('-');

                // Parse optional value
                bool flagHasValue = next < args.Length && !args[next].StartsWith("-");
                string value = flagHasValue ? args[next].TrimStart('-') : "";
                bool secret = Secrets.Contains(flag);
                string displayValue = secret ? "*HIDDEN*" : "\"" + value + "\"";

                // Assign
                Console.WriteLine($"Found flag \"{flag}\" with value {displayValue}.");
                providedArguments.Add(flag, value);
            }
        }

        private static void Build(BuildTarget buildTarget, int buildSubtarget, string filePath)
        {
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(s => s.path).ToArray();
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                target = buildTarget,
                //                targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget),
                locationPathName = filePath,
                //                options = UnityEditor.BuildOptions.Development
#if UNITY_2021_2_OR_NEWER
                subtarget = buildSubtarget
#endif
            };

            BuildSummary buildSummary = BuildPipeline.BuildPlayer(buildPlayerOptions).summary;
            ReportSummary(buildSummary);
            ExitWithResult(buildSummary.result);
        }

        private static void ReportSummary(BuildSummary summary)
        {
            Console.WriteLine(
                $"{Eol}" +
                $"###########################{Eol}" +
                $"#      Build results      #{Eol}" +
                $"###########################{Eol}" +
                $"{Eol}" +
                $"Duration: {summary.totalTime.ToString()}{Eol}" +
                $"Warnings: {summary.totalWarnings.ToString()}{Eol}" +
                $"Errors: {summary.totalErrors.ToString()}{Eol}" +
                $"Size: {summary.totalSize.ToString()} bytes{Eol}" +
                $"{Eol}"
            );
        }

        private static void ExitWithResult(BuildResult result)
        {
            switch (result)
            {
                case BuildResult.Succeeded:
                    Console.WriteLine("Build succeeded!");
                    EditorApplication.Exit(0);
                    break;
                case BuildResult.Failed:
                    Console.WriteLine("Build failed!");
                    EditorApplication.Exit(101);
                    break;
                case BuildResult.Cancelled:
                    Console.WriteLine("Build cancelled!");
                    EditorApplication.Exit(102);
                    break;
                case BuildResult.Unknown:
                default:
                    Console.WriteLine("Build result is unknown!");
                    EditorApplication.Exit(103);
                    break;
            }
        }

        private static void SetBuildConfiguration(string environment, string buildId)
        {
            var buildConfig = GameBuildConfigLoader.LoadBuildConfiguration();
            buildConfig.Environment = environment;
            buildConfig.BuildId = buildId;
            buildConfig.BuildDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            
            EditorUtility.SetDirty(buildConfig);
            Debug.Log($"Configured build with environment: {environment}, buildId: {buildId}, buildDate: {buildConfig.BuildDate}");
        }

        private static void BuildAddressableContent()
        {
            Debug.Log("Building Addressable content...");
            
            try
            {
                // Get the default addressable settings
                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                
                if (settings == null)
                {
                    Debug.LogError("Addressable Asset Settings not found. Please configure Addressables in your project.");
                    return;
                }

                // Build the addressable content using the correct API
                AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
                
                if (result != null && string.IsNullOrEmpty(result.Error))
                {
                    Debug.Log("Addressable content built successfully.");
                }
                else
                {
                    string errorMessage = result?.Error ?? "Unknown error occurred during Addressable build";
                    Debug.LogError($"Addressable build failed: {errorMessage}");
                    throw new Exception($"Addressable build failed: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception during Addressable build: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// Editor window for advanced build configuration
    /// </summary>
    public class BuildSettingsWindow : EditorWindow
    {
        private BuildTarget selectedBuildTarget = BuildTarget.StandaloneWindows64;
        private string customBuildPath = "";
        private bool developmentBuild = false;
        private bool allowDebugging = false;
        private bool autoconnectProfiler = false;

        // EditorPrefs keys for persistence
        private const string CUSTOM_BUILD_PATH_KEY = "BuildSettingsWindow.CustomBuildPath";
        private const string SELECTED_BUILD_TARGET_KEY = "BuildSettingsWindow.SelectedBuildTarget";
        private const string DEVELOPMENT_BUILD_KEY = "BuildSettingsWindow.DevelopmentBuild";
        private const string ALLOW_DEBUGGING_KEY = "BuildSettingsWindow.AllowDebugging";
        private const string AUTOCONNECT_PROFILER_KEY = "BuildSettingsWindow.AutoconnectProfiler";

        public static void ShowWindow()
        {
            BuildSettingsWindow window = GetWindow<BuildSettingsWindow>("Advanced Build Settings");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Load build target
            if (EditorPrefs.HasKey(SELECTED_BUILD_TARGET_KEY))
            {
                selectedBuildTarget = (BuildTarget)EditorPrefs.GetInt(SELECTED_BUILD_TARGET_KEY, (int)EditorUserBuildSettings.activeBuildTarget);
            }
            else
            {
                selectedBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            }

            // Load custom build path or use default
            if (EditorPrefs.HasKey(CUSTOM_BUILD_PATH_KEY))
            {
                customBuildPath = EditorPrefs.GetString(CUSTOM_BUILD_PATH_KEY);
                
                // Validate that the saved path is still appropriate for the current build target
                string expectedExtension = GetExtensionForTarget(selectedBuildTarget);
                if (!string.IsNullOrEmpty(expectedExtension) && 
                    !customBuildPath.EndsWith($".{expectedExtension}", System.StringComparison.OrdinalIgnoreCase))
                {
                    // If the saved path doesn't match the current target, reset to default
                    customBuildPath = GetDefaultBuildPath(selectedBuildTarget);
                }
            }
            else
            {
                customBuildPath = GetDefaultBuildPath(selectedBuildTarget);
            }

            // Load build options
            developmentBuild = EditorPrefs.GetBool(DEVELOPMENT_BUILD_KEY, false);
            allowDebugging = EditorPrefs.GetBool(ALLOW_DEBUGGING_KEY, false);
            autoconnectProfiler = EditorPrefs.GetBool(AUTOCONNECT_PROFILER_KEY, false);
        }

        private void SaveSettings()
        {
            EditorPrefs.SetString(CUSTOM_BUILD_PATH_KEY, customBuildPath);
            EditorPrefs.SetInt(SELECTED_BUILD_TARGET_KEY, (int)selectedBuildTarget);
            EditorPrefs.SetBool(DEVELOPMENT_BUILD_KEY, developmentBuild);
            EditorPrefs.SetBool(ALLOW_DEBUGGING_KEY, allowDebugging);
            EditorPrefs.SetBool(AUTOCONNECT_PROFILER_KEY, autoconnectProfiler);
        }

        private void OnGUI()
        {
            GUILayout.Label("Advanced Build Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Build Target
            EditorGUI.BeginChangeCheck();
            selectedBuildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Build Target", selectedBuildTarget);
            if (EditorGUI.EndChangeCheck())
            {
                // When build target changes, update the build path to match the new target
                string currentFileName = Path.GetFileNameWithoutExtension(customBuildPath);
                customBuildPath = GetDefaultBuildPathWithCustomName(selectedBuildTarget, currentFileName);
                SaveSettings();
            }

            EditorGUILayout.Space();

            // Build Options
            GUILayout.Label("Build Options", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            developmentBuild = EditorGUILayout.Toggle("Development Build", developmentBuild);
            allowDebugging = EditorGUILayout.Toggle("Script Debugging", allowDebugging);
            autoconnectProfiler = EditorGUILayout.Toggle("Autoconnect Profiler", autoconnectProfiler);
            if (EditorGUI.EndChangeCheck())
            {
                SaveSettings();
            }

            EditorGUILayout.Space();

            // Build Path
            GUILayout.Label("Build Path", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            customBuildPath = EditorGUILayout.TextField(customBuildPath);
            if (EditorGUI.EndChangeCheck())
            {
                SaveSettings();
            }
            
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string newPath = EditorUtility.SaveFilePanel("Choose Build Location", 
                    Path.GetDirectoryName(customBuildPath), 
                    Path.GetFileNameWithoutExtension(customBuildPath), 
                    GetExtensionForTarget(selectedBuildTarget));
                if (!string.IsNullOrEmpty(newPath))
                {
                    customBuildPath = newPath;
                    SaveSettings();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Reset to Default Path"))
            {
                customBuildPath = GetDefaultBuildPath(selectedBuildTarget);
                SaveSettings();
            }

            EditorGUILayout.Space();

            // Build Button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Build", GUILayout.Height(30)))
            {
                ExecuteBuild();
            }
            GUI.backgroundColor = Color.white;
        }

        private void ExecuteBuild()
        {
            // Combine build options
            BuildOptions buildOptions = BuildOptions.None;
            if (developmentBuild) buildOptions |= BuildOptions.Development;
            if (allowDebugging) buildOptions |= BuildOptions.AllowDebugging;
            if (autoconnectProfiler) buildOptions |= BuildOptions.ConnectWithProfiler;

            // Ensure build directory exists
            string buildDir = Path.GetDirectoryName(customBuildPath);
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            // Execute build
            Debug.Log($"Starting custom build: {selectedBuildTarget} at {customBuildPath}");
            
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(s => s.path).ToArray();
            
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                target = selectedBuildTarget,
                locationPathName = customBuildPath,
                options = buildOptions
            };

            BuildSummary buildSummary = BuildPipeline.BuildPlayer(buildPlayerOptions).summary;
            
            if (buildSummary.result == BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog("Build Complete", $"Build completed successfully!\n\nLocation: {customBuildPath}", "OK");
                EditorUtility.RevealInFinder(customBuildPath);
                Close();
            }
            else
            {
                EditorUtility.DisplayDialog("Build Failed", $"Build failed with result: {buildSummary.result}", "OK");
            }
        }

        private string GetDefaultBuildPath(BuildTarget buildTarget)
        {
            string projectPath = Application.dataPath.Replace("/Assets", "");
            string buildsFolder = Path.Combine(projectPath, "Builds");
            string platformFolder = buildTarget.ToString();
            string fileName = PlayerSettings.productName;

            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return Path.Combine(buildsFolder, platformFolder, $"{fileName}.exe");
                case BuildTarget.Android:
                    return Path.Combine(buildsFolder, platformFolder, $"{fileName}.apk");
                case BuildTarget.StandaloneOSX:
                    return Path.Combine(buildsFolder, platformFolder, $"{fileName}.app");
                case BuildTarget.StandaloneLinux64:
                    return Path.Combine(buildsFolder, platformFolder, fileName);
                default:
                    return Path.Combine(buildsFolder, platformFolder, fileName);
            }
        }

        private string GetDefaultBuildPathWithCustomName(BuildTarget buildTarget, string customName)
        {
            string projectPath = Application.dataPath.Replace("/Assets", "");
            string buildsFolder = Path.Combine(projectPath, "Builds");
            string platformFolder = buildTarget.ToString();
            string fileName = string.IsNullOrEmpty(customName) ? PlayerSettings.productName : customName;

            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return Path.Combine(buildsFolder, platformFolder, $"{fileName}.exe");
                case BuildTarget.Android:
                    return Path.Combine(buildsFolder, platformFolder, $"{fileName}.apk");
                case BuildTarget.StandaloneOSX:
                    return Path.Combine(buildsFolder, platformFolder, $"{fileName}.app");
                case BuildTarget.StandaloneLinux64:
                    return Path.Combine(buildsFolder, platformFolder, fileName);
                default:
                    return Path.Combine(buildsFolder, platformFolder, fileName);
            }
        }

        private string GetExtensionForTarget(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "exe";
                case BuildTarget.Android:
                    return "apk";
                case BuildTarget.StandaloneOSX:
                    return "app";
                default:
                    return "";
            }
        }
    }
}