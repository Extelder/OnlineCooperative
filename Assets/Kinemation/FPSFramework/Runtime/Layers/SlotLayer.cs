// Designed by Kinemation, 2023

using System;
using Kinemation.FPSFramework.Runtime.Core.Components;
using Kinemation.FPSFramework.Runtime.Core.Types;
using UnityEngine;

namespace Kinemation.FPSFramework.Runtime.Layers
{
    // Procedural animation
    [Serializable]
    public struct DynamicMotion
    {
        // Curve-based animation
        public VectorCurve rot;
        public VectorCurve loc;
        
        // How fast to blend to another motion
        [SerializeField] private float blendSpeed;
        [SerializeField] private float playRate;
        
        private float playBack;
        private float blendAlpha;
        
        public LocRot outMotion;
        // Used to blend to the currentMotion
        private LocRot cachedMotion;
        private float motionLength;

        public void Reset()
        {
            outMotion = cachedMotion = new LocRot(Vector3.zero, Quaternion.identity);
        }

        public void Play(ref DynamicMotion previousMotion)
        {
            cachedMotion = previousMotion.outMotion;
            playRate = Mathf.Approximately(playRate, 0f) ? 1f : playRate;
            motionLength = Mathf.Max(loc.GetLastTime(), rot.GetLastTime());
            playBack = 0f;
            blendAlpha = 0f;
        }

        private LocRot Evaluate()
        {
            return new LocRot(loc.Evaluate(playBack), Quaternion.Euler(rot.Evaluate(playBack)));
        }

        // Return currently playing motion
        public void UpdateMotion()
        {
            if (Mathf.Approximately(playBack, motionLength))
            {
                outMotion = new LocRot(Vector3.zero, Quaternion.identity);
                return;
            }
            
            playBack += Time.deltaTime * playRate;
            playBack = Mathf.Clamp(playBack, 0f, motionLength);
            var currentMotion = Evaluate();

            blendAlpha += Time.deltaTime * blendSpeed;
            blendAlpha = Mathf.Min(1f, blendAlpha);

            var result = CoreToolkitLib.Lerp(cachedMotion, currentMotion, blendAlpha);
            outMotion = result;
        }
    }

    public struct MotionPlayer
    {
        private DynamicMotion motion;

        public void Reset()
        {
            motion.Reset();
        }
        
        public void Play(DynamicMotion motionToPlay)
        {
            var cache = motion;
            motion = motionToPlay;
            motion.Reset();
            motion.Play(ref cache);
        }

        public void UpdateMotion()
        {
            motion.UpdateMotion();
        }

        public LocRot Get()
        {
            return motion.outMotion;
        }
    }

    public class SlotLayer : AnimLayer
    {
        public MotionPlayer motionPlayer;
        
        public void PlayMotion(DynamicMotion motionToPlay)
        {
            motionPlayer.Play(motionToPlay);
        }

        public override void OnAnimStart()
        {
            motionPlayer.Reset();
        }

        public override void OnAnimUpdate()
        {
            motionPlayer.UpdateMotion();
            
            CoreToolkitLib.MoveInBoneSpace(GetRootBone(), GetMasterPivot(), motionPlayer.Get().position, 
                smoothLayerAlpha);
            CoreToolkitLib.RotateInBoneSpace(GetRootBone().rotation, GetMasterPivot(), 
                motionPlayer.Get().rotation, smoothLayerAlpha);
        }
    }
}