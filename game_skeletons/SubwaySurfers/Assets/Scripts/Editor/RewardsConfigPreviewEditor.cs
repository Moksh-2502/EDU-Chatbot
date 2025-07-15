using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;
using SubwaySurfers.UI;

namespace SubwaySurfers.Editor
{
    /// <summary>
    /// Editor utility for previewing rewards from RewardsConfig
    /// Adds context menu functionality to iterate through all rewardables
    /// </summary>
    public class RewardsConfigPreviewEditor : EditorWindow
    {
        private static RewardsConfig selectedRewardsConfig;
        private static List<ItemData> allItems = new List<ItemData>();
        private static int currentItemIndex = 0;
        private static RewardPreviewController previewController;

        [MenuItem("Tools/Rewards Preview Window")]
        public static void ShowWindow()
        {
            GetWindow<RewardsConfigPreviewEditor>("Rewards Preview");
        }

        void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rewards Preview Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // RewardsConfig selection
            selectedRewardsConfig = (RewardsConfig)EditorGUILayout.ObjectField(
                "Rewards Config", selectedRewardsConfig, typeof(RewardsConfig), false);

            EditorGUILayout.Space();

            if (selectedRewardsConfig != null)
            {
                RefreshItemsList();

                EditorGUILayout.LabelField($"Total Items: {allItems.Count}");
                
                if (allItems.Count > 0)
                {
                    EditorGUILayout.LabelField($"Current Item: {currentItemIndex + 1} / {allItems.Count}");
                    
                    var currentItem = allItems[currentItemIndex];
                    EditorGUILayout.LabelField($"Item Name: {currentItem.Name ?? "Unnamed"}");
                    EditorGUILayout.LabelField($"Item Type: {currentItem.ItemType}");
                    if (currentItem.ItemType == ItemType.Equipment)
                    {
                        EditorGUILayout.LabelField($"Equip Slot: {currentItem.EquipSlot}");
                    }

                    EditorGUILayout.Space();

                    // Preview controls
                    EditorGUILayout.BeginHorizontal();
                    
                    if (GUILayout.Button("Previous", GUILayout.Height(30)))
                    {
                        PreviewPrevious();
                    }
                    
                    if (GUILayout.Button("Preview Current", GUILayout.Height(30)))
                    {
                        PreviewCurrent();
                    }
                    
                    if (GUILayout.Button("Next", GUILayout.Height(30)))
                    {
                        PreviewNext();
                    }
                    
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();

                    if (GUILayout.Button("Hide Preview", GUILayout.Height(25)))
                    {
                        HidePreview();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No items found in the selected RewardsConfig.", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Please select a RewardsConfig to begin previewing.", MessageType.Info);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview Controller Status:");
            
            FindPreviewController();
            if (previewController != null)
            {
                EditorGUILayout.LabelField($"✓ Found: {previewController.name}");
            }
            else
            {
                EditorGUILayout.HelpBox("RewardPreviewController not found in scene. Please ensure the Main scene is loaded.", MessageType.Warning);
            }
        }

        private static void RefreshItemsList()
        {
            allItems.Clear();
            
            if (selectedRewardsConfig?.Rewards != null)
            {
                foreach (var rewardData in selectedRewardsConfig.Rewards)
                {
                    if (rewardData?.Items != null)
                    {
                        allItems.AddRange(rewardData.Items.Where(item => item != null));
                    }
                }
            }

            // Ensure current index is within bounds
            if (currentItemIndex >= allItems.Count)
            {
                currentItemIndex = 0;
            }
        }

        private static void FindPreviewController()
        {
            if (previewController == null)
            {
                previewController = GameObject.FindAnyObjectByType<RewardPreviewController>();
            }
        }

        private static void PreviewCurrent()
        {
            if (allItems.Count == 0 || currentItemIndex >= allItems.Count)
            {
                Debug.LogWarning("No items available to preview.");
                return;
            }

            FindPreviewController();
            if (previewController == null)
            {
                EditorUtility.DisplayDialog("Preview Controller Missing", 
                    "Could not find RewardPreviewController in the scene.\n\nPlease ensure the Main scene is loaded and contains the preview system.", 
                    "OK");
                return;
            }

            var currentItem = allItems[currentItemIndex];
            Debug.Log($"Previewing item: {currentItem.Name ?? "Unnamed"} ({currentItemIndex + 1}/{allItems.Count})");
            previewController.ShowPreview(currentItem);
        }

        private static void PreviewNext()
        {
            if (allItems.Count == 0) return;
            
            currentItemIndex = (currentItemIndex + 1) % allItems.Count;
            PreviewCurrent();
        }

        private static void PreviewPrevious()
        {
            if (allItems.Count == 0) return;
            
            currentItemIndex = (currentItemIndex - 1 + allItems.Count) % allItems.Count;
            PreviewCurrent();
        }

        private static void HidePreview()
        {
            FindPreviewController();
            if (previewController != null)
            {
                previewController.HidePreview();
                Debug.Log("Preview hidden.");
            }
        }

        #region Context Menu Items for RewardsConfig
        
        [MenuItem("CONTEXT/RewardsConfig/Preview First Reward")]
        private static void PreviewFirstReward(MenuCommand command)
        {
            var rewardsConfig = command.context as RewardsConfig;
            if (rewardsConfig == null) return;

            selectedRewardsConfig = rewardsConfig;
            RefreshItemsList();
            currentItemIndex = 0;
            PreviewCurrent();
        }

        [MenuItem("CONTEXT/RewardsConfig/Preview Next Reward")]
        private static void PreviewNextReward(MenuCommand command)
        {
            var rewardsConfig = command.context as RewardsConfig;
            if (rewardsConfig == null) return;

            selectedRewardsConfig = rewardsConfig;
            RefreshItemsList();
            PreviewNext();
        }

        [MenuItem("CONTEXT/RewardsConfig/Preview Previous Reward")]
        private static void PreviewPreviousReward(MenuCommand command)
        {
            var rewardsConfig = command.context as RewardsConfig;
            if (rewardsConfig == null) return;

            selectedRewardsConfig = rewardsConfig;
            RefreshItemsList();
            PreviewPrevious();
        }

        [MenuItem("CONTEXT/RewardsConfig/Hide Preview")]
        private static void HideRewardPreview(MenuCommand command)
        {
            HidePreview();
        }

        [MenuItem("CONTEXT/RewardsConfig/Open Preview Window")]
        private static void OpenPreviewWindow(MenuCommand command)
        {
            var rewardsConfig = command.context as RewardsConfig;
            selectedRewardsConfig = rewardsConfig;
            ShowWindow();
        }

        #endregion

        void OnDestroy()
        {
            // Clean up when window is closed
            HidePreview();
        }
    }
} 