// Designed by Kinemation, 2023

using System;
using Kinemation.FPSFramework.Runtime.Core.Components;
using Kinemation.FPSFramework.Runtime.Core.Types;
using UnityEngine;

namespace Kinemation.FPSFramework.Runtime.Layers
{
    public class AdsLayer : AnimLayer
    {
        [Header("SightsAligner")]
        [SerializeField] private EaseMode adsEaseMode;
        [SerializeField] private EaseMode pointAimEaseMode;
        
        [Range(0f, 1f)] public float aimLayerAlphaLoc;
        [Range(0f, 1f)] public float aimLayerAlphaRot;
        [SerializeField] [Bone] protected Transform aimTarget;

        protected bool bAds;
        protected float adsProgress;
        
        protected bool bPointAim;
        protected float pointAimProgress;
        
        protected float adsWeight;
        protected float pointAimWeight;
        
        protected LocRot interpAimPoint;
        protected LocRot viewOffsetCache;
        protected LocRot viewOffset;

        [Obsolete("use `SetAds(bool)` instead")]
        public void SetAdsAlpha(float weight)
        {
            weight = Mathf.Clamp01(weight);
            bAds = !Mathf.Approximately(weight, 0f);
        }
        
        [Obsolete("use `SetPointAim(bool)` instead")]
        public void SetPointAlpha(float weight)
        {
            weight = Mathf.Clamp01(weight);
            bPointAim = !Mathf.Approximately(weight, 0f);
        }
        
        public void SetAds(bool bAiming)
        {
            bAds = bAiming;
            interpAimPoint = bAds ? GetAdsOffset() : interpAimPoint;
        }
        
        public void SetPointAim(bool bAiming)
        {
            bPointAim = bAiming;
        }

        public override void OnAnimStart()
        {
            
        }

        public override void OnAnimUpdate()
        {
            Vector3 baseLoc = GetMasterPivot().position;
            Quaternion baseRot = GetMasterPivot().rotation;
            
            ApplyPointAiming();
            ApplyAiming();
            
            Vector3 postLoc = GetMasterPivot().position;
            Quaternion postRot = GetMasterPivot().rotation;

            GetMasterPivot().position = Vector3.Lerp(baseLoc, postLoc, smoothLayerAlpha);
            GetMasterPivot().rotation = Quaternion.Slerp(baseRot, postRot, smoothLayerAlpha);
        }

        public void CalculateAimData()
        {
            var aimData = GetGunData().gunAimData;
            
            var stateName = aimData.target.stateName.Length > 0
                ? aimData.target.stateName
                : aimData.target.staticPose.name;

            if (GetAnimator() != null)
            {
                GetAnimator().Play(stateName);
                GetAnimator().Update(0f);
            }
            
            // Cache the local data, so we can apply it without issues
            aimData.target.aimLoc = aimData.pivotPoint.InverseTransformPoint(aimTarget.position);
            aimData.target.aimRot = Quaternion.Inverse(aimData.pivotPoint.rotation) * GetRootBone().rotation;
        }

        protected void UpdateAimWeights(float adsRate = 1f, float pointAimRate = 1f)
        {
            adsWeight = CurveLib.Ease(0f, 1f, adsProgress, adsEaseMode);
            pointAimWeight = CurveLib.Ease(0f, 1f, pointAimProgress, pointAimEaseMode);
            
            adsProgress += Time.deltaTime * (bAds ? adsRate : -adsRate);
            pointAimProgress += Time.deltaTime * (bPointAim ? pointAimRate : -pointAimRate);

            adsProgress = Mathf.Clamp(adsProgress, 0f, 1f);
            pointAimProgress = Mathf.Clamp(pointAimProgress, 0f, 1f);
        }

        protected LocRot GetAdsOffset()
        {
            var aimData = GetGunData().gunAimData;
            LocRot adsOffset = new LocRot(Vector3.zero, Quaternion.identity);
            if (aimData.aimPoint != null)
            {
                adsOffset.rotation = Quaternion.Inverse(aimData.pivotPoint.rotation) * aimData.aimPoint.rotation;
                adsOffset.position = -aimData.pivotPoint.InverseTransformPoint(aimData.aimPoint.position);
            }

            return adsOffset;
        }

