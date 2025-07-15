using UnityEngine;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem
{
    [System.Serializable]
    public struct TransformData
    {
        [field: SerializeField] public bool Local {get; private set;}
        [field: SerializeField] public Pose Pose {get; private set;}
        [field: SerializeField] public Vector3 Scale {get; private set;}

        public TransformData ApplyToTransform(Transform target)
        {
            if(Local)
            {
                target.SetLocalPositionAndRotation(Pose.position, Pose.rotation);
            }
            else
            {
                target.SetPositionAndRotation(Pose.position, Pose.rotation);
            }
            target.localScale = Scale;
            return this;
        }

        public TransformData FromTransform(Transform transform, bool local)
        {
            this.Local = local;
            if(Local)
            {
                this.Pose = new Pose(transform.localPosition, transform.localRotation);
            }
            else{
                this.Pose = new Pose(transform.position, transform.rotation);
            }
            this.Scale= transform.localScale;
            return this;
        }
    }
}