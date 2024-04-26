// Designed by Kinemation, 2023

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Kinemation.FPSFramework.Runtime.Core.Types
{
    [Serializable, Tooltip("Blend time in seconds")]
    public struct BlendTime
    {
        [Min(0f)] public float blendInTime;
        [Min(0f)] public float blendOutTime;

        public BlendTime(float blendIn, float blendOut)
        {
            blendInTime = blendIn;
            blendOutTime = blendOut;
        }

        public void Validate()
        {
            blendInTime = blendInTime < 0f ? 0f : blendInTime;
            blendOutTime = blendOutTime < 0f ? 0f : blendOutTime;
        }
    }

    public struct CoreAnimPlayable
    {
        public AnimationClipPlayable playableClip;
        public BlendTime blendTime;
        public float cachedWeight;

        public CoreAnimPlayable(PlayableGraph graph, AnimationClip clip)
        {
            playableClip = AnimationClipPlayable.Create(graph, clip);
            blendTime = new BlendTime(0f, 0f);
            cachedWeight = 0f;
        }
        
        public float GetLength()
        {
            return playableClip.IsValid() ? playableClip.GetAnimationClip().length : 0f;
        }

        public void Release()
        {
            if (playableClip.IsValid())
            {
                blendTime = new BlendTime(0f, 0f);
                playableClip.Destroy();
            }
        }
    }
    
    public struct CoreAnimMixer
    {
        public AnimationLayerMixerPlayable mixer;
        
        private List<CoreAnimPlayable> _playables;
        private float _mixerWeight;
        private float _playingWeight;
        private int _playingIndex;
        private bool _bBlendOut;
        
        private AnimCurve[] _curves;
        private Dictionary<string, AnimCurveValue> _curveTable;
        
        public CoreAnimMixer(PlayableGraph graph, int inputCount, bool bBlendOut)
        {
            mixer = AnimationLayerMixerPlayable.Create(graph, inputCount);
            
            _playables = new List<CoreAnimPlayable>();
            for (int i = 0; i < inputCount - 1; i++)
            {
                _playables.Add(new CoreAnimPlayable());
            }
            
            _bBlendOut = bBlendOut;
            _playingIndex = -1;
            _mixerWeight = 1f;
            _playingWeight = 0f;
            _curves = null;
            _curveTable = new Dictionary<string, AnimCurveValue>();
        }

        public void SetAvatarMask(AvatarMask mask)
        {
            for (int i = 1; i <= _playingIndex; i++)
            {
                if (mixer.GetInput(i).IsValid())
                {
                    mixer.SetLayerMaskFromAvatarMask((uint) i, mask);
                }
            }
        }

        public void AddClip(CoreAnimPlayable clip, AvatarMask mask, AnimCurve[] curves = null)
        {
            CacheCurves();
            _curves = curves;
            AddCurves();
            
            UpdatePlayingIndex();
            clip.blendTime.Validate();
            
            mixer.ConnectInput(_playingIndex, clip.playableClip, 0, 0);
            _playables[_playingIndex - 1] = clip;
            mixer.SetLayerMaskFromAvatarMask((uint) _playingIndex, mask);
        }

        public float GetCurveValue(string curveName)
        {
            if (!_curveTable.ContainsKey(curveName)) return 0f;
            return _curveTable[curveName].value;
        }

        public float Update()
        {
            if (!mixer.GetInput(_playingIndex).IsValid())
            {
                return 0f;
            }
            
            BlendInPlayable();
            BlendOutPlayable();
            
            return _playingWeight;
        }

        public void UpdateMixerWeight()
        {
            BlendMixerWeight();
        }
        
        public void SetMixerWeight(float weight)
        {
            _mixerWeight = Mathf.Clamp01(weight);
        }

        private void BlendMixerWeight()
        {
            for (int i = 1; i <= _playingIndex; i++)
            {
                if (!mixer.GetInput(i).IsValid())
                {
                    continue;
                }

                float weight = mixer.GetInputWeight(i);
                mixer.SetInputWeight(i, weight * _mixerWeight);
            }
        }

        // Save curve values
        private void CacheCurves()
        {
            if (_curves == null) return;
            
            foreach (var curve in _curves)
            {
                var newCurve = _curveTable[curve.name];
                newCurve.cache = newCurve.value;
                _curveTable[curve.name] = newCurve;
            }
        }

        private void AddCurves()
        {
            if (_curves == null) return;
            
            foreach (var curve in _curves)
            {
                if (!_curveTable.ContainsKey(curve.name))
                {
                    _curveTable.Add(curve.name, new AnimCurveValue());
                }
            }
        }

        private void UpdatePlayingIndex()
        {
            if (_playingIndex == -1)
            {
                for (int i = 1; i < mixer.GetInputCount(); i++)
                {
                    mixer.DisconnectInput(i);
                    _playables[i - 1].Release();
                }
                _playingIndex = 1;
                return;
            }
            
            // Try to use the next slot
            if (_playingIndex + 1 < mixer.GetInputCount())
            {
                _playingIndex++;
                // Save current weights
                for (int i = 1; i < _playingIndex; i++)
                {
                    var clip = _playables[i - 1];
                    clip.cachedWeight = mixer.GetInputWeight(i);
                    _playables[i - 1] = clip;
                }
                return;
            }

            _playables[0].Release();
            // Reconnect
            for (int i = 1; i < mixer.GetInputCount() - 1; i++)
            {
                if (!mixer.GetInput(i + 1).IsValid())
                {
                    continue;
                }
                
                float inputWeight = mixer.GetInputWeight(i + 1);
                var clip = _playables[i];
                clip.cachedWeight = inputWeight;
                _playables[i - 1] = clip;

                mixer.DisconnectInput(i);
                var source = mixer.GetInput(i + 1);
                mixer.DisconnectInput(i + 1);
                mixer.ConnectInput(i, source, 0, inputWeight);
            }
            
            _playingIndex = mixer.GetInputCount() - 1;
            mixer.DisconnectInput(_playingIndex);
        }

        private void UpdateCurve(string curveName, float value)
        {
            var newCurve = _curveTable[curveName];
            newCurve.target = value;
            _curveTable[curveName] = newCurve;
        }

        private void BlendInCurve(string curveName, float weight)
        {
            var newCurve = _curveTable[curveName];
            newCurve.value = Mathf.Lerp(newCurve.cache, newCurve.target, weight);
            _curveTable[curveName] = newCurve;
        }

        private void BlendInPlayable()
        {
            var animation = _playables[_playingIndex - 1];
            float blendTime = animation.blendTime.blendInTime;
            var time = (float) animation.playableClip.GetTime();
            
            if (_bBlendOut && (time >= animation.GetLength()))
            {
                return;
            }
            
            // todo: use CurveLib easing functions
            float alpha = Mathf.Approximately(blendTime, 0f) ? 1f : time / blendTime;
            _playingWeight = Mathf.Lerp(0f, 1f, alpha);
            mixer.SetInputWeight(_playingIndex, _playingWeight);
            
            BlendOutInactive();

            if (_curves == null) return;

            //Blend curves here
            foreach (var curve in _curves)
            {
                float curveValue = curve.curve != null ? curve.curve.Evaluate(time / animation.GetLength()) : 0f;
                UpdateCurve(curve.name, curveValue);
                BlendInCurve(curve.name, _playingWeight);
            }
        }

        private void BlendOutPlayable()
        {
            if (!_bBlendOut)
            {
                return;
            }

            var animPlayable = _playables[_playingIndex - 1];
            var blendTime = animPlayable.blendTime;
            var time = (float) animPlayable.playableClip.GetTime();
            
            if (time >= animPlayable.GetLength())
            {
                // todo: use CurveLib ease functions
                float alpha = 0f;
                if (Mathf.Approximately(blendTime.blendOutTime, 0f))
                {
                    alpha = 1f;
                }
                else
                {
                    alpha = (time - animPlayable.GetLength()) / blendTime.blendOutTime;
                }
                
                float weight = Mathf.Lerp(_playingWeight, 0f, alpha);
                mixer.SetInputWeight(_playingIndex, weight);

                if (Mathf.Approximately(weight, 0f))
                {
                    mixer.DisconnectInput(_playingIndex);
                    _playables[_playingIndex - 1].Release();
                    _playingIndex = -1;
                    alpha = 1f;
                }
                
                if (_curves == null) return;
                
                // Blend out curves here
                foreach (var curve in _curves)
                {
                    var newCurve = _curveTable[curve.name];
                    newCurve.value *= 1f - alpha;
                    _curveTable[curve.name] = newCurve;
                }
            }
        }

        private void BlendOutInactive()
        {
            if (!_bBlendOut)
            {
                return;
            }
            
            for (int i = 1; i < _playingIndex; i++)
            {
                var animation = _playables[i - 1];
                if (!animation.playableClip.IsValid())
                {
                    continue;
                }

                float weight = Mathf.Lerp(animation.cachedWeight, 0f, _playingWeight);
                mixer.SetInputWeight(i, weight);
                
                if (Mathf.Approximately(weight, 0f))
                {
                    mixer.DisconnectInput(i);
                    _playables[i - 1].Release();
                }
            }
        }
    }
}