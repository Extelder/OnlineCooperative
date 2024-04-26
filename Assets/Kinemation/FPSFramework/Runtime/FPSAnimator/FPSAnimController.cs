// Designed by Kinemation, 2023

using UnityEngine;
using Kinemation.FPSFramework.Runtime.Camera;
using Kinemation.FPSFramework.Runtime.Core.Components;
using Kinemation.FPSFramework.Runtime.Core.Types;
using Kinemation.FPSFramework.Runtime.Layers;
using Kinemation.FPSFramework.Runtime.Recoil;

namespace Kinemation.FPSFramework.Runtime.FPSAnimator
{
    // Animation Controller Interface
    // Make sure to derive your controller from this class
    public abstract class FPSAnimController : MonoBehaviour
    {
        private CoreAnimComponent fpsAnimator;
        private FPSCamera fpsCamera;
        private LookLayer internalLookLayer;
        protected RecoilAnimation recoilComponent;
        protected CharAnimData charAnimData;
        
        // Used primarily for function calls from Animation Events
        // Runs once at the beginning of the next update
        protected CoreToolkitLib.PostUpdateDelegate queuedAnimEvents;
        
        private CoreToolkitLib.PostUpdateDelegate initialWeaponSetup;

        // A temporary workaround for strange Unity Animator behaviour at Start()
        // for some reason it takes 3 frames to finalize the animator
        private int skippedInitialFrames = 2;
        private FPSAnimWeapon currentWeapon = null;

        // Call this once when the character is initialized
        protected void InitAnimController()
        {
            fpsAnimator = GetComponent<CoreAnimComponent>();
            fpsAnimator.animGraph.InitPlayableGraph();

            recoilComponent = GetComponent<RecoilAnimation>();
            charAnimData = new CharAnimData();

            fpsCamera = GetComponentInChildren<FPSCamera>();
            internalLookLayer = GetComponent<LookLayer>();
        }

        // Call this once when the character is initialized
        protected void InitAnimController(CoreToolkitLib.PostUpdateDelegate cameraDelegate)
        {
            InitAnimController();
            fpsAnimator.OnPostAnimUpdate += cameraDelegate;

            if (fpsCamera == null) return;
            fpsAnimator.OnPostAnimUpdate += fpsCamera.UpdateCamera;
        }

        private void ValidateFirstInit()
        {
            if (skippedInitialFrames > 0)
            {
                skippedInitialFrames--;
                return;
            }
            
            PlayPose(currentWeapon.overlayPose);
            fpsAnimator.OnPoseSampled();
            initialWeaponSetup = null;
        }

        // Call this when equipping a new weapon
        protected void InitWeapon(FPSAnimWeapon weapon)
        {
            // Handled by the weapon bone
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;

            recoilComponent.Init(weapon.recoilData, weapon.fireRate, weapon.fireMode);
            fpsAnimator.OnGunEquipped(weapon.gunData);
            fpsAnimator.ikRigData.weaponTransform = weapon.weaponBone;

            internalLookLayer.SetAimOffsetTable(weapon.aimOffsetTable);
            currentWeapon = weapon;
            
            if (skippedInitialFrames > 0 && initialWeaponSetup == null)
            {
                initialWeaponSetup += ValidateFirstInit;
            }
            else
            {
                PlayPose(currentWeapon.overlayPose);
                fpsAnimator.OnPoseSampled();
            }
        }

        // Call this when changing sights
        protected void InitAimPoint(FPSAnimWeapon weapon)
        {
            fpsAnimator.OnSightChanged(weapon.GetAimPoint());
        }

        // Call this during Update after all the gameplay logic
        protected void UpdateAnimController()
        {
            if (queuedAnimEvents != null)
            {
                queuedAnimEvents.Invoke();
                queuedAnimEvents = null;
            }

            if (initialWeaponSetup != null)
            {
                initialWeaponSetup.Invoke();
            }

            charAnimData.recoilAnim = new LocRot(recoilComponent.OutLoc, Quaternion.Euler(recoilComponent.OutRot));
            fpsAnimator.SetCharData(charAnimData);
            fpsAnimator.animGraph.UpdateGraph();
        }

        // Call this to play a Camera shake
        protected void PlayCameraShake(FPSCameraShake shake)
        {
            if (fpsCamera != null)
            {
                fpsCamera.PlayShake(shake.shakeInfo);
            }
        }

        // Call this to play a static pose on the character upper body
        protected void PlayPose(AnimSequence motion)
        {
            if (motion == null) return;
            fpsAnimator.animGraph.PlayPose(motion.clip, motion.blendTime.blendInTime);
        }

        // Call this to play an animation on the character upper body
        protected void PlayAnimation(AnimSequence motion)
        {
            if (motion == null) return;
            fpsAnimator.animGraph.PlayAnimation(motion.clip, motion.blendTime, motion.curves.ToArray());
        }
    }
}