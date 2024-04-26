// Designed by Kinemation, 2023

using UnityEngine;

namespace Kinemation.FPSFramework.Runtime.FPSAnimator
{
    public static class FPSAnimLib
    {
        public static float ExpDecay(float a, float b, float speed, float deltaTime)
        {
            return Mathf.Lerp(a, b, 1 - Mathf.Exp(-speed * deltaTime));
        }

        public static Vector2 ExpDecay(Vector2 a, Vector2 b, float speed, float deltaTime)
        {
            return Vector2.Lerp(a, b, 1 - Mathf.Exp(-speed * deltaTime));
        }
    }
}