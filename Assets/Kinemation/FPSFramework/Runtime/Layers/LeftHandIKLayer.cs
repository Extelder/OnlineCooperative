// Designed by Kinemation, 2023

using Kinemation.FPSFramework.Runtime.Core.Components;
using Kinemation.FPSFramework.Runtime.Core.Types;
using UnityEngine;

namespace Kinemation.FPSFramework.Runtime.Layers
{
    public class LeftHandIKLayer : AnimLayer
    {
        public Transform leftHandTarget;

        private LocRot _cache;
        private LocRot _final;
        
        public override void OnAnimUpdate()
        {
            LocRot handTransform;
            if (GetGunData().leftHandTarget == null)
            {
                handTransform = leftHandTarget != null ? new LocRot(leftHandTarget) : LocRot.identity;
            }
            else
            {
                var target = GetGunData().leftHandTarget;
                handTransform = new LocRot(target.localPosition, target.localRotation);
            }
            
            var basePos = GetMasterPivot().InverseTransformPoint(GetLeftHand().position);
            var baseRot = Quaternion.Inverse(Quaternion.Inverse(GetMasterPivot().rotation) * GetLeftHand().rotation);

            float alpha = 1f - GetCurveValue("MaskLeftHand");
            
            float progress = core.animGraph.GetPoseProgress();
            if (Mathf.Approximately(progress, 0f))
            {
                _cache = _final;
            }

            handTransform.position = -basePos + handTransform.position;
            handTransform.rotation *= baseRot;
            
            _final = CoreToolkitLib.Lerp(_cache, handTransform, progress);
            
            GetLeftHandIK().Move(GetMasterPivot(), _final.position - GetPivotOffset(), alpha);
            GetLeftHandIK().Rotate(GetMasterPivot().rotation, _final.rotation, alpha);
        }
    }
}