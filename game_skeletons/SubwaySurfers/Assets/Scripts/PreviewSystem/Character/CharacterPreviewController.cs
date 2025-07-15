using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace SubwaySurfers.UI.PreviewSystem
{
    /// <summary>
    /// Specialized preview controller for Character objects
    /// Handles character loading, accessory management, and character-specific animations
    /// </summary>
    public class CharacterPreviewController : BasePreviewController<CharacterPreviewData, Character>
    {
        [Header("Character Preview UI")]
        [SerializeField] private CharacterPreviewUI previewUI;
        [SerializeField] private Button dismissButton;

        [Header("Character Settings")]
        [SerializeField] private bool moveCharacterOffscreenDuringLoad = true;
        [SerializeField] private Vector3 offscreenPosition = Vector3.right * 1000;

        // Character-specific state
        private Character _currentCharacterComponent;

        protected override void Awake()
        {
            base.Awake();
            
            // Setup dismiss button
            if (dismissButton != null)
            {
                dismissButton.onClick.AddListener(HidePreview);
            }
        }

        protected override async UniTask OnPreviewStarting(CharacterPreviewData previewData)
        {
            if (previewData == null)
            {
                Debug.LogError("CharacterPreviewController: Invalid preview data type");
                return;
            }

            // Show UI
            if (previewUI != null)
            {
                previewUI.UpdateDisplay(previewData);
                previewUI.Show();
            }

            await UniTask.Yield();
        }

        protected override async UniTask OnPreviewReady(CharacterPreviewData previewData)
        {
            if (previewData == null) return;

            // Get the Character component from the instantiated object
            if (_currentPreviewInstance != null)
            {
                _currentCharacterComponent = _currentPreviewInstance.GetComponent<Character>();
                
                if (_currentCharacterComponent != null)
                {
                    // Handle T-pose flash prevention
                    if (moveCharacterOffscreenDuringLoad)
                    {
                        await PreventTPoseFlash();
                    }
                }
                else
                {
                    Debug.LogWarning("CharacterPreviewController: Character component not found on instantiated object");
                }
            }

            await UniTask.Yield();
        }

        protected override void OnPreviewHiding()
        {
            if (previewUI != null)
            {
                previewUI.Hide();
            }

            _currentCharacterComponent = null;
        }

        protected override void OnPreviewInstanceSetup(GameObject instance, CharacterPreviewData previewData)
        {
            if (instance == null) return;

            // Character-specific setup can be added here
            // For example, adjusting materials, adding effects, etc.
        }
        /// <summary>
        /// Prevents T-pose flash by moving character off-screen during animator initialization
        /// </summary>
        private async UniTask PreventTPoseFlash()
        {
            if (_currentPreviewInstance == null) return;

            // Move character off-screen
            Vector3 originalPosition = _currentPreviewInstance.transform.localPosition;
            _currentPreviewInstance.transform.localPosition = offscreenPosition;

            // Wait for animator to initialize (one frame is usually enough)
            await UniTask.Yield();

            // Move character back to original position
            _currentPreviewInstance.transform.localPosition = originalPosition;
        }

        /// <summary>
        /// Shows a character preview using Character object
        /// </summary>
        /// <param name="character">Character to preview</param>
        /// <param name="position">Position for the character</param>
        /// <param name="accessoryIndex">Index of accessory to show (-1 for none)</param>
        public async UniTask ShowCharacterPreviewAsync(Character character)
        {
            var previewData = CharacterPreviewData.Create(character);
            
            if (previewData != null)
            {
                await ShowPreviewAsync(previewData);
            }
        }
    }
} 