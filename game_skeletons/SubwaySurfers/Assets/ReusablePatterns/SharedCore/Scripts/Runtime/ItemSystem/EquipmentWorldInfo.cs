using UnityEngine;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem
{
    public class EquipmentWorldInfo : ItemWorldInfo
    {
        [field: SerializeField] public TransformData EquipState {get; private set;}

        [ContextMenu("Apply Equip State")]
        public void ApplyEquipState()
        {
            EquipState.ApplyToTransform(transform);
        }

        [ContextMenu("Capture Equip State")]
        public void CaptureEquipState()
        {
            EquipState = EquipState.FromTransform(transform, true);
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
    }
}