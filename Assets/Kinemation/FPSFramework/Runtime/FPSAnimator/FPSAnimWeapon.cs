// Designed by Kinemation, 2023

using UnityEngine;
using Kinemation.FPSFramework.Runtime.Core.Types;
using Kinemation.FPSFramework.Runtime.Recoil;

namespace Kinemation.FPSFramework.Runtime.FPSAnimator
{
    public abstract class FPSAnimWeapon : MonoBehaviour
    {
        public WeaponAnimData gunData = new(LocRot.identity);
        public AimOffsetTable aimOffsetTable;
        public RecoilAnimData recoilData;
    
        public FireMode fireMode;
        public float fireRate;
        public int burstAmount;
        public AnimSequence overlayPose;
        public LocRot weaponBone = LocRot.identity;

        // Returns the aim point by default
        public virtual Transform GetAimPoint()
        {
            return gunData.gunAimData.aimPoint;
        }

        public void SetupWeapon()
        {
            Transform FindPoint(Transform target, string searchName)
            {
                foreach (Transform child in target)
                {
                    if (child.name.ToLower().Equals(searchName.ToLower()))
                    {
                        return child;
                    }
                }

                return null;
            }
            
            if (gunData.gunAimData.pivotPoint == null)
            {
                var found = FindPoint(transform, "pivot");
                gunData.gunAimData.pivotPoint = found == null ? new GameObject("PivotPoint").transform : found;
                gunData.gunAimData.pivotPoint.parent = transform;
            }
            
            if (gunData.gunAimData.aimPoint == null)
            {
                var found = FindPoint(transform, "aim");
                gunData.gunAimData.pivotPoint = found == null ? new GameObject("AimPoint").transform : found;
                gunData.gunAimData.pivotPoint.parent = transform;
            }
        }
    }
}