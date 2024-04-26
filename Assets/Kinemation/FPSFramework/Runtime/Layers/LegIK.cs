// Designed by Kinemation, 2023

using Kinemation.FPSFramework.Runtime.Core.Components;
using Kinemation.FPSFramework.Runtime.Core.Types;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Kinemation.FPSFramework.Runtime.Layers
{
    public class LegIK : AnimLayer
    {
        [SerializeField] protected float footTraceLength;
        [SerializeField] protected float footInterpSpeed;
        [SerializeField] protected float pelvisInterpSpeed;

        [SerializeField] protected LayerMask layerName;

        protected LocRot smoothRfIK;
        protected LocRot smoothLfIK;
        protected float smoothPelvis;

        protected Vector3 traceStart;
        protected Vector3 traceEnd;

        private void OnDrawGizmos()
        {
            if (!runInEditor)
            {
                return;
            }
            
            Gizmos.color = Color.red;
            Gizmos.DrawLine(traceStart, traceEnd);
        }

        private LocRot TraceFoot(Transform footTransform)
        {
            Vector3 origin = footTransform.position;
            origin.y = GetPelvis().position.y;
            
            traceStart = origin;
            traceEnd = traceStart - GetRootBone().up * footTraceLength;
            
            LocRot target = new LocRot(footTransform.position, footTransform.rotation);
            Quaternion finalRotation = footTransform.rotation;
            if (Physics.Raycast(origin, -GetRootBone().up, out RaycastHit hit, footTraceLength,layerName))
            {
                var rotation = footTransform.rotation;
                finalRotation = Quaternion.FromToRotation(GetRootBone().up, hit.normal) * rotation;
                finalRotation.Normalize();
                target.position = hit.point;

                float animOffset = GetRootBone().InverseTransformPoint(footTransform.position).y;
                target.position = new Vector3(target.position.x, target.position.y + animOffset, target.position.z);
            }
            
            target.position -= footTransform.position;
            target.rotation = Quaternion.Inverse(footTransform.rotation) * finalRotation;
            
            return target;
        }
        
        public override void OnAnimUpdate()
        {
            var rightFoot = GetRightFoot();
            var leftFoot = GetLeftFoot();
            
            Vector3 rf = rightFoot.position;
            Vector3 lf = leftFoot.position;

            LocRot rfIK = TraceFoot(rightFoot);
            LocRot lfIK = TraceFoot(leftFoot);

            smoothRfIK = CoreToolkitLib.Glerp(smoothRfIK, rfIK, footInterpSpeed);
            smoothLfIK = CoreToolkitLib.Glerp(smoothLfIK, lfIK, footInterpSpeed);
            
            CoreToolkitLib.MoveInBoneSpace(GetRootBone(), rightFoot, smoothRfIK.position, smoothLayerAlpha);
            CoreToolkitLib.MoveInBoneSpace(GetRootBone(), leftFoot, smoothLfIK.position, smoothLayerAlpha);

            rightFoot.rotation *= Quaternion.Slerp(Quaternion.identity, smoothRfIK.rotation, smoothLayerAlpha);
            leftFoot.rotation *= Quaternion.Slerp(Quaternion.identity, smoothLfIK.rotation, smoothLayerAlpha);
            
            var dtR = rightFoot.position - rf;
            var dtL = leftFoot.position - lf;

            float pelvisOffset = dtR.y < dtL.y ? dtR.y : dtL.y;
            smoothPelvis = CoreToolkitLib.Glerp(smoothPelvis, pelvisOffset, pelvisInterpSpeed);
            
            CoreToolkitLib.MoveInBoneSpace(GetRootBone(), GetPelvis(),
                new Vector3(0f, smoothPelvis, 0f), smoothLayerAlpha);
        }
    }
}