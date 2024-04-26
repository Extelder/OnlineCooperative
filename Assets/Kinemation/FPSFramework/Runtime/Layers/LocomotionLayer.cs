// Designed by Kinemation, 2023

using Kinemation.FPSFramework.Runtime.Core.Components;
using Kinemation.FPSFramework.Runtime.Core.Types;
using UnityEngine;

namespace Kinemation.FPSFramework.Runtime.Layers
{
    public enum ReadyPose
    {
        LowReady,
        HighReady
    }

    public class LocomotionLayer : AnimLayer
    {
        [Header("Ready Poses")]
        [SerializeField] public LocRot highReadyPose;
        [SerializeField] public LocRot lowReadyPose;
        [SerializeField] private ReadyPose readyPoseType;
        [SerializeField] private float readyInterpSpeed;
        private float smoothReadyAlpha;
        private float readyPoseAlpha;
        
        // Curve-based animation
        private static readonly int RotX = Animator.StringToHash("RotX");
        private static readonly int RotY = Animator.StringToHash("RotY");
        private static readonly int RotZ = Animator.StringToHash("RotZ");
        private static readonly int LocX = Animator.StringToHash("LocX");
        private static readonly int LocY = Animator.StringToHash("LocY");
        private static readonly int LocZ = Animator.StringToHash("LocZ");

        [Header("Sprint")] 
        [SerializeField] protected AnimationCurve sprintBlendCurve = new (new Keyframe(0f, 0f));
        [SerializeField] protected LocRot sprintPose;

        private float smoothSprintLean;

        public void SetReadyWeight(float weight)
        {
            readyPoseAlpha = Mathf.Clamp01(weight);
        }
        
        public override void OnPreAnimUpdate()
        {
            base.OnPreAnimUpdate();
            smoothLayerAlpha *= 1f - core.animGraph.GetCurveValue("Overlay");
            core.animGraph.SetGraphWeight(1f - smoothLayerAlpha);
            core.ikRigData.weaponBoneWeight = GetCurveValue("WeaponBone");
        }

        public override void OnAnimUpdate()
        {
            ApplyReadyPose();
            ApplyLocomotion();
        }

        private void ApplyReadyPose()
        {
            var master = GetMasterPivot();

            float alpha = readyPoseAlpha * (1f - smoothLayerAlpha) * layerAlpha;
            smoothReadyAlpha = CoreToolkitLib.Glerp(smoothReadyAlpha, alpha, readyInterpSpeed);

            var finalPose = readyPoseType == ReadyPose.HighReady ? highReadyPose : lowReadyPose;
            
            CoreToolkitLib.MoveInBoneSpace(GetRootBone(), master, finalPose.position, smoothReadyAlpha);
            CoreToolkitLib.RotateInBoneSpace(GetRootBone().rotation, master, finalPose.rotation, smoothReadyAlpha);
        }
        
        private void ApplyLocomotion()
        {
            var master = GetMasterPivot();
            var animator = GetAnimator();

            Vector3 curveData = new Vector3();
            curveData.x = animator.GetFloat(RotX);
            curveData.y = animator.GetFloat(RotY);
            curveData.z = animator.GetFloat(RotZ);

            var animRot = Quaternion.Euler(curveData * 100f);
            animRot.Normalize();

            curveData.x = animator.GetFloat(LocX);
            curveData.y = animator.GetFloat(LocY);
            curveData.z = animator.GetFloat(LocZ);
            
            var mouseInput = GetCharData().deltaAimInput;

            smoothSprintLean = CoreToolkitLib.Glerp(smoothSprintLean, 4f * mouseInput.x, 3f);
            smoothSprintLean = Mathf.Clamp(smoothSprintLean, -15f, 15f);

            float alpha = sprintBlendCurve.Evaluate(smoothLayerAlpha);
            float locoAlpha = (1f - alpha) * layerAlpha;
            
            var leanVector = new Vector3(0f, smoothSprintLean, -smoothSprintLean);
            var sprintLean = Quaternion.Slerp(Quaternion.identity,Quaternion.Euler(leanVector), alpha);
            
            CoreToolkitLib.RotateInBoneSpace(GetRootBone().rotation,GetPelvis(),sprintLean, 1f);
            
            CoreToolkitLib.MoveInBoneSpace(GetRootBone(),master,curveData / 100f, locoAlpha);
            CoreToolkitLib.RotateInBoneSpace(GetRootBone().rotation, master,animRot, locoAlpha);

            CoreToolkitLib.MoveInBoneSpace(GetRootBone(),master, sprintPose.position, alpha);
            CoreToolkitLib.RotateInBoneSpace(master.rotation, master, sprintPose.rotation, alpha);
        }
    }
}
