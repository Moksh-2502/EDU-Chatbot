using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
using UnityEngine.AddressableAssets;
using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;
namespace SubwaySurfers.Editor
{
    public class GlassesDisplayNameEditor : EditorWindow
    {
        private const string GLASSES_FOLDER = "Assets/Bundles/Items/Glasses";
        
        private Dictionary<string, string> glassesDisplayNames = new Dictionary<string, string>();
        private Vector2 scrollPosition;
        private GlassesDisplayNamesData displayNamesData;
        
        // Cool display names based on the preview images I can see
        private readonly Dictionary<string, string> suggestedNames = new Dictionary<string, string>
        {
            {"glasses1.001", "Jungle Shades"},      // Green glasses with dark lenses
            {"glasses2.001", "Ocean Blues"},        // Blue glasses 
            {"glasses3.001", "Sky Rider"},          // Blue glasses with dark lenses
            {"glasses4.001", "Shadow Hunter"},      // Purple/dark glasses
            {"glasses5.003", "Neon Glow"},          // Green glasses with clear lenses
            {"glasses7.001", "Fire Eyes"},          // Clear glasses with red lenses
            {"glasses8.001", "Ninja Mode"},         // Black glasses
            {"glasses9.001", "Galaxy Quest"},       // Blue glasses with large lenses
            {"glasses10.001", "Forest Ranger"},     // Green glasses with dark lenses
            {"glasses11.001", "Ice Cool"},          // Teal/blue glasses
            {"glasses12.001", "Electric Blue"},     // Blue glasses
            {"glasses13.001", "Pixel Power"},       // Black round glasses
            {"glasses14.001", "Laser Focus"},       // Green glasses with striped lenses
            {"glasses15.001", "Storm Shades"},      // Black glasses with patterns
            {"glasses16.001", "Cosmic Drift"},      // Purple glasses
            {"glasses17.001", "Rainbow Rebel"},     // Green and purple glasses
            {"glasses18.001", "Flame Burst"},       // Brown/red glasses
            {"glasses19.001", "Crystal Clear"},     // Clear glasses
            {"glasses20.001", "Holiday Spirit"},    // Red and green glasses
            {"glasses21.001", "Cool Cat"},          // Black glasses
            {"glasses22.001", "Fire Storm"},        // Red glasses
            {"glasses23.001", "Lava Flow"},         // Red glasses
            {"glasses24.001", "Midnight"},          // Black glasses
            {"glasses25.001", "Sun Flare"},         // Yellow glasses
            {"glasses26.001", "Emerald Edge"},      // Green glasses
            {"glasses27.001", "Ghost Rider"},       // Clear glasses with dark lenses
            {"glasses28.001", "Wind Walker"},       // Clear glasses
            {"glasses29.001", "Earth Shaker"},      // Green glasses
        };
        
        [MenuItem("Tools/Glasses Display Names", false, 102)]
        public static void ShowWindow()
        {
            GetWindow<GlassesDisplayNameEditor>("Glasses Display Names");
        }
        
        private void OnEnable()
        {
            LoadGlassesFiles();
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.LabelField("Glasses Display Names Editor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Assign cool display names that players will see in the game UI", MessageType.Info);
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Suggested Names", GUILayout.Height(30)))
            {
                LoadSuggestedNames();
            }
            
            if (GUILayout.Button("Clear All Names", GUILayout.Height(30)))
            {
                ClearAllNames();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            DrawGlassesList();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Create ItemData Assets", GUILayout.Height(40)))
            {
                CreateItemDataAssets();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void LoadGlassesFiles()
        {
            if (!AssetDatabase.IsValidFolder(GLASSES_FOLDER))
            {
                Debug.LogError($"Glasses folder not found: {GLASSES_FOLDER}");
                return;
            }
            
            string[] prefabFiles = Directory.GetFiles(GLASSES_FOLDER, "*.prefab");
            foreach (string filePath in prefabFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                if (!glassesDisplayNames.ContainsKey(fileName))
                {
                    glassesDisplayNames[fileName] = displayNamesData?.GetDisplayName(fileName) ?? "";
                }
            }
            
            Debug.Log($"Loaded {glassesDisplayNames.Count} glasses prefabs");
        }
        
        private void DrawGlassesList()
        {
            EditorGUILayout.LabelField($"Glasses Display Names ({glassesDisplayNames.Count})", EditorStyles.boldLabel);
            
            var sortedGlasses = glassesDisplayNames.Keys.OrderBy(k => k).ToList();
            
            foreach (string glassesKey in sortedGlasses)
            {
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"File: {glassesKey}", EditorStyles.miniLabel);
                
                // Show suggested name if available
                if (suggestedNames.ContainsKey(glassesKey))
                {
                    EditorGUILayout.LabelField($"Suggested: {suggestedNames[glassesKey]}", EditorStyles.miniLabel);
                }
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Display Name:", GUILayout.Width(80));
                glassesDisplayNames[glassesKey] = EditorGUILayout.TextField(glassesDisplayNames[glassesKey]);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space();
            }
        }
        
        private void LoadSuggestedNames()
        {
            foreach (var kvp in suggestedNames)
            {
                if (glassesDisplayNames.ContainsKey(kvp.Key))
                {
                    glassesDisplayNames[kvp.Key] = kvp.Value;
                }
            }
            
            Debug.Log("Loaded suggested display names based on visual appearance");
        }
        
        private void ClearAllNames()
        {
            var keys = glassesDisplayNames.Keys.ToList();
            foreach (string key in keys)
            {
                glassesDisplayNames[key] = "";
            }
            
            Debug.Log("Cleared all display names");
        }
    
        
        private void CreateItemDataAssets()
        {
            // Ensure the Glasses folder exists
            string glassesDataFolder = "Assets/Data/Items/Glasses";
            if (!AssetDatabase.IsValidFolder(glassesDataFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Data/Items"))
                {
                    AssetDatabase.CreateFolder("Assets/Data", "Items");
                }
                AssetDatabase.CreateFolder("Assets/Data/Items", "Glasses");
                AssetDatabase.Refresh();
            }
            
            int createdCount = 0;
            int skippedCount = 0;
            
            foreach (var kvp in glassesDisplayNames)
            {
                string fileName = kvp.Key;
                string displayName = kvp.Value.Trim();
                
                if (string.IsNullOrEmpty(displayName))
                    continue;
                
                // Check if ItemData already exists
                string itemDataPath = $"{glassesDataFolder}/{displayName.Replace(" ", "_")}.asset";
                
                if (AssetDatabase.LoadAssetAtPath<ItemData>(itemDataPath) != null)
                {
                    Debug.Log($"ItemData already exists for {displayName}");
                    skippedCount++;
                    continue;
                }
                
                // Find the corresponding prefab
                string prefabPath = $"{GLASSES_FOLDER}/{fileName}.prefab";
                if (!File.Exists(prefabPath))
                {
                    Debug.LogWarning($"Prefab not found for {fileName} at {prefabPath}");
                    continue;
                }
                
                // Create new ItemData
                ItemData itemData = ScriptableObject.CreateInstance<ItemData>();
                
                // Set properties using reflection (since setters are private)
                SetItemDataProperties(itemData, displayName, prefabPath);
                
                // Create and save the asset
                string uniquePath = AssetDatabase.GenerateUniqueAssetPath(itemDataPath);
                AssetDatabase.CreateAsset(itemData, uniquePath);
                createdCount++;
                
                Debug.Log($"Created ItemData: {Path.GetFileName(uniquePath)} for {fileName}");
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            string message = $"ItemData creation complete!\n\nCreated: {createdCount}\nSkipped (already exists): {skippedCount}";
            EditorUtility.DisplayDialog("ItemData Creation Complete", message, "OK");
        }
        
        private void SetItemDataProperties(ItemData itemData, string displayName, string prefabPath)
        {
            // Generate a unique ID
            var idProperty = typeof(ItemData).GetProperty("Id");
            if (idProperty != null)
            {
                idProperty.SetValue(itemData, Guid.NewGuid().ToString());
            }
            
            // Set the display name
            var nameProperty = typeof(ItemData).GetProperty("Name");
            if (nameProperty != null)
            {
                nameProperty.SetValue(itemData, displayName);
            }
            
            // Set ItemType to Equipment
            var itemTypeProperty = typeof(ItemData).GetProperty("ItemType");
            if (itemTypeProperty != null)
            {
                itemTypeProperty.SetValue(itemData, ItemType.Equipment);
            }
            
            // Set EquipSlot to Glasses
            var equipSlotProperty = typeof(ItemData).GetProperty("EquipSlot");
            if (equipSlotProperty != null)
            {
                equipSlotProperty.SetValue(itemData, SlotType.Glasses);
            }
            
            // Create AssetReference for the prefab
            var itemProperty = typeof(ItemData).GetProperty("Item");
            if (itemProperty != null)
            {
                string guid = AssetDatabase.AssetPathToGUID(prefabPath);
                AssetReference assetRef = new AssetReference(guid);
                itemProperty.SetValue(itemData, assetRef);
            }
        }
    }
    
    [System.Serializable]
    public class GlassesDisplayNamesData : ScriptableObject
    {
        [SerializeField]
        private List<string> glassesKeys = new List<string>();
        
        [SerializeField]
        private List<string> displayNames = new List<string>();
        
        public void UpdateDisplayNames(Dictionary<string, string> nameDict)
        {
            glassesKeys.Clear();
            displayNames.Clear();
            
            foreach (var kvp in nameDict)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    glassesKeys.Add(kvp.Key);
                    displayNames.Add(kvp.Value);
                }
            }
        }
        
        public string GetDisplayName(string glassesKey)
        {
            int index = glassesKeys.IndexOf(glassesKey);
            return index >= 0 ? displayNames[index] : "";
        }
        
        public Dictionary<string, string> GetAllDisplayNames()
        {
            var result = new Dictionary<string, string>();
            for (int i = 0; i < glassesKeys.Count && i < displayNames.Count; i++)
            {
                result[glassesKeys[i]] = displayNames[i];
            }
            return result;
        }
    }
}