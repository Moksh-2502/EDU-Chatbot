using UnityEngine;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem
{
    public class ItemWorldInfo : MonoBehaviour
    {
        [field: SerializeField] public TransformData PreviewState {get; private set;}


        [ContextMenu("Apply Preview State")]
        public void ApplyPreviewState()
        {
            PreviewState.ApplyToTransform(transform);
        }

        [ContextMenu("Capture Preview State")]
        public void CapturePreviewState()
        {
            PreviewState = PreviewState.FromTransform(transform, true);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        } 
    }
}