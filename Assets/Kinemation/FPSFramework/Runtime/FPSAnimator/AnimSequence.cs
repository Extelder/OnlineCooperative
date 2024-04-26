// Designed by Kinemation, 2023

using System.Collections.Generic;
using Kinemation.FPSFramework.Runtime.Core.Types;
using UnityEngine;

namespace Kinemation.FPSFramework.Runtime.FPSAnimator
{
    [System.Serializable, CreateAssetMenu(fileName = "NewAnimSequence", menuName = "FPS Animator/AnimSequence")]
    public class AnimSequence : ScriptableObject
    {
        public AnimationClip clip;
        public BlendTime blendTime;
        public List<AnimCurve> curves;
    }
}