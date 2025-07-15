using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;
using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;

namespace SubwaySurfers.Editor
{
    public class AdvancedItemPreviewGenerator : EditorWindow
    {
        private const string DEFAULT_OUTPUT_FOLDER = "Assets/GeneratedItemPreviews";
        private const int DEFAULT_PREVIEW_SIZE = 1024;
        private const int MAX_PREVIEW_SIZE = 4096;
        
        [System.Serializable]
        public class PreviewSettings
        {
            [Header("Output Settings")]
            public string outputFolder = DEFAULT_OUTPUT_FOLDER;
            public int previewSize = DEFAULT_PREVIEW_SIZE;
            public bool assignIconsToItemData = true;
            public bool generateTransparentBackground = true;
            
            [Header("Camera Settings")]
            public float fieldOfView = 60f;
            public Vector3 cameraRotation = new Vector3(15f, 45f, 0f);
            public Vector3 previewContainerOffset = new Vector3(0f, 0f, 3);
            
            [Header("Auto Framing")]
            public bool enableAutoFraming = true;
            public float framingPadding = 0.1f; // 10% padding around the object
            
            [Header("Render Settings")]
            public bool useHDR = true;
            public int superSampling = 2; // Render at 2x size then downscale
            public bool usePostProcessing = false;
            
            [Header("Background")]
            public Color backgroundColor = new Color(0f, 0f, 0f, 0f);
            public Texture2D backgroundTexture;
        }
        
        private PreviewSettings settings = new PreviewSettings();
        private Vector2 scrollPosition;
        private List<ItemData> selectedItems = new List<ItemData>();
        private bool showPreviewSettings = true;
        private bool showAdvancedSettings = false;
        
        // Preview scene objects
        private GameObject previewSceneRoot;
        private Camera previewCamera;
        private GameObject previewContainer;
        private RenderTexture previewRenderTexture;
        
        [MenuItem("Tools/Item Preview Generator", false, 100)]
        public static void ShowWindow()
        {
            GetWindow<AdvancedItemPreviewGenerator>("Item Preview Generator");
        }
        
        [MenuItem("Assets/Generate Item Preview Icons", false, 30)]
        private static void GeneratePreviewsForSelected()
        {
            var window = GetWindow<AdvancedItemPreviewGenerator>("Item Preview Generator");
            window.RefreshSelectedItems();
            window.Show();
        }
        
        [MenuItem("Assets/Generate Item Preview Icons", true)]
        private static bool ValidateGeneratePreviewsForSelected()
        {
            return Selection.objects != null && Selection.objects.Length > 0 && 
                   Selection.objects.Any(obj => obj is ItemData || IsFolder(obj));
        }
        
        [MenuItem("Tools/Generate All Item Previews", false, 101)]
        private static void GenerateAllItemPreviews()
        {
            var window = GetWindow<AdvancedItemPreviewGenerator>("Item Preview Generator");
            window.FindAllItemData();
            window.GenerateAllPreviews();
        }
        
        private void OnEnable()
        {
            RefreshSelectedItems();
            SetupPreviewScene();
        }
        
