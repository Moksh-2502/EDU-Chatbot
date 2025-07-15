using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace SubwaySurfers.Editor
{
    public class MobileResolutionHelper : EditorWindow
    {
        private static readonly List<GameViewSize> MobileResolutions = new List<GameViewSize>
        {
            // iPhone Resolutions
            new GameViewSize { name = "iPhone SE (375x667)", width = 375, height = 667 },
            new GameViewSize { name = "iPhone 8 (375x667)", width = 375, height = 667 },
            new GameViewSize { name = "iPhone 8 Plus (414x736)", width = 414, height = 736 },
            new GameViewSize { name = "iPhone X/XS (375x812)", width = 375, height = 812 },
            new GameViewSize { name = "iPhone XR (414x896)", width = 414, height = 896 },
            new GameViewSize { name = "iPhone XS Max (414x896)", width = 414, height = 896 },
            new GameViewSize { name = "iPhone 12 Mini (360x780)", width = 360, height = 780 },
            new GameViewSize { name = "iPhone 12/Pro (390x844)", width = 390, height = 844 },
            new GameViewSize { name = "iPhone 12 Pro Max (428x926)", width = 428, height = 926 },
            new GameViewSize { name = "iPhone 13 Mini (375x812)", width = 375, height = 812 },
            new GameViewSize { name = "iPhone 13/Pro (390x844)", width = 390, height = 844 },
            new GameViewSize { name = "iPhone 13 Pro Max (428x926)", width = 428, height = 926 },
            new GameViewSize { name = "iPhone 14 (390x844)", width = 390, height = 844 },
            new GameViewSize { name = "iPhone 14 Plus (428x926)", width = 428, height = 926 },
            new GameViewSize { name = "iPhone 14 Pro (393x852)", width = 393, height = 852 },
            new GameViewSize { name = "iPhone 14 Pro Max (430x932)", width = 430, height = 932 },
            new GameViewSize { name = "iPhone 15 (393x852)", width = 393, height = 852 },
            new GameViewSize { name = "iPhone 15 Plus (430x932)", width = 430, height = 932 },
            new GameViewSize { name = "iPhone 15 Pro (393x852)", width = 393, height = 852 },
            new GameViewSize { name = "iPhone 15 Pro Max (430x932)", width = 430, height = 932 },
            
            // iPad Resolutions
            new GameViewSize { name = "iPad (768x1024)", width = 768, height = 1024 },
            new GameViewSize { name = "iPad Air (820x1180)", width = 820, height = 1180 },
            new GameViewSize { name = "iPad Pro 11\" (834x1194)", width = 834, height = 1194 },
            new GameViewSize { name = "iPad Pro 12.9\" (1024x1366)", width = 1024, height = 1366 },
            
            // Android Resolutions (Common)
            new GameViewSize { name = "Android Small (320x480)", width = 320, height = 480 },
            new GameViewSize { name = "Android Medium (360x640)", width = 360, height = 640 },
            new GameViewSize { name = "Android Large (384x640)", width = 384, height = 640 },
            new GameViewSize { name = "Android XL (411x731)", width = 411, height = 731 },
            new GameViewSize { name = "Android XXL (414x896)", width = 414, height = 896 },
            
            // Samsung Galaxy Resolutions
            new GameViewSize { name = "Galaxy S8 (360x740)", width = 360, height = 740 },
            new GameViewSize { name = "Galaxy S9 (360x740)", width = 360, height = 740 },
            new GameViewSize { name = "Galaxy S10 (360x760)", width = 360, height = 760 },
            new GameViewSize { name = "Galaxy S20 (360x800)", width = 360, height = 800 },
            new GameViewSize { name = "Galaxy S21 (384x854)", width = 384, height = 854 },
            new GameViewSize { name = "Galaxy S22 (360x780)", width = 360, height = 780 },
            new GameViewSize { name = "Galaxy S23 (393x851)", width = 393, height = 851 },
            new GameViewSize { name = "Galaxy Note 10 (412x869)", width = 412, height = 869 },
            new GameViewSize { name = "Galaxy Note 20 (412x915)", width = 412, height = 915 },
            
            // Google Pixel Resolutions
            new GameViewSize { name = "Pixel 3 (393x786)", width = 393, height = 786 },
            new GameViewSize { name = "Pixel 4 (393x830)", width = 393, height = 830 },
            new GameViewSize { name = "Pixel 5 (393x851)", width = 393, height = 851 },
            new GameViewSize { name = "Pixel 6 (411x914)", width = 411, height = 914 },
            new GameViewSize { name = "Pixel 7 (412x915)", width = 412, height = 915 },
            
            // Tablet Resolutions
            new GameViewSize { name = "Android Tablet 7\" (600x960)", width = 600, height = 960 },
            new GameViewSize { name = "Android Tablet 10\" (800x1280)", width = 800, height = 1280 },
            new GameViewSize { name = "Galaxy Tab (768x1024)", width = 768, height = 1024 },
            new GameViewSize { name = "Galaxy Tab S (800x1280)", width = 800, height = 1280 }
        };

        [Serializable]
        public class GameViewSize
        {
            public string name;
            public int width;
            public int height;
        }

        // Game View size related enums
        public enum GameViewSizeType
        {
            AspectRatio = 0,
            FixedResolution = 1
        }

        public enum GameViewSizeGroupType
        {
            Standalone = 0,
            WebGL = 1,
            iOS = 2,
            Android = 3,
            Tizen = 4,
            Switch = 5,
            Lumin = 6,
            tvOS = 7,
            PS4 = 8,
            XboxOne = 9,
            WSA = 10,
            VisionOS = 11
        }

        [MenuItem("Tools/Mobile Resolution Helper/Add All Mobile Resolutions")]
        public static void AddAllMobileResolutions()
        {
            int successCount = 0;
            foreach (var resolution in MobileResolutions)
            {
                if (AddCustomGameViewSize(resolution.name, resolution.width, resolution.height))
                {
                    successCount++;
                }
            }
            
            Debug.Log($"Successfully added {successCount}/{MobileResolutions.Count} mobile resolutions to Game View");
            EditorUtility.DisplayDialog("Mobile Resolution Helper", 
                $"Successfully added {successCount}/{MobileResolutions.Count} mobile resolutions to the Game View!", "OK");
        }

        [MenuItem("Tools/Mobile Resolution Helper/Add Common Resolutions Only")]
        public static void AddCommonMobileResolutions()
        {
            var commonResolutions = new List<GameViewSize>
            {
                new GameViewSize { name = "iPhone SE (375x667)", width = 375, height = 667 },
                new GameViewSize { name = "iPhone 12/13 (390x844)", width = 390, height = 844 },
                new GameViewSize { name = "iPhone 14 Pro (393x852)", width = 393, height = 852 },
                new GameViewSize { name = "iPhone 15 Pro Max (430x932)", width = 430, height = 932 },
                new GameViewSize { name = "Android Medium (360x640)", width = 360, height = 640 },
                new GameViewSize { name = "Android Large (384x640)", width = 384, height = 640 },
                new GameViewSize { name = "Galaxy S21 (384x854)", width = 384, height = 854 },
                new GameViewSize { name = "Pixel 6 (411x914)", width = 411, height = 914 },
                new GameViewSize { name = "iPad (768x1024)", width = 768, height = 1024 },
                new GameViewSize { name = "iPad Pro 11\" (834x1194)", width = 834, height = 1194 }
            };

            int successCount = 0;
            foreach (var resolution in commonResolutions)
            {
                if (AddCustomGameViewSize(resolution.name, resolution.width, resolution.height))
                {
                    successCount++;
                }
            }
            
            Debug.Log($"Successfully added {successCount}/{commonResolutions.Count} common mobile resolutions to Game View");
            EditorUtility.DisplayDialog("Mobile Resolution Helper", 
                $"Successfully added {successCount}/{commonResolutions.Count} common mobile resolutions to the Game View!", "OK");
        }

        [MenuItem("Tools/Mobile Resolution Helper/Remove All Custom Resolutions")]
        public static void RemoveAllCustomResolutions()
        {
            if (EditorUtility.DisplayDialog("Remove Custom Resolutions", 
                "This will remove all custom resolutions from the Game View. Are you sure?", "Yes", "Cancel"))
            {
                if (RemoveAllCustomGameViewSizes())
                {
                    Debug.Log("Removed all custom Game View resolutions");
                    EditorUtility.DisplayDialog("Mobile Resolution Helper", 
                        "Successfully removed all custom resolutions from the Game View!", "OK");
                }
                else
                {
                    Debug.LogWarning("Failed to remove custom resolutions");
                    EditorUtility.DisplayDialog("Mobile Resolution Helper", 
                        "Failed to remove custom resolutions. Please check the console for details.", "OK");
                }
            }
        }

        [MenuItem("Tools/Mobile Resolution Helper/Open Resolution Manager")]
        public static void OpenResolutionManager()
        {
            GetWindow<MobileResolutionHelper>("Mobile Resolution Manager");
        }

        private Vector2 scrollPosition;

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Mobile Resolution Helper", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Quick Actions:", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Add All Mobile Resolutions"))
            {
                AddAllMobileResolutions();
            }
            
            if (GUILayout.Button("Add Common Resolutions Only"))
            {
                AddCommonMobileResolutions();
            }
            
            if (GUILayout.Button("Remove All Custom Resolutions"))
            {
                RemoveAllCustomResolutions();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Available Resolutions:", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            foreach (var resolution in MobileResolutions)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{resolution.name} ({resolution.width}x{resolution.height})");
                if (GUILayout.Button("Add", GUILayout.Width(50)))
                {
                    if (AddCustomGameViewSize(resolution.name, resolution.width, resolution.height))
                    {
                        Debug.Log($"Added resolution: {resolution.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to add resolution: {resolution.name}");
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "These resolutions will be added to your Game View dropdown for easy testing on different mobile devices. " +
                "You can access them from the Game View's resolution dropdown menu.", 
                MessageType.Info);
        }

        /// <summary>
        /// Adds a custom game view size using Unity's GameViewSizes API
        /// </summary>
        /// <param name="name">Display name for the resolution</param>
        /// <param name="width">Width in pixels</param>
        /// <param name="height">Height in pixels</param>
        /// <returns>True if successful, false otherwise</returns>
        private static bool AddCustomGameViewSize(string name, int width, int height)
        {
            try
            {
                // Use Unity's internal API more safely
                var sizesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes");
                var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var instanceProp = singleType.GetProperty("instance");
                var sizes = instanceProp.GetValue(null, null);

                var getGroup = sizesType.GetMethod("GetGroup");
                var group = getGroup.Invoke(sizes, new object[] { (int)GameViewSizeGroupType.Standalone });

                var addCustomSize = group.GetType().GetMethod("AddCustomSize");
                var gameViewSizeType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSize");
                var gameViewSizeTypeType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizeType");

                var constructor = gameViewSizeType.GetConstructor(new Type[] 
                { 
                    gameViewSizeTypeType, 
                    typeof(int), 
                    typeof(int), 
                    typeof(string) 
                });

                var newSize = constructor.Invoke(new object[] 
                { 
                    (int)GameViewSizeType.FixedResolution, 
                    width, 
                    height, 
                    name 
                });

                addCustomSize.Invoke(group, new object[] { newSize });
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to add Game View size '{name}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Removes all custom game view sizes
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        private static bool RemoveAllCustomGameViewSizes()
        {
            try
            {
                var sizesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes");
                var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var instanceProp = singleType.GetProperty("instance");
                var sizes = instanceProp.GetValue(null, null);

                var getGroup = sizesType.GetMethod("GetGroup");
                var group = getGroup.Invoke(sizes, new object[] { (int)GameViewSizeGroupType.Standalone });

                var removeCustomSize = group.GetType().GetMethod("RemoveCustomSize");
                var getCustomCount = group.GetType().GetMethod("GetCustomCount");

                if (removeCustomSize != null && getCustomCount != null)
                {
                    int customCount = (int)getCustomCount.Invoke(group, null);
                    
                    // Remove all custom sizes (iterate backwards to avoid index issues)
                    for (int i = customCount - 1; i >= 0; i--)
                    {
                        removeCustomSize.Invoke(group, new object[] { i });
                    }
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to remove custom Game View sizes: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if the Game View API is available
        /// </summary>
        /// <returns>True if API is available</returns>
        public static bool IsGameViewAPIAvailable()
        {
            try
            {
                var sizesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes");
                return sizesType != null;
            }
            catch
            {
                return false;
            }
        }

        [MenuItem("Tools/Mobile Resolution Helper/Test API Availability")]
        public static void TestAPIAvailability()
        {
            bool available = IsGameViewAPIAvailable();
            string message = available ? 
                "Game View API is available and working!" : 
                "Game View API is not available in this Unity version.";
            
            Debug.Log(message);
            EditorUtility.DisplayDialog("API Test", message, "OK");
        }
    }
} 