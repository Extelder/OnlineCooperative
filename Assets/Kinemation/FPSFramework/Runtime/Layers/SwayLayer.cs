// Designed by Kinemation, 2023

using Kinemation.FPSFramework.Runtime.Core.Components;
using Kinemation.FPSFramework.Runtime.Core.Types;
using UnityEngine;

namespace Kinemation.FPSFramework.Runtime.Layers
{
    public class SwayLayer : AnimLayer
    {
        [Header("Deadzone Rotation")]
        [SerializeField] [Bone] protected Transform headBone;
        [SerializeField] protected FreeAimData freeAimData;
        [SerializeField] protected bool bFreeAim;
        [SerializeField] protected bool useCircleMethod;
        
        protected Vector3 smoothMoveSwayRot;
        protected Vector3 smoothMoveSwayLoc;

        protected Quaternion deadZoneRot;
        protected Vector2 deadZoneRotTarget;
        
        protected float smoothFreeAimAlpha;

        protected Vector2 swayTarget;
        protected Vector3 swayLoc;
        protected Vector3 swayRot;

        public void SetFreeAimEnable(bool enable)
        {
            bFreeAim = enable;
        }

        public override void OnAnimUpdate()
        {
            if (Mathf.Approximately(Time.deltaTime, 0f))
            {
                return;
            }
            
            var master = GetMasterPivot();
            LocRot baseT = new LocRot(master.position, master.rotation);

            freeAimData = GetGunData().freeAimData;

            ApplySway();
            //ApplyFreeAim();
            ApplyMoveSway();

            LocRot newT = new LocRot(GetMasterPivot().position, GetMasterPivot().rotation);
        
            GetMasterPivot().position = Vector3.Lerp(baseT.position, newT.position, smoothLayerAlpha);
            GetMasterPivot().rotation = Quaternion.Slerp(baseT.rotation, newT.rotation, smoothLayerAlpha);
        }

        protected virtual void ApplyFreeAim()
        {
            float deltaRight = GetCharData().deltaAimInput.x;
            float deltaUp = GetCharData().deltaAimInput.y;
            
            if (bFreeAim)
            {
                deadZoneRotTarget.x += deltaUp * freeAimData.scalar;
                deadZoneRotTarget.y += deltaRight * freeAimData.scalar;
            }
            else
            {
                deadZoneRotTarget = Vector2.zero;
            }
            
            deadZoneRotTarget.x = Mathf.Clamp(deadZoneRotTarget.x, -freeAimData.maxValue, freeAimData.maxValue);
            
            if (useCircleMethod)
            {
                var maxY = Mathf.Sqrt(Mathf.Pow(freeAimData.maxValue, 2f) - Mathf.Pow(deadZoneRotTarget.x, 2f));
                deadZoneRotTarget.y = Mathf.Clamp(deadZoneRotTarget.y, -maxY, maxY);
            }
            else
            {
                deadZoneRotTarget.y = Mathf.Clamp(deadZoneRotTarget.y, -freeAimData.maxValue, freeAimData.maxValue);
            }
            
            deadZoneRot.x = CoreToolkitLib.Glerp(deadZoneRot.x, deadZoneRotTarget.x, freeAimData.speed);
            deadZoneRot.y = CoreToolkitLib.Glerp(deadZoneRot.y, deadZoneRotTarget.y, freeAimData.speed);

            Quaternion q = Quaternion.Euler(new Vector3(deadZoneRot.x, deadZoneRot.y, 0f));
            q.Normalize();

            smoothFreeAimAlpha = CoreToolkitLib.Glerp(smoothFreeAimAlpha, bFreeAim ? 1f : 0f, 10f);
            q = Quaternion.Slerp(Quaternion.identity, q, smoothFreeAimAlpha);
            
            CoreToolkitLib.RotateInBoneSpace(GetRootBone().rotation, headBone,q, layerAlpha);
        }

        protected virtual void ApplySway()
        {
            var masterDynamic = GetMasterPivot();
            
            float deltaRight = core.characterData.deltaAimInput.x / Time.deltaTime;
            float deltaUp = core.characterData.deltaAimInput.y / Time.deltaTime; 

            swayTarget += new Vector2(deltaRight, deltaUp) * 0.01f;
            swayTarget.x = CoreToolkitLib.GlerpLayer(swayTarget.x * 0.01f, 0f, 5f);
            swayTarget.y = CoreToolkitLib.GlerpLayer(swayTarget.y * 0.01f, 0f, 5f);

            Vector3 targetLoc = new Vector3(swayTarget.x, swayTarget.y,0f);
            Vector3 targetRot = new Vector3(swayTarget.y, swayTarget.x, swayTarget.x);

            swayLoc = CoreToolkitLib.SpringInterp(swayLoc, targetLoc, ref core.gunData.springData.loc);
            swayRot = CoreToolkitLib.SpringInterp(swayRot, targetRot, ref core.gunData.springData.rot);

            var rot = core.ikRigData.rootBone.rotation;

            CoreToolkitLib.RotateInBoneSpace(rot, masterDynamic, Quaternion.Euler(swayRot), 1f);
            CoreToolkitLib.MoveInBoneSpace(core.ikRigData.rootBone, masterDynamic, swayLoc, 1f);
        }

        protected virtual void ApplyMoveSway()
        {
            var moveRotTarget = new Vector3();
            var moveLocTarget = new Vector3();

            var moveSwayData = GetGunData().moveSwayData;
            var moveInput = GetCharData().moveInput;

            moveRotTarget.x = moveInput.y * moveSwayData.maxMoveRotSway.x;
            moveRotTarget.y = moveInput.x * moveSwayData.maxMoveRotSway.y;
            moveRotTarget.z = moveInput.x * moveSwayData.maxMoveRotSway.z;
            
            moveLocTarget.x = moveInput.x * moveSwayData.maxMoveLocSway.x;
            moveLocTarget.y = moveInput.y * moveSwayData.maxMoveLocSway.y;
            moveLocTarget.z = moveInput.y * moveSwayData.maxMoveLocSway.z;

            smoothMoveSwayRot.x = CoreToolkitLib.Glerp(smoothMoveSwayRot.x, moveRotTarget.x, 3.8f);
            smoothMoveSwayRot.y = CoreToolkitLib.Glerp(smoothMoveSwayRot.y, moveRotTarget.y, 3f);
            smoothMoveSwayRot.z = CoreToolkitLib.Glerp(smoothMoveSwayRot.z, moveRotTarget.z, 5f);
            
            smoothMoveSwayLoc.x = CoreToolkitLib.Glerp(smoothMoveSwayLoc.x, moveLocTarget.x, 2.2f);
            smoothMoveSwayLoc.y = CoreToolkitLib.Glerp(smoothMoveSwayLoc.y, moveLocTarget.y, 3f);
            smoothMoveSwayLoc.z = CoreToolkitLib.Glerp(smoothMoveSwayLoc.z, moveLocTarget.z, 2.5f);
            
            CoreToolkitLib.MoveInBoneSpace(core.ikRigData.rootBone, GetMasterPivot(), 
                smoothMoveSwayLoc, 1f);
            CoreToolkitLib.RotateInBoneSpace(GetMasterPivot().rotation, GetMasterPivot(), 
                Quaternion.Euler(smoothMoveSwayRot), 1f);
        }
    }
}