        private void OnDisable()
        {
            CleanupPreviewScene();
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.LabelField("Item Preview Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            DrawPreviewSettingsSection();
            EditorGUILayout.Space();
            
            DrawSelectedItemsSection();
            EditorGUILayout.Space();
            
            DrawActionButtons();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawPreviewSettingsSection()
        {
            EditorGUILayout.BeginVertical("box");
            
            showPreviewSettings = EditorGUILayout.Foldout(showPreviewSettings, "Preview Settings", true);
            if (showPreviewSettings)
            {
                EditorGUI.indentLevel++;
                
                // Output Settings
                EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
                settings.outputFolder = EditorGUILayout.TextField("Output Folder", settings.outputFolder);
                settings.previewSize = EditorGUILayout.IntSlider("Preview Size", settings.previewSize, 256, MAX_PREVIEW_SIZE);
                settings.assignIconsToItemData = EditorGUILayout.Toggle("Assign Icons to ItemData", settings.assignIconsToItemData);
                settings.generateTransparentBackground = EditorGUILayout.Toggle("Transparent Background", settings.generateTransparentBackground);
                
                EditorGUILayout.Space();
                
                // Camera Settings
                EditorGUILayout.LabelField("Camera Settings", EditorStyles.boldLabel);
                settings.fieldOfView = EditorGUILayout.Slider("Field of View", settings.fieldOfView, 10f, 120f);
                settings.cameraRotation = EditorGUILayout.Vector3Field("Camera Rotation", settings.cameraRotation);
                settings.previewContainerOffset = EditorGUILayout.Vector3Field("Preview Container Offset", settings.previewContainerOffset);
                
                EditorGUILayout.Space();
                
                // Auto Framing Settings
                EditorGUILayout.LabelField("Auto Framing", EditorStyles.boldLabel);
                settings.enableAutoFraming = EditorGUILayout.Toggle("Enable Auto Framing", settings.enableAutoFraming);
                if (settings.enableAutoFraming)
                {
                    settings.framingPadding = EditorGUILayout.Slider("Framing Padding", settings.framingPadding, 0f, 0.5f);
                    EditorGUILayout.HelpBox("Auto framing will adjust camera distance to fit items tightly in the frame.", MessageType.Info);
                }
                
                EditorGUILayout.Space();
                
                // Advanced Settings
                showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings", true);
                if (showAdvancedSettings)
                {
                    EditorGUI.indentLevel++;
                    
                    settings.useHDR = EditorGUILayout.Toggle("Use HDR", settings.useHDR);
                    settings.superSampling = EditorGUILayout.IntSlider("Super Sampling", settings.superSampling, 1, 4);
                    settings.usePostProcessing = EditorGUILayout.Toggle("Use Post Processing", settings.usePostProcessing);
                    
                    if (!settings.generateTransparentBackground)
                    {
                        settings.backgroundColor = EditorGUILayout.ColorField("Background Color", settings.backgroundColor);
                        settings.backgroundTexture = EditorGUILayout.ObjectField("Background Texture", settings.backgroundTexture, typeof(Texture2D), false) as Texture2D;
                    }
                    
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSelectedItemsSection()
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Selected Items", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                RefreshSelectedItems();
            }
            if (GUILayout.Button("Find All Items", GUILayout.Width(80)))
            {
                FindAllItemData();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField($"Items to process: {selectedItems.Count}", EditorStyles.miniLabel);
            
            // Info about ItemWorldInfo
            EditorGUILayout.HelpBox("Items with 📐 have ItemWorldInfo component for custom preview positioning. " +
                                  "Items without it will use default preview settings.", MessageType.Info);
            
            if (selectedItems.Count == 0)
            {
                EditorGUILayout.HelpBox("No ItemData assets selected. Select ItemData assets or folders containing them in the Project window.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < Mathf.Min(selectedItems.Count, 5); i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(selectedItems[i], typeof(ItemData), false);
                    EditorGUI.EndDisabledGroup();
                    
                    if (selectedItems[i].Item != null && selectedItems[i].Item.RuntimeKeyIsValid())
                    {
                        // Check if the referenced prefab has ItemWorldInfo
                        string assetPath = AssetDatabase.GUIDToAssetPath(selectedItems[i].Item.AssetGUID);
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                        bool hasItemWorldInfo = prefab != null && prefab.GetComponent<ItemWorldInfo>() != null;
                        
                        string status = hasItemWorldInfo ? "✓📐" : "✓";
                        EditorGUILayout.LabelField(status, GUILayout.Width(25));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("✗", GUILayout.Width(25));
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                if (selectedItems.Count > 5)
                {
                    EditorGUILayout.LabelField($"... and {selectedItems.Count - 5} more items", EditorStyles.miniLabel);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUI.BeginDisabledGroup(selectedItems.Count == 0);
            if (GUILayout.Button("Generate All Previews", GUILayout.Height(30)))
            {
                GenerateAllPreviews();
            }
            EditorGUI.EndDisabledGroup();
            
            if (GUILayout.Button("Clear Selection", GUILayout.Height(30), GUILayout.Width(120)))
            {
                selectedItems.Clear();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void SetupPreviewScene()
        {
            CleanupPreviewScene();
            
            // Create root object
            previewSceneRoot = new GameObject("ItemPreviewScene");
            previewSceneRoot.hideFlags = HideFlags.HideAndDontSave;
            
            // Setup camera
            GameObject cameraObject = new GameObject("PreviewCamera");
            cameraObject.transform.SetParent(previewSceneRoot.transform);
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.clearFlags = settings.generateTransparentBackground ? CameraClearFlags.Color : CameraClearFlags.Color;
            previewCamera.backgroundColor = settings.generateTransparentBackground ? Color.clear : settings.backgroundColor;
            previewCamera.fieldOfView = settings.fieldOfView;
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = 100f;
            previewCamera.renderingPath = RenderingPath.Forward;
            
            // Disable shadows on URP camera
            var urpCameraData = cameraObject.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (urpCameraData != null)
            {
                urpCameraData.renderShadows = false;
            }

            // Setup preview container
            previewContainer = new GameObject("PreviewContainer");
            previewContainer.transform.SetParent(previewSceneRoot.transform);
            previewContainer.transform.localPosition = settings.previewContainerOffset;
            
            // Setup render texture
            CreateRenderTexture();
            
            // Apply settings
            ApplyPreviewSettings();
        }
        
        private void CreateRenderTexture()
        {
            if (previewRenderTexture != null)
            {
                DestroyImmediate(previewRenderTexture);
            }
            
            int renderSize = settings.previewSize * settings.superSampling;
            previewRenderTexture = new RenderTexture(renderSize, renderSize, 24, 
                settings.useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            previewRenderTexture.antiAliasing = 8;
            previewRenderTexture.filterMode = FilterMode.Bilinear;
            previewRenderTexture.wrapMode = TextureWrapMode.Clamp;
            
            if (previewCamera != null)
            {
                previewCamera.targetTexture = previewRenderTexture;
            }
        }
        
        private void ApplyPreviewSettings()
        {
            if (previewCamera == null) return;
            
            // Apply camera settings
            previewCamera.transform.rotation = Quaternion.Euler(settings.cameraRotation);
            previewCamera.fieldOfView = settings.fieldOfView;
            previewCamera.backgroundColor = settings.generateTransparentBackground ? Color.clear : settings.backgroundColor;
            
            // Recreate render texture if size changed
            if (previewRenderTexture == null || previewRenderTexture.width != settings.previewSize * settings.superSampling)
            {
                CreateRenderTexture();
            }
        }
        
        private void ApplyAutoFraming(GameObject targetObject)
        {
            if (previewCamera == null || targetObject == null) return;
            
            // Calculate the bounds of the object
            Bounds objectBounds = CalculateObjectBounds(targetObject);
            
            if (objectBounds.size.magnitude == 0) return; // No renderers found
            
            // Calculate the required camera distance to fit the object
            float distance = CalculateRequiredCameraDistance(objectBounds);
            
            // Apply some padding
            distance *= (1f + settings.framingPadding);
            
            // Position the camera
            Vector3 cameraDirection = -previewCamera.transform.forward;
            Vector3 cameraPosition = objectBounds.center + cameraDirection * distance;
            previewCamera.transform.position = cameraPosition;
        }
        
        private Bounds CalculateObjectBounds(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds();
            
            Bounds combinedBounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
            
            return combinedBounds;
        }
        
        private float CalculateRequiredCameraDistance(Bounds bounds)
        {
            // Get the maximum dimension of the bounds
            float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            
            // Calculate distance based on field of view
            float halfFOV = settings.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float distance = (maxDimension * 0.5f) / Mathf.Tan(halfFOV);
            
            return distance;
        }
        

        
        private void CleanupPreviewScene()
        {
            if (previewRenderTexture != null)
            {
                DestroyImmediate(previewRenderTexture);
                previewRenderTexture = null;
            }
            
            if (previewSceneRoot != null)
            {
                DestroyImmediate(previewSceneRoot);
                previewSceneRoot = null;
            }
            
            previewCamera = null;
            previewContainer = null;
        }
        
        private void RefreshSelectedItems()
        {
            selectedItems.Clear();
            
            if (Selection.objects != null)
            {
                foreach (var obj in Selection.objects)
                {
                    if (obj is ItemData itemData)
                    {
                        selectedItems.Add(itemData);
                    }
                    else if (IsFolder(obj))
                    {
                        string folderPath = AssetDatabase.GetAssetPath(obj);
                        var foundItems = FindItemDataInFolder(folderPath);
                        selectedItems.AddRange(foundItems);
                    }
                }
            }
        }
        
        public void FindAllItemData()
        {
            selectedItems.Clear();
            
            string[] guids = AssetDatabase.FindAssets("t:ItemData");
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var itemData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
                if (itemData != null)
                {
                    selectedItems.Add(itemData);
                }
            }
        }
        
        private static bool IsFolder(Object obj)
        {
            if (obj == null) return false;
            string assetPath = AssetDatabase.GetAssetPath(obj);
            return !string.IsNullOrEmpty(assetPath) && AssetDatabase.IsValidFolder(assetPath);
        }
        
        private List<ItemData> FindItemDataInFolder(string folderPath)
        {
            List<ItemData> foundItems = new List<ItemData>();
            
            if (!AssetDatabase.IsValidFolder(folderPath))
                return foundItems;
            
            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { folderPath });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var itemData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
                if (itemData != null)
                {
                    foundItems.Add(itemData);
                }
            }
            
            return foundItems;
        }
        
        public void GenerateAllPreviews()
        {
            if (selectedItems.Count == 0)
            {
                EditorUtility.DisplayDialog("No Items", "Please select ItemData assets to generate previews for.", "OK");
                return;
            }
            
            // Ensure output directory exists
            if (!AssetDatabase.IsValidFolder(settings.outputFolder))
            {
                Directory.CreateDirectory(settings.outputFolder);
                AssetDatabase.Refresh();
            }
            
            SetupPreviewScene();
            
            int successCount = 0;
            int totalCount = selectedItems.Count;
            
            try
            {
                for (int i = 0; i < totalCount; i++)
                {
                    var itemData = selectedItems[i];
                    
                    EditorUtility.DisplayProgressBar("Generating Item Previews", 
                        $"Processing {itemData.Name} ({i + 1}/{totalCount})", (float)i / totalCount);
                    
                    try
                    {
                        if (GeneratePreviewForItem(itemData, false))
                            successCount++;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to generate preview for {itemData.Name}: {e.Message}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
            
            string message = $"Preview generation complete!\nSuccessful: {successCount}/{totalCount}";
            EditorUtility.DisplayDialog("Preview Generation Complete", message, "OK");
        }
        
        private bool GeneratePreviewForItem(ItemData itemData, bool isPreview)
        {
            if (itemData == null || itemData.Item == null || !itemData.Item.RuntimeKeyIsValid())
            {
                Debug.LogWarning($"ItemData {itemData?.Name} has invalid item reference");
                return false;
            }
            
            // Load the referenced asset
            string assetPath = AssetDatabase.GUIDToAssetPath(itemData.Item.AssetGUID);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning($"Could not resolve asset path for {itemData.Name}");
                return false;
            }
            
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogWarning($"Could not load prefab for {itemData.Name}");
                return false;
            }
            
            // Apply default camera settings
            ApplyPreviewSettings();
            
            // Instantiate the object
            GameObject instance = Instantiate(prefab, previewContainer.transform);
            
            // Check for ItemWorldInfo component and apply preview state
            var itemWorldInfo = instance.GetComponent<ItemWorldInfo>();
            if (itemWorldInfo != null)
            {
                itemWorldInfo.ApplyPreviewState();
            }
            
            // Apply auto framing if enabled
            if (settings.enableAutoFraming)
            {
                ApplyAutoFraming(instance);
            }
            
            try
            {
                // Render the preview
                previewCamera.Render();
                
                if (!isPreview)
                {
                    // Save the preview
                    Texture2D finalTexture = RenderTextureToTexture2D(previewRenderTexture);
                    string savedPath = SavePreviewAsSprite(finalTexture, itemData.Name);
                    
                    if (!string.IsNullOrEmpty(savedPath) && settings.assignIconsToItemData)
                    {
                        Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(savedPath);
                        if (iconSprite != null)
                        {
                            AssignIconToItemData(itemData, iconSprite);
                        }
                    }
                    
                    DestroyImmediate(finalTexture);
                }
                
                return true;
            }
            finally
            {
                // Cleanup
                DestroyImmediate(instance);
            }
        }
        
        private Texture2D RenderTextureToTexture2D(RenderTexture renderTexture)
        {
            RenderTexture.active = renderTexture;
            
            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, 
                settings.generateTransparentBackground ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
            
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();
            
            RenderTexture.active = null;
            
            // Downscale if super sampling was used
            if (settings.superSampling > 1)
            {
                texture = ScaleTexture(texture, settings.previewSize, settings.previewSize);
            }
            
            return texture;
        }
        
        private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
            Graphics.Blit(source, rt);
            
            RenderTexture.active = rt;
            Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, false);
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();
            
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            
            DestroyImmediate(source);
            return result;
        }
        
        private string SavePreviewAsSprite(Texture2D texture, string itemName)
        {
            try
            {
                byte[] bytes = texture.EncodeToPNG();
                string fileName = $"{itemName}_icon.png";
                string filePath = Path.Combine(settings.outputFolder, fileName);
                
                // Overwrite existing file if it exists
                if (File.Exists(filePath))
                {
                    Debug.Log($"Overwriting existing preview: {filePath}");
                }
                
                File.WriteAllBytes(filePath, bytes);
                AssetDatabase.Refresh();
                
                // Configure texture import settings
                TextureImporter textureImporter = AssetImporter.GetAtPath(filePath) as TextureImporter;
                if (textureImporter != null)
                {
                    textureImporter.textureType = TextureImporterType.Sprite;
                    textureImporter.spriteImportMode = SpriteImportMode.Single;
                    textureImporter.mipmapEnabled = false;
                    textureImporter.wrapMode = TextureWrapMode.Clamp;
                    textureImporter.filterMode = FilterMode.Bilinear;
                    textureImporter.maxTextureSize = settings.previewSize;
                    textureImporter.alphaIsTransparency = settings.generateTransparentBackground;
                    textureImporter.SaveAndReimport();
                }
                
                Debug.Log($"Generated preview: {filePath}");
                return filePath;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save preview for {itemName}: {e.Message}");
                return null;
            }
        }
        
        private void AssignIconToItemData(ItemData itemData, Sprite iconSprite)
        {
            try
            {
                SerializedObject serializedObject = new SerializedObject(itemData);
                SerializedProperty iconProperty = serializedObject.FindProperty("<Icon>k__BackingField");
                
                if (iconProperty != null)
                {
                    iconProperty.objectReferenceValue = iconSprite;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(itemData);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to assign icon to {itemData.Name}: {e.Message}");
            }
        }
    }
} 