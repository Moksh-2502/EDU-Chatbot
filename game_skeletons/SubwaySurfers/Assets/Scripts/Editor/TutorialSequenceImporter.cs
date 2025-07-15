#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using SubwaySurfers.Tutorial.Data;
using SubwaySurfers.Tutorial.Events;

namespace SubwaySurfers.Tutorial.Editor
{
    /// <summary>
    /// Unity Editor utility for importing tutorial sequence JSON files and converting them to ScriptableObjects
    /// </summary>
    public class TutorialSequenceImporter : EditorWindow
    {
        private string jsonFolderPath = "Assets/Scripts/Tutorial/Data/TutorialSequences";
        private string outputFolderPath = "Assets/ScriptableObjects/Education";
        private Vector2 scrollPosition;
        private bool showAdvancedOptions = false;

        [MenuItem("Trash Dash/Tutorial/Sequence Importer")]
        public static void ShowWindow()
        {
            var window = GetWindow<TutorialSequenceImporter>("Tutorial Sequence Importer");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tutorial Obstacle Sequence Importer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This tool imports JSON files and converts them to TutorialObstacleSequence ScriptableObjects.\n" +
                "JSON files should be in the format specified in the tutorial documentation.",
                MessageType.Info);

            EditorGUILayout.Space();

            // Folder paths
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            jsonFolderPath = EditorGUILayout.TextField("JSON Folder Path", jsonFolderPath);
            outputFolderPath = EditorGUILayout.TextField("Output Folder Path", outputFolderPath);

            EditorGUILayout.Space();

            // Quick import buttons
            EditorGUILayout.LabelField("Quick Import", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Import All JSON Files", GUILayout.Height(30)))
            {
                ImportAllJsonFiles();
            }
            if (GUILayout.Button("Refresh File List", GUILayout.Height(30)))
            {
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Advanced options
            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options");
            if (showAdvancedOptions)
            {
                EditorGUILayout.BeginVertical("box");
                
                if (GUILayout.Button("Create Output Folder"))
                {
                    CreateOutputFolder();
                }

                if (GUILayout.Button("Validate All Sequences"))
                {
                    ValidateAllSequences();
                }

                if (GUILayout.Button("Export Sequences to JSON"))
                {
                    ExportSequencesToJson();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            // File list
            EditorGUILayout.LabelField("Available JSON Files", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, "box");
            DisplayJsonFiles();
            EditorGUILayout.EndScrollView();
        }

        private void DisplayJsonFiles()
        {
            if (!Directory.Exists(jsonFolderPath))
            {
                EditorGUILayout.HelpBox($"JSON folder does not exist: {jsonFolderPath}", MessageType.Warning);
                return;
            }

            var jsonFiles = Directory.GetFiles(jsonFolderPath, "*.json", SearchOption.TopDirectoryOnly);
            
            if (jsonFiles.Length == 0)
            {
                EditorGUILayout.HelpBox("No JSON files found in the specified folder.", MessageType.Info);
                return;
            }

            foreach (var filePath in jsonFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var outputAssetPath = Path.Combine(outputFolderPath, fileName + ".asset");
                var assetExists = File.Exists(outputAssetPath);

                EditorGUILayout.BeginHorizontal("box");
                
                // File info
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(fileName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Status: {(assetExists ? "Asset Exists" : "Not Imported")}", 
                    assetExists ? EditorStyles.miniLabel : EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndVertical();

                // Action buttons
                EditorGUILayout.BeginVertical(GUILayout.Width(120));
                
                if (GUILayout.Button(assetExists ? "Update" : "Import"))
                {
                    ImportJsonFile(filePath);
                }

                if (assetExists && GUILayout.Button("Validate"))
                {
                    ValidateSequence(outputAssetPath);
                }

                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
            }
        }

        private void ImportAllJsonFiles()
        {
            if (!Directory.Exists(jsonFolderPath))
            {
                EditorUtility.DisplayDialog("Error", $"JSON folder does not exist: {jsonFolderPath}", "OK");
                return;
            }

            var jsonFiles = Directory.GetFiles(jsonFolderPath, "*.json", SearchOption.TopDirectoryOnly);
            
            if (jsonFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("Info", "No JSON files found to import.", "OK");
                return;
            }

            var imported = 0;
            var failed = 0;

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    ImportJsonFile(filePath);
                    imported++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to import {Path.GetFileName(filePath)}: {ex.Message}");
                    failed++;
                }
            }

            EditorUtility.DisplayDialog("Import Complete", 
                $"Imported: {imported}\nFailed: {failed}", "OK");

            AssetDatabase.Refresh();
        }

        private void ImportJsonFile(string jsonFilePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(jsonFilePath);
            var jsonContent = File.ReadAllText(jsonFilePath);

            // Parse JSON
            TutorialObstacleSequenceJson jsonData;
            try
            {
                jsonData = JsonUtility.FromJson<TutorialObstacleSequenceJson>(jsonContent);
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Failed to parse JSON: {ex.Message}");
            }

            // Create or load existing ScriptableObject
            var outputAssetPath = Path.Combine(outputFolderPath, fileName + ".asset");
            var sequence = AssetDatabase.LoadAssetAtPath<TutorialObstacleSequence>(outputAssetPath);

            if (sequence == null)
            {
                // Create new ScriptableObject
                sequence = CreateInstance<TutorialObstacleSequence>();
                
                // Ensure output directory exists
                CreateOutputFolder();
                
                AssetDatabase.CreateAsset(sequence, outputAssetPath);
            }

            // Apply JSON data to sequence
            jsonData.ApplyToSequence(sequence);

            // Save changes
            EditorUtility.SetDirty(sequence);
            AssetDatabase.SaveAssets();

            Debug.Log($"Successfully imported tutorial sequence: {fileName}");
        }

        private void CreateOutputFolder()
        {
            if (!AssetDatabase.IsValidFolder(outputFolderPath))
            {
                var parentFolder = Path.GetDirectoryName(outputFolderPath);
                var folderName = Path.GetFileName(outputFolderPath);
                
                if (!AssetDatabase.IsValidFolder(parentFolder))
                {
                    Directory.CreateDirectory(parentFolder);
                    AssetDatabase.Refresh();
                }
                
                AssetDatabase.CreateFolder(parentFolder, folderName);
                Debug.Log($"Created output folder: {outputFolderPath}");
            }
        }

        private void ValidateSequence(string assetPath)
        {
            var sequence = AssetDatabase.LoadAssetAtPath<TutorialObstacleSequence>(assetPath);
            if (sequence == null)
            {
                Debug.LogError($"Could not load sequence at: {assetPath}");
                return;
            }

            var isValid = sequence.ValidateConfiguration();
            var message = isValid ? "Sequence validation PASSED" : "Sequence validation FAILED";
            
            Debug.Log($"{sequence.name}: {message}");
            EditorUtility.DisplayDialog("Validation Result", $"{sequence.name}\n{message}", "OK");
        }

        private void ValidateAllSequences()
        {
            var sequences = AssetDatabase.FindAssets("t:TutorialObstacleSequence", new[] { outputFolderPath });
            var validCount = 0;
            var invalidCount = 0;

            foreach (var guid in sequences)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sequence = AssetDatabase.LoadAssetAtPath<TutorialObstacleSequence>(path);
                
                if (sequence != null)
                {
                    if (sequence.ValidateConfiguration())
                    {
                        validCount++;
                        Debug.Log($"✓ {sequence.name}: Validation PASSED");
                    }
                    else
                    {
                        invalidCount++;
                        Debug.LogWarning($"✗ {sequence.name}: Validation FAILED");
                    }
                }
            }

            EditorUtility.DisplayDialog("Validation Complete", 
                $"Valid: {validCount}\nInvalid: {invalidCount}", "OK");
        }

        private void ExportSequencesToJson()
        {
            var sequences = AssetDatabase.FindAssets("t:TutorialObstacleSequence", new[] { outputFolderPath });
            var exported = 0;

            foreach (var guid in sequences)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sequence = AssetDatabase.LoadAssetAtPath<TutorialObstacleSequence>(path);
                
                if (sequence != null)
                {
                    var jsonData = TutorialObstacleSequenceJson.FromSequence(sequence);
                    var jsonContent = JsonUtility.ToJson(jsonData, true);
                    var jsonPath = Path.Combine(jsonFolderPath, sequence.name + ".json");
                    
                    File.WriteAllText(jsonPath, jsonContent);
                    exported++;
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Export Complete", $"Exported {exported} sequences to JSON", "OK");
        }
    }

    /// <summary>
    /// Menu items for quick actions
    /// </summary>
    public static class TutorialSequenceMenuItems
    {
        [MenuItem("Assets/Create/Trash Dash/Tutorial/Import JSON to Sequence", false, 0)]
        public static void ImportSelectedJsonFile()
        {
            var selectedObject = Selection.activeObject;
            if (selectedObject == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a JSON file to import.", "OK");
                return;
            }

            var path = AssetDatabase.GetAssetPath(selectedObject);
            if (!path.EndsWith(".json"))
            {
                EditorUtility.DisplayDialog("Error", "Selected file is not a JSON file.", "OK");
                return;
            }

            try
            {
                var importer = new TutorialSequenceImporter();
                // Use reflection to call private method
                var method = typeof(TutorialSequenceImporter).GetMethod("ImportJsonFile", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(importer, new object[] { path });
                
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Success", "JSON file imported successfully!", "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to import JSON file:\n{ex.Message}", "OK");
            }
        }

        [MenuItem("Assets/Create/Trash Dash/Tutorial/Import JSON to Sequence", true)]
        public static bool ValidateImportSelectedJsonFile()
        {
            var selectedObject = Selection.activeObject;
            if (selectedObject == null) return false;
            
            var path = AssetDatabase.GetAssetPath(selectedObject);
            return path.EndsWith(".json");
        }
    }
}
#endif 