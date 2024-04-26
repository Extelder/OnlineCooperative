// Designed by Kinemation, 2023

using System;
using UnityEngine;

namespace Kinemation.FPSFramework.Runtime.Core.Types
{
    [Serializable]
    public struct FreeAimData
    {
        public float scalar;
        public float maxValue;
        public float speed;
    }

    [Serializable]
    public struct MoveSwayData
    {
        public Vector3 maxMoveLocSway;
        public Vector3 maxMoveRotSway;
    }
    
    [Serializable]
    public struct GunBlockData
    {
        public float weaponLength;
        public float startOffset;
        public float threshold;
        public LocRot restPose;

        public GunBlockData(LocRot pose)
        {
            restPose = pose;
            weaponLength = startOffset = threshold = 0f;
        }
    }

    [Serializable]
    public struct GunAimData
    {
        public TargetAimData target;
        public Transform pivotPoint;
        public Transform aimPoint;
        public LocRot pointAimOffset;
        public float aimSpeed;
        public float changeSightSpeed;
        public float pointAimSpeed;

        public GunAimData(LocRot pointAimOffset)
        {
            target = null;
            pivotPoint = aimPoint = null;
            this.pointAimOffset = pointAimOffset;
            aimSpeed = changeSightSpeed = pointAimSpeed = 1f;
        }
    }

    // Defines weapon-related properties, updated when weapon is equipped/unequipped
    [Serializable]
    public struct WeaponAnimData
    {
        [Header("LeftHandIK")]
        public Transform leftHandTarget;
        
        [Header("AdsLayer")]
        public GunAimData gunAimData;
        public LocRot viewOffset;
        [Header("SwayLayer")]
        public LocRotSpringData springData;
        public FreeAimData freeAimData;
        public MoveSwayData moveSwayData;
        [Header("WeaponCollision")] 
        public GunBlockData blockData;

        public WeaponAnimData(LocRot viewOffset)
        {
            leftHandTarget = null;
            gunAimData = new GunAimData(LocRot.identity);
            this.viewOffset = viewOffset;
            springData = new LocRotSpringData();
            freeAimData = new FreeAimData();
            moveSwayData = new MoveSwayData();
            blockData = new GunBlockData(LocRot.identity);
        }
    }
}