// Designed by Kinemation, 2023

using System;
using Kinemation.FPSFramework.Runtime.Core.Types;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Kinemation.FPSFramework.Runtime.Core.Components
{
    // Unity Animator sub-system
    [ExecuteInEditMode, Serializable]
    public class CoreAnimGraph : MonoBehaviour
    {
        public float graphWeight;
        [SerializeField] private AvatarMask upperBodyMask;

        [SerializeField, Min(1), Tooltip("Max blending poses")] private int maxPoseCount = 3;
        [SerializeField, Min(1), Tooltip("Max blending clips")] private int maxAnimCount = 3;

        private Animator _animator;
        private PlayableGraph _playableGraph;

        private CoreAnimMixer _overlayPoseMixer;
        private CoreAnimMixer _animMixer;
        private AnimationLayerMixerPlayable _previewMixer;
        
        private float _poseProgress = 0f;
        
#if UNITY_EDITOR
        [SerializeField] [HideInInspector] private AnimationClip previewClip;
        [SerializeField] [HideInInspector] private bool loopPreview;
#endif

        public bool InitPlayableGraph()
        {
            if (_playableGraph.IsValid())
            {
                return true;
            }
            
            _animator = GetComponent<Animator>();
            _playableGraph = _animator.playableGraph;

            if (!_playableGraph.IsValid())
            {
                Debug.LogWarning(gameObject.name + " Animator Controller is null!");
                return false;
            }

            _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            _previewMixer = AnimationLayerMixerPlayable.Create(_playableGraph, 2);
            _animMixer = new CoreAnimMixer(_playableGraph, 1 + maxAnimCount, true);
            var output = AnimationPlayableOutput.Create(_playableGraph, "FPSAnimator", _animator);
            output.SetSourcePlayable(_previewMixer);
            
            _overlayPoseMixer = new CoreAnimMixer(_playableGraph, 1 + maxPoseCount, false);
            
            var controllerPlayable = AnimatorControllerPlayable.Create(_playableGraph, _animator.runtimeAnimatorController);
            _playableGraph.Connect(controllerPlayable, 0, _overlayPoseMixer.mixer, 0);
            _playableGraph.Connect(_overlayPoseMixer.mixer, 0, _animMixer.mixer,0);
            _playableGraph.Connect(_animMixer.mixer, 0, _previewMixer, 0);
            
            // Enable Animator layer by default
            _overlayPoseMixer.mixer.SetInputWeight(0,1f);
            _animMixer.mixer.SetInputWeight(0 ,1f);
            _previewMixer.SetInputWeight(0, 1f);
            _previewMixer.SetInputWeight(1, 0f);
            
            _playableGraph.Play();
            return true;
        }
        
        public void UpdateGraph()
        {
            if (Application.isPlaying)
            {
                _poseProgress = _overlayPoseMixer.Update();
                _animMixer.Update();
            }
        }

        public float GetCurveValue(string curveName)
        {
            return _animMixer.GetCurveValue(curveName);
        }

        public float GetPoseProgress()
        {
            return _poseProgress;
        }

        public void SetGraphWeight(float weight)
        {
            if (!_playableGraph.IsValid())
            {
                return;
            }

            graphWeight = weight;
            _overlayPoseMixer.SetMixerWeight(weight);
            _animMixer.SetMixerWeight(weight);
        }
        
        public void PlayPose(AnimationClip clip, float blendIn, float playRate = 1f)
        {
            if (clip == null)
            {
                return;
            }
            
            CoreAnimPlayable animPlayable = new CoreAnimPlayable(_playableGraph, clip)
            {
                blendTime = new BlendTime(blendIn, 0f)
            };

            animPlayable.playableClip.SetTime(0f);
            animPlayable.playableClip.SetSpeed(playRate);
            _overlayPoseMixer.AddClip(animPlayable, upperBodyMask);
            
            SamplePose(clip);
        }
    
        public void PlayAnimation(AnimationClip clip, BlendTime blendTime, AnimCurve[] curves = null)
        {
            if (clip == null)
            {
                return;
            }
            
            CoreAnimPlayable animPlayable = new CoreAnimPlayable(_playableGraph, clip)
            {
                blendTime = blendTime,
            };

            animPlayable.playableClip.SetTime(0f);
            animPlayable.playableClip.SetSpeed(1f);
            _animMixer.AddClip(animPlayable, upperBodyMask, curves);
        }

        public bool IsPlaying()
        {
            return _playableGraph.IsValid() && _playableGraph.IsPlaying();
        }

        public void BeginSample()
        {
            // Disable the animator layer
            _overlayPoseMixer.mixer.SetInputWeight(0, 0f);
            // Make sure the overlay pose is applied to the whole body
            _overlayPoseMixer.SetAvatarMask(new AvatarMask());
            // Apply graph
            _playableGraph.Evaluate();
        }

        public void EndSample()
        {
            // Enable animator back
            _overlayPoseMixer.mixer.SetInputWeight(0, 1f);
            // Restore original avatar mask
            _overlayPoseMixer.SetAvatarMask(upperBodyMask);
            // Update graph weights. We need to keep the weights as is before the IK bone retargeting
            _overlayPoseMixer.UpdateMixerWeight();
            _animMixer.UpdateMixerWeight();
            // Apply graph
            _playableGraph.Evaluate();
        }

        // Samples overlay static pose, must be called during Update()
        public void SamplePose(AnimationClip clip)
        {
            var samplePlayable = AnimationClipPlayable.Create(_playableGraph, clip);
            _previewMixer.ConnectInput(1, samplePlayable, 0);
            
            _previewMixer.SetInputWeight(0, 0f);
            _previewMixer.SetInputWeight(1, 1f);
            
            _playableGraph.Evaluate();

            _previewMixer.SetInputWeight(0, 1f);
            _previewMixer.SetInputWeight(1, 0f);
            
            _previewMixer.DisconnectInput(1);
        }
        
        private void OnDestroy()
        {
            if (!_playableGraph.IsValid())
            {
                return;
            }

            _playableGraph.Stop();
            _playableGraph.Destroy();
        }

#if UNITY_EDITOR
        private void LoopPreview()
        {
            if (!_playableGraph.IsPlaying())
            {
                EditorApplication.update -= LoopPreview;
            }
            
            if (loopPreview && _playableGraph.IsValid() 
                            && _previewMixer.GetInput(1).GetTime() >= previewClip.length)
            {
                _previewMixer.GetInput(1).SetTime(0f);
            }
        }
        
        public void StartPreview()
        {
            if (!InitPlayableGraph())
            {
                return;
            }

            if (previewClip != null)
            {
                var previewPlayable = AnimationClipPlayable.Create(_playableGraph, previewClip);
                previewPlayable.SetTime(0f);
                previewPlayable.SetSpeed(1f);

                if (_previewMixer.GetInput(1).IsValid())
                {
                    _previewMixer.DisconnectInput(1);
                }

                _previewMixer.ConnectInput(1, previewPlayable, 0, 1f);
                EditorApplication.update += LoopPreview;
            }

            _playableGraph.Play();
        }

        public void StopPreview()
        {
            if (_playableGraph.IsValid())
            {
                _previewMixer.SetInputWeight(1, 0f);
                _previewMixer.DisconnectInput(1);
                _playableGraph.Stop();
            }
            
            EditorApplication.update -= LoopPreview;
        }
#endif
    }
}