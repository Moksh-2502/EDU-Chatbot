using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SubwaySurfers.Editor
{
    public class ModelPreviewGenerator : EditorWindow
    {
        private const string DEFAULT_OUTPUT_FOLDER = "Assets/GeneratedPreviews";
        private const int DEFAULT_PREVIEW_SIZE = 512;
        private const int MAX_PREVIEW_SIZE = 2048;
        
        private string outputFolder = DEFAULT_OUTPUT_FOLDER;
        private int previewSize = DEFAULT_PREVIEW_SIZE;
        private bool includeSubfolders = true;
        private bool showInProjectWindow = true;
        
        private Vector2 scrollPosition;
        private List<Object> selectedObjects = new List<Object>();
        
        [MenuItem("Tools/Model Preview Generator", false, 100)]
        public static void ShowWindow()
        {
            GetWindow<ModelPreviewGenerator>("Model Preview Generator");
        }
        
        [MenuItem("Assets/Generate Preview Images", false, 30)]
        private static void GeneratePreviewsForSelected()
        {
            var window = GetWindow<ModelPreviewGenerator>("Model Preview Generator");
            window.RefreshSelectedObjects();
            window.Show();
        }
        
        [MenuItem("Assets/Generate Preview Images", true)]
        private static bool ValidateGeneratePreviewsForSelected()
        {
            return Selection.objects != null && Selection.objects.Length > 0 && 
                   Selection.objects.Any(obj => IsValidPreviewTarget(obj) || IsFolder(obj));
        }
        
        private void OnEnable()
        {
            RefreshSelectedObjects();
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.LabelField("Model Preview Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            DrawSettingsSection();
            EditorGUILayout.Space();
            
            DrawSelectedObjectsSection();
            EditorGUILayout.Space();
            
            DrawActionButtons();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawSettingsSection()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            // Output folder
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Output Folder:", GUILayout.Width(100));
            outputFolder = EditorGUILayout.TextField(outputFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string folder = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
                if (!string.IsNullOrEmpty(folder))
                {
                    if (folder.StartsWith(Application.dataPath))
                    {
                        outputFolder = "Assets" + folder.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid Folder", 
                            "Please select a folder within the Assets directory.", "OK");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // Preview size
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Preview Size:", GUILayout.Width(100));
            previewSize = EditorGUILayout.IntSlider(previewSize, 64, MAX_PREVIEW_SIZE);
            EditorGUILayout.EndHorizontal();
            
            // Folder options
            includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", includeSubfolders);
            
            // Other options
            showInProjectWindow = EditorGUILayout.Toggle("Show in Project Window", showInProjectWindow);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSelectedObjectsSection()
        {
            EditorGUILayout.LabelField("Selected Objects", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Objects to process: {selectedObjects.Count}", EditorStyles.miniLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                RefreshSelectedObjects();
            }
            EditorGUILayout.EndHorizontal();
            
            // Show selection summary
            if (Selection.objects != null && Selection.objects.Length > 0)
            {
                int folderCount = Selection.objects.Count(IsFolder);
                int objectCount = Selection.objects.Count(IsValidPreviewTarget);
                
                if (folderCount > 0 || objectCount > 0)
                {
                    string summaryText = "";
                    if (folderCount > 0)
                        summaryText += $"{folderCount} folder(s)";
                    if (objectCount > 0)
                    {
                        if (!string.IsNullOrEmpty(summaryText)) summaryText += ", ";
                        summaryText += $"{objectCount} object(s)";
                    }
                    summaryText += " selected";
                    
                    EditorGUILayout.LabelField(summaryText, EditorStyles.miniLabel);
                }
            }
            
            if (selectedObjects.Count == 0)
            {
                EditorGUILayout.HelpBox("No valid objects selected. Select models, prefabs, GameObjects, or folders in the Project window.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < Mathf.Min(selectedObjects.Count, 10); i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(selectedObjects[i], typeof(Object), false);
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndHorizontal();
                }
                
                if (selectedObjects.Count > 10)
                {
                    EditorGUILayout.LabelField($"... and {selectedObjects.Count - 10} more objects", EditorStyles.miniLabel);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUI.BeginDisabledGroup(selectedObjects.Count == 0);
            if (GUILayout.Button("Generate Previews", GUILayout.Height(30)))
            {
                GeneratePreviews();
            }
            EditorGUI.EndDisabledGroup();
            
            if (GUILayout.Button("Clear Selection", GUILayout.Height(30), GUILayout.Width(120)))
            {
                selectedObjects.Clear();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void RefreshSelectedObjects()
        {
            selectedObjects.Clear();
            
            if (Selection.objects != null)
            {
                foreach (var obj in Selection.objects)
                {
                    if (IsValidPreviewTarget(obj))
                    {
                        selectedObjects.Add(obj);
                    }
                    else if (IsFolder(obj))
                    {
                        // Find models in the selected folder
                        string folderPath = AssetDatabase.GetAssetPath(obj);
                        var modelsInFolder = FindModelsInFolder(folderPath, includeSubfolders);
                        selectedObjects.AddRange(modelsInFolder);
                    }
                }
            }
        }
        
        private static bool IsValidPreviewTarget(Object obj)
        {
            if (obj == null) return false;
            
            // Check if it's a prefab
            if (obj is GameObject gameObject)
            {
                string assetPath = AssetDatabase.GetAssetPath(gameObject);
                if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".prefab"))
                    return true;
            }
            
            // Check if it's a 3D model
            if (obj is Mesh || obj is GameObject)
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    string extension = Path.GetExtension(assetPath).ToLower();
                    return extension == ".fbx" || extension == ".obj" || extension == ".blend" || 
                           extension == ".dae" || extension == ".3ds" || extension == ".max";
                }
            }
            
            return false;
        }
        
        private static bool IsFolder(Object obj)
        {
            if (obj == null) return false;
            
            string assetPath = AssetDatabase.GetAssetPath(obj);
            return !string.IsNullOrEmpty(assetPath) && AssetDatabase.IsValidFolder(assetPath);
        }
        
        private List<Object> FindModelsInFolder(string folderPath, bool recursive)
        {
            List<Object> foundModels = new List<Object>();
            
            if (!AssetDatabase.IsValidFolder(folderPath))
                return foundModels;
            
            // Get all files in the folder
            string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });
            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                
                // Skip if it's a folder and we're not recursive, or if it's a nested folder we don't want
                if (AssetDatabase.IsValidFolder(assetPath))
                    continue;
                
                // Check if we should include this file based on recursion setting
                if (!recursive && Path.GetDirectoryName(assetPath).Replace('\\', '/') != folderPath)
                    continue;
                
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                if (asset != null && IsValidPreviewTarget(asset))
                {
                    foundModels.Add(asset);
                }
            }
            
            return foundModels;
        }
        
        private void GeneratePreviews()
        {
            if (selectedObjects.Count == 0)
            {
                EditorUtility.DisplayDialog("No Objects", "Please select objects to generate previews for.", "OK");
                return;
            }
            
            // Ensure output directory exists
            if (!AssetDatabase.IsValidFolder(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
                AssetDatabase.Refresh();
            }
            
            int successCount = 0;
            int totalCount = selectedObjects.Count;
            
            for (int i = 0; i < totalCount; i++)
            {
                var obj = selectedObjects[i];
                
                EditorUtility.DisplayProgressBar("Generating Previews", 
                    $"Processing {obj.name} ({i + 1}/{totalCount})", (float)i / totalCount);
                
                try
                {
                    if (GeneratePreview(obj))
                        successCount++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to generate preview for {obj.name}: {e.Message}");
                }
            }
            
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            
            string message = $"Preview generation complete!\nSuccessful: {successCount}/{totalCount}";
            if (showInProjectWindow && successCount > 0)
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(outputFolder);
                EditorGUIUtility.PingObject(Selection.activeObject);
            }
            
            EditorUtility.DisplayDialog("Preview Generation Complete", message, "OK");
        }
        
        private bool GeneratePreview(Object obj)
        {
            // Increase cache size to help with preview generation
            AssetPreview.SetPreviewTextureCacheSize(1000);
            
            // Force Unity to "notice" the asset by briefly selecting it
            var originalSelection = Selection.activeObject;
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
            
            // First call to get the asset preview (this starts the async loading)
            Texture2D preview = AssetPreview.GetAssetPreview(obj);
            
            // If preview is null or still loading, wait for it using Unity's proper async checking
            if (preview == null || AssetPreview.IsLoadingAssetPreview(obj.GetInstanceID()))
            {
                var startTime = EditorApplication.timeSinceStartup;
                while ((preview == null || AssetPreview.IsLoadingAssetPreview(obj.GetInstanceID())) && 
                       (EditorApplication.timeSinceStartup - startTime) < 5.0) // Increased timeout
                {
                    // Repaint to allow Unity to continue processing
                    EditorApplication.QueuePlayerLoopUpdate();
                    System.Threading.Thread.Sleep(100);
                    preview = AssetPreview.GetAssetPreview(obj);
                }
            }
            
            // If we got a preview but it's just a mini thumbnail, try once more
            if (preview != null && preview.width <= 64 && preview.height <= 64)
            {
                System.Threading.Thread.Sleep(200);
                var betterPreview = AssetPreview.GetAssetPreview(obj);
                if (betterPreview != null && (betterPreview.width > preview.width || betterPreview.height > preview.height))
                {
                    preview = betterPreview;
                }
            }
            
            // If still no preview, try mini thumbnail as last resort
            if (preview == null)
            {
                preview = AssetPreview.GetMiniThumbnail(obj);
                if (preview != null)
                {
                    Debug.LogWarning($"Only mini thumbnail available for {obj.name} - preview may be low quality");
                }
            }
            
            if (preview == null)
            {
                Debug.LogWarning($"Could not generate any preview for {obj.name}");
                Selection.activeObject = originalSelection;
                return false;
            }
            
            // Log preview info for debugging
            Debug.Log($"Generated preview for {obj.name}: {preview.width}x{preview.height}");
            
            // Resize to target size
            Texture2D finalPreview = ResizeTexture(preview, previewSize, previewSize);
            
            // Restore original selection
            Selection.activeObject = originalSelection;
            
            return SaveTextureToFile(finalPreview, obj.name);
        }
        
        private Texture2D ResizeTexture(Texture2D source, int width, int height)
        {
            if (source.width == width && source.height == height)
                return source;
            
            RenderTexture rt = RenderTexture.GetTemporary(width, height);
            Graphics.Blit(source, rt);
            
            RenderTexture.active = rt;
            Texture2D result = new Texture2D(width, height);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            
            return result;
        }
        
        private bool SaveTextureToFile(Texture2D texture, string objectName)
        {
            try
            {
                byte[] bytes = texture.EncodeToPNG();
                string fileName = $"{objectName}_preview.png";
                string filePath = Path.Combine(outputFolder, fileName);
                
                // Make sure filename is unique
                filePath = AssetDatabase.GenerateUniqueAssetPath(filePath);
                
                File.WriteAllBytes(filePath, bytes);
                
                Debug.Log($"Generated preview: {filePath}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save preview for {objectName}: {e.Message}");
                return false;
            }
        }
    }
} 