        protected virtual void ApplyAiming()
        {
            var aimData = GetGunData().gunAimData;
            
            // Base Animation layer
            
            LocRot defaultPose = new LocRot(GetMasterPivot());
            ApplyHandsOffset();
            LocRot handsPose = new LocRot(GetMasterPivot());
            
            GetMasterPivot().position = defaultPose.position;
            GetMasterPivot().rotation = defaultPose.rotation;

            UpdateAimWeights(aimData.aimSpeed, aimData.pointAimSpeed);

            interpAimPoint = CoreToolkitLib.Glerp(interpAimPoint, GetAdsOffset(), aimData.changeSightSpeed);
            
            LocRot additiveAim = aimData.target != null ? new LocRot(aimData.target.aimLoc, aimData.target.aimRot) 
                : new LocRot(Vector3.zero, Quaternion.identity);
            
            Vector3 addAimLoc = additiveAim.position;
            Quaternion addAimRot = additiveAim.rotation;
            
            CoreToolkitLib.MoveInBoneSpace(GetMasterPivot(), GetMasterPivot(), addAimLoc, 1f);
            GetMasterPivot().rotation *= addAimRot;
            CoreToolkitLib.MoveInBoneSpace(GetMasterPivot(), GetMasterPivot(), interpAimPoint.position, 1f);

            addAimLoc = GetMasterPivot().position;
            addAimRot = GetMasterPivot().rotation;

            GetMasterPivot().position = handsPose.position;
            GetMasterPivot().rotation = handsPose.rotation;
            ApplyAbsAim(interpAimPoint.position, interpAimPoint.rotation);

            // Blend between Absolute and Additive
            GetMasterPivot().position = Vector3.Lerp(GetMasterPivot().position, addAimLoc, aimLayerAlphaLoc);
            GetMasterPivot().rotation = Quaternion.Slerp(GetMasterPivot().rotation, addAimRot, aimLayerAlphaRot);

            float aimWeight = Mathf.Clamp01(adsWeight - pointAimWeight);
            
            // Blend Between Non-Aiming and Aiming
            GetMasterPivot().position = Vector3.Lerp(handsPose.position, GetMasterPivot().position, aimWeight);
            GetMasterPivot().rotation = Quaternion.Slerp(handsPose.rotation, GetMasterPivot().rotation, aimWeight);
        }

        protected virtual void ApplyPointAiming()
        {
            var aimData = GetGunData().gunAimData;
            
            CoreToolkitLib.MoveInBoneSpace(GetRootBone(), GetMasterPivot(),
                aimData.pointAimOffset.position, pointAimWeight);
            CoreToolkitLib.RotateInBoneSpace(GetRootBone().rotation, GetMasterPivot(),
                aimData.pointAimOffset.rotation, pointAimWeight);
        }

        protected virtual void ApplyHandsOffset()
        {
            float progress = core.animGraph.GetPoseProgress();
            if (Mathf.Approximately(progress, 0f))
            {
                viewOffsetCache = viewOffset;
            }

            viewOffset = CoreToolkitLib.Lerp(viewOffsetCache, GetGunData().viewOffset, progress);
            
            CoreToolkitLib.MoveInBoneSpace(GetRootBone(), GetMasterPivot(), 
                viewOffset.position, 1f);
            CoreToolkitLib.RotateInBoneSpace(GetRootBone().rotation, GetMasterPivot(), 
                viewOffset.rotation, 1f);
        }

        // Absolute aiming overrides base animation
        protected virtual void ApplyAbsAim(Vector3 loc, Quaternion rot)
        {
            Vector3 offset = -loc - GetPivotOffset();
            GetMasterPivot().position = aimTarget.position;
            GetMasterPivot().rotation = GetRootBone().rotation * rot;
            CoreToolkitLib.MoveInBoneSpace(GetMasterPivot(),GetMasterPivot(), -offset, 1f);
        }
    }
}