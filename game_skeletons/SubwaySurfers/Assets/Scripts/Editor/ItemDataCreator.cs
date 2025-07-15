using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;

namespace SubwaySurfers.Editor
{
    public static class ItemDataCreator
    {
        private const string ITEMS_FOLDER = "Assets/Data/Items";
        
        [MenuItem("Assets/Create ItemData from Prefab", false, 20)]
        private static void CreateItemDataFromPrefab()
        {
            CreateItemDataFromSelectedPrefabs(false);
        }
        
        [MenuItem("Assets/Create ItemData from All Selected Prefabs", false, 21)]
        private static void CreateItemDataFromAllSelectedPrefabs()
        {
            CreateItemDataFromSelectedPrefabs(true);
        }
        
        [MenuItem("Assets/Create ItemData from Prefab", true)]
        private static bool ValidateCreateItemDataFromPrefab()
        {
            return ValidateSelection(false);
        }
        
        [MenuItem("Assets/Create ItemData from All Selected Prefabs", true)]
        private static bool ValidateCreateItemDataFromAllSelectedPrefabs()
        {
            return ValidateSelection(true);
        }
        
        private static bool ValidateSelection(bool allowMultiple)
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
                return false;
            
            if (!allowMultiple && Selection.gameObjects.Length > 1)
                return false;
            
            // Check if all selected objects are prefabs
            foreach (GameObject obj in Selection.gameObjects)
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".prefab"))
                    return false;
            }
            
            return true;
        }
        
        private static void CreateItemDataFromSelectedPrefabs(bool processAll)
        {
            GameObject[] selectedPrefabs = processAll ? Selection.gameObjects : new GameObject[] { Selection.activeGameObject };
            
            if (selectedPrefabs == null || selectedPrefabs.Length == 0)
            {
                Debug.LogError("No prefabs selected!");
                return;
            }

            int successCount = 0;
            int totalCount = selectedPrefabs.Length;

            foreach (GameObject selectedPrefab in selectedPrefabs)
            {
                if (selectedPrefab == null)
                    continue;

                // Get the asset path to confirm it's a prefab
                string assetPath = AssetDatabase.GetAssetPath(selectedPrefab);
                if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".prefab"))
                {
                    Debug.LogWarning($"Skipping {selectedPrefab.name} - not a prefab!");
                    continue;
                }

                // Extract clean name from prefab name
                string cleanName = ExtractItemName(selectedPrefab.name);
                
                // Create ItemData scriptable object
                ItemData itemData = ScriptableObject.CreateInstance<ItemData>();
                
                // Set the ItemType (assuming these are equipment items based on the naming pattern)
                SetItemType(itemData, selectedPrefab.name);
                
                // Create AssetReference for the prefab
                SetAssetReference(itemData, assetPath);
                
                // Ensure the Items folder exists
                if (!AssetDatabase.IsValidFolder(ITEMS_FOLDER))
                {
                    Directory.CreateDirectory(ITEMS_FOLDER);
                    AssetDatabase.Refresh();
                }
                
                // Create the asset with a clean name
                string fileName = $"{cleanName}.asset";
                string fullPath = Path.Combine(ITEMS_FOLDER, fileName);
                
                // Make sure the filename is unique
                fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
                
                // Create and save the asset
                AssetDatabase.CreateAsset(itemData, fullPath);
                successCount++;
                
                Debug.Log($"Created ItemData: {Path.GetFileName(fullPath)} for prefab: {selectedPrefab.name}");
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            if (successCount == 1)
            {
                // Select the newly created asset if only one was created
                string lastCreatedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(lastCreatedPath))
                {
                    Object lastCreated = AssetDatabase.LoadAssetAtPath<ItemData>(lastCreatedPath);
                    if (lastCreated != null)
                    {
                        Selection.activeObject = lastCreated;
                        EditorGUIUtility.PingObject(lastCreated);
                    }
                }
            }
            
            Debug.Log($"ItemData creation complete: {successCount}/{totalCount} successful");
        }
        
        private static string ExtractItemName(string prefabName)
        {
            // Handle naming pattern: SM_Chr_Attach_[ItemCategory]_[ItemName]_[Number]
            // Examples:
            // SM_Chr_Attach_Backpack_Spacesuit_01 -> Backpack Spacesuit
            // SM_Chr_Attach_Bag_Bear_01 -> Bag Bear
            // SM_Chr_Attach_Hat_Beanie_Earflaps_01 -> Hat Beanie Earflaps
            
            // Remove the prefix "SM_Chr_Attach_" if present
            string cleanName = prefabName;
            if (cleanName.StartsWith("SM_Chr_Attach_"))
            {
                cleanName = cleanName.Substring("SM_Chr_Attach_".Length);
            }
            
            // Remove trailing numbers and underscores (like _01, _02)
            cleanName = Regex.Replace(cleanName, @"_\d+$", "");
            
            // Replace underscores with spaces
            cleanName = cleanName.Replace("_", " ");
            
            // Convert to title case
            cleanName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleanName.ToLower());
            
            return cleanName;
        }
        
        private static void SetItemType(ItemData itemData, string prefabName)
        {
            // Use reflection to set the private setter
            var itemTypeProperty = typeof(ItemData).GetProperty("ItemType");
            if (itemTypeProperty != null)
            {
                // Default to Equipment for character attachments
                ItemType itemType = ItemType.Equipment;
                
                // You can add logic here to determine item type based on prefab name
                // For now, all character attachments are Equipment
                if (prefabName.Contains("Chr_Attach"))
                {
                    itemType = ItemType.Equipment;
                }
                
                itemTypeProperty.SetValue(itemData, itemType);
            }
        }
        
        private static void SetAssetReference(ItemData itemData, string assetPath)
        {
            // Create AssetReference from the prefab path
            var assetRefProperty = typeof(ItemData).GetProperty("Item");
            if (assetRefProperty != null)
            {
                // Get the GUID of the asset
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                
                // Create AssetReference
                AssetReference assetRef = new AssetReference(guid);
                
                assetRefProperty.SetValue(itemData, assetRef);
            }
        }
    }
} 