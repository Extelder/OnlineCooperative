// Designed by Kinemation, 2023

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

using Kinemation.FPSFramework.Runtime.Core.Types;

namespace Kinemation.FPSFramework.Runtime.Core.Components
{
    // DynamicBone is essentially an IK bone
    [Serializable]
    public struct DynamicBone
    {
        [Tooltip("Actual bone")]
        [Bone] public Transform target;
        [Tooltip("Target for elbows/knees")]
        [Bone] public Transform hintTarget;
        [Tooltip("The representation of the DynamicBone in space")]
        public GameObject obj;

        public void Retarget()
        {
            if (target == null)
            {
                return;
            }

            obj.transform.position = target.position;
            obj.transform.rotation = target.rotation;
        }

        public void Rotate(Quaternion parent, Quaternion rotation, float alpha)
        {
            CoreToolkitLib.RotateInBoneSpace(parent, obj.transform, rotation, alpha);
        }

        public void Rotate(Quaternion rotation, float alpha)
        {
            CoreToolkitLib.RotateInBoneSpace(obj.transform.rotation, obj.transform, rotation, alpha);
        }

        public void Move(Transform parent, Vector3 offset, float alpha)
        {
            CoreToolkitLib.MoveInBoneSpace(parent, obj.transform, offset, alpha);
        }

        public void Move(Vector3 offset, float alpha)
        {
            CoreToolkitLib.MoveInBoneSpace(obj.transform, obj.transform, offset, alpha);
        }
    }
    
    // Essential skeleton data, used by Anim Layers
    [Serializable]
    public struct DynamicRigData
    {
        public Animator animator;
        [Bone] public Transform pelvis;
        
        public Transform weaponBone;
        public Transform weaponBoneRight;
        public Transform weaponBoneLeft;
        
        [HideInInspector] public float weaponBoneWeight;
        [HideInInspector] public LocRot weaponTransform;
        
        public DynamicBone masterDynamic;
        public DynamicBone rightHand;
        public DynamicBone leftHand;
        public DynamicBone rightFoot;
        public DynamicBone leftFoot;

        [Tooltip("Used for mesh space calculations")]
        [Bone] public Transform spineRoot;
        [Bone] public Transform rootBone;
        
        public Quaternion GetSpineRootMS()
        {
            return Quaternion.Inverse(rootBone.rotation) * spineRoot.rotation;
        }

        public void RetargetHandBones()
        {
            LocRot target = new LocRot()
            {
                position = rootBone.TransformPoint(weaponTransform.position),
                rotation = rootBone.rotation * weaponTransform.rotation
            };

            weaponBone.position = target.position;
            weaponBone.rotation = target.rotation;

            weaponBoneRight.position = target.position;
            weaponBoneRight.rotation = target.rotation;
            
            weaponBoneLeft.position = target.position;
            weaponBoneLeft.rotation = target.rotation;
        }

        public void RetargetWeaponBone()
        {
            weaponBone.position = rootBone.TransformPoint(weaponTransform.position);
            weaponBone.rotation = rootBone.rotation * weaponTransform.rotation;
        }

        public void UpdateWeaponParent()
        {
            LocRot boneDefault = new LocRot(weaponBoneRight);;
            LocRot boneRight = new LocRot(masterDynamic.obj.transform);
            LocRot boneLeft = new LocRot(weaponBoneLeft);
            
            LocRot result = LocRot.identity;
            if (weaponBoneWeight >= 0f)
            {
                result = CoreToolkitLib.Lerp(boneDefault, boneRight, weaponBoneWeight);
            }
            else
            {
                result = CoreToolkitLib.Lerp(boneDefault, boneLeft, -weaponBoneWeight);
            }
            
            masterDynamic.obj.transform.position = result.position;
            masterDynamic.obj.transform.rotation = result.rotation;
        }

        public void AlignWeaponBone(Vector3 offset)
        {
            if (!Application.isPlaying) return; 
            
            masterDynamic.Move(offset, 1f);
            
            weaponBone.position = masterDynamic.obj.transform.position;
            weaponBone.rotation = masterDynamic.obj.transform.rotation;
        }
        
        public void Retarget()
        {
            rightFoot.Retarget();
            leftFoot.Retarget();
        }
    }
    
    [Serializable]
    public abstract class AnimLayer : MonoBehaviour
    {
        [Header("Layer Blending")] 
        
        [SerializeField] protected string curveName;
        [SerializeField, Range(0f, 1f)] protected float layerAlpha = 1f;
        [SerializeField] protected float lerpSpeed;
        protected float smoothLayerAlpha;
        
        [Header("Misc")]
        [SerializeField] public bool runInEditor;
        protected CoreAnimComponent core;
        
        public void SetLayerAlpha(float weight)
        {
            layerAlpha = Mathf.Clamp01(weight);
        }
        
        // Called in Start()
        public virtual void OnAnimStart()
        {
        }

        // Called each frame to copy IK bones transforms
        public virtual void OnRetarget()
        {
        }
        
        // Called before the main anim update cycle
        public virtual void OnPreAnimUpdate()
        {
            float target = !string.IsNullOrEmpty(curveName) ? GetAnimator().GetFloat(curveName) : layerAlpha;
            smoothLayerAlpha = CoreToolkitLib.GlerpLayer(smoothLayerAlpha, Mathf.Clamp01(target), lerpSpeed);
        }
        
        // Main anim update
        public virtual void OnAnimUpdate()
        {
        }

        // Called after the IK is applied
        public virtual void OnPostIK()
        {
        }
        
        // Called when an overlay pose is sampled
        public virtual void OnPoseSampled()
        {
        }

        protected virtual void Awake()
        {
        }

        public void OnEnable()
        {
            core = GetComponent<CoreAnimComponent>();
        }

        protected WeaponAnimData GetGunData()
        {
            return core.gunData;
        }
        
        protected CharAnimData GetCharData()
        {
            return core.characterData;
        }

        protected Transform GetMasterPivot()
        {
            return core.ikRigData.masterDynamic.obj.transform;
        }

        protected Transform GetRootBone()
        {
            return core.ikRigData.rootBone;
        }

        protected Transform GetPelvis()
        {
            return core.ikRigData.pelvis;
        }
        
        protected DynamicBone GetMasterIK()
        {
            return core.ikRigData.masterDynamic;
        }
        
        protected DynamicBone GetRightHandIK()
        {
            return core.ikRigData.rightHand;
        }
        
        protected DynamicBone GetLeftHandIK()
        {
            return core.ikRigData.leftHand;
        }

        protected Transform GetRightHand()
        {
            return core.ikRigData.rightHand.obj.transform;
        }
        
        protected Transform GetLeftHand()
        {
            return core.ikRigData.leftHand.obj.transform;
        }
        
        protected Transform GetRightFoot()
        {
            return core.ikRigData.rightFoot.obj.transform;
        }
        
        protected Transform GetLeftFoot()
        {
            return core.ikRigData.leftFoot.obj.transform;
        }

        protected Animator GetAnimator()
        {
            return core.ikRigData.animator;
        }

        protected DynamicRigData GetRigData()
        {
            return core.ikRigData;
        }

        protected float GetCurveValue(string curve)
        {
            return core.animGraph.GetCurveValue(curve);
        }
        
        // Offsets master pivot only, without affecting the child IK bones
        // Useful if weapon has multiple pivots
        protected void OffsetMasterPivot(LocRot offset)
        {
            LocRot rightHandTip = new LocRot(GetRightHand());
            LocRot leftHandTip = new LocRot(GetLeftHand());
            
            GetMasterIK().Move(GetMasterPivot(), offset.position, 1f);
            GetMasterIK().Rotate(GetMasterPivot().rotation, offset.rotation, 1f);

            GetRightHand().position = rightHandTip.position;
            GetRightHand().rotation = rightHandTip.rotation;
            
            GetLeftHand().position = leftHandTip.position;
            GetLeftHand().rotation = leftHandTip.rotation;
        }

        protected Vector3 GetPivotOffset()
        {
            var pivotPoint = GetGunData().gunAimData.pivotPoint;
            return pivotPoint != null ? pivotPoint.localPosition : Vector3.zero;
        }
    }
    
    [ExecuteInEditMode, AddComponentMenu("FPS Animator")]
    public class CoreAnimComponent : MonoBehaviour
    {
        public event CoreToolkitLib.PostUpdateDelegate OnPostAnimUpdate;

        [FormerlySerializedAs("rigData")]
        public DynamicRigData ikRigData;
        public CharAnimData characterData;
        public WeaponAnimData gunData;

        [HideInInspector] public CoreAnimGraph animGraph;
        [SerializeField] [HideInInspector] private List<AnimLayer> animLayers;
        [SerializeField] private bool useIK = true;
        
        [SerializeField] private bool drawDebug;

        private bool _updateInEditor;
        private float _interpHands;
        private float _interpLayer;

        // General IK weight for hands
        [SerializeField, Range(0f, 1f)] private float handIkWeight = 1f;
        // Global IK weight for feet
        [SerializeField, Range(0f, 1f)] private float legIkWeight = 1f;

        private Tuple<float, float> rightHandWeight = new(1f, 1f);
        private Tuple<float, float> leftHandWeight = new(1f, 1f);
        private Tuple<float, float> rightFootWeight = new(1f, 1f);
        private Tuple<float, float> leftFootWeight = new(1f, 1f);
        
        private void ApplyIK()
        {
            if (!useIK)
            {
                return;
            }
            
            void SolveIK(DynamicBone tipBone, Tuple<float, float> weights, float sliderWeight)
            {
                if (Mathf.Approximately(sliderWeight, 0f))
                {
                    return;
                }

                float tWeight = sliderWeight * weights.Item1;
                float hWeight = sliderWeight * weights.Item2;
                
                var lowerBone = tipBone.target.parent;
                CoreToolkitLib.SolveTwoBoneIK(lowerBone.parent, lowerBone, tipBone.target,
                    tipBone.obj.transform, tipBone.hintTarget, tWeight, tWeight, hWeight);
            }
            
            SolveIK(ikRigData.rightHand, rightHandWeight, handIkWeight);
            SolveIK(ikRigData.leftHand, leftHandWeight, handIkWeight);
            SolveIK(ikRigData.rightFoot, rightFootWeight, legIkWeight);
            SolveIK(ikRigData.leftFoot, leftFootWeight, legIkWeight);
        }

        private void OnEnable()
        {
            animLayers ??= new List<AnimLayer>();
            animGraph = GetComponent<CoreAnimGraph>();

            if (animGraph == null)
            {
                animGraph = gameObject.AddComponent<CoreAnimGraph>();
            }
            
            foreach (var layer in animLayers)
            {
                layer.OnEnable();
            }
        }

        private void Start()
        {
            foreach (var layer in animLayers)
            {
                layer.OnAnimStart();
            }
        }
        
        private void UpdateWeaponBone()
        {
            animGraph.BeginSample();
            
            ikRigData.RetargetWeaponBone();
            ikRigData.masterDynamic.Retarget();

            Quaternion spineRotMS = ikRigData.GetSpineRootMS();
            
            animGraph.EndSample();
            
            ikRigData.UpdateWeaponParent();

            var pivotPoint = gunData.gunAimData.pivotPoint;
            var pivotOffset = pivotPoint != null ? pivotPoint.localPosition : Vector3.zero;
            ikRigData.masterDynamic.Move(pivotOffset, 1f);

            ikRigData.rightHand.Retarget();
            ikRigData.leftHand.Retarget();

            spineRotMS = ikRigData.rootBone.rotation * spineRotMS;
            ikRigData.spineRoot.rotation = Quaternion.Slerp(ikRigData.spineRoot.rotation, spineRotMS, 
                animGraph.graphWeight);
        }

        private void LateUpdate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && (!_updateInEditor || !animGraph.IsPlaying()))
            {
                DisableEditorPreview();
                return;
            }
#endif
            UpdateWeaponBone();
            Retarget();
            PreUpdateLayers();
            UpdateLayers();
            ApplyIK();
            PostUpdateLayers();
            
            var pivotPoint = gunData.gunAimData.pivotPoint;
            var pivotOffset = pivotPoint != null ? pivotPoint.localPosition : Vector3.zero;
            ikRigData.AlignWeaponBone(-pivotOffset);
            
            OnPostAnimUpdate?.Invoke();
        }

        private void OnDestroy()
        {
            OnPostAnimUpdate = null;
        }

        private void Retarget()
        {
            foreach (var layer in animLayers)
            {
                if (!Application.isPlaying && !layer.runInEditor)
                {
                    continue;
                }
                
                layer.OnRetarget();
            }
            
            ikRigData.Retarget();
        }

        // Called right after retargeting
        private void PreUpdateLayers()
        {
            foreach (var layer in animLayers)
            {
                if (!Application.isPlaying && !layer.runInEditor)
                {
                    continue;
                }
                
                layer.OnPreAnimUpdate();
            }
        }

        private void UpdateLayers()
        {
            foreach (var layer in animLayers)
            {
                if (!Application.isPlaying && !layer.runInEditor)
                {
                    continue;
                }
                
                layer.OnAnimUpdate();
            }
        }

        // Called after IK update
        private void PostUpdateLayers()
        {
            foreach (var layer in animLayers)
            {
                if (!Application.isPlaying && !layer.runInEditor)
                {
                    continue;
                }
                
                layer.OnPostIK();
            }
        }
        
        public void OnPoseSampled()
        {
            ikRigData.RetargetHandBones();
            
            foreach (var layer in animLayers)
            {
                if (!Application.isPlaying && !layer.runInEditor)
                {
                    continue;
                }
                
                layer.OnPoseSampled();
            }
        }

        public void OnGunEquipped(WeaponAnimData gunAimData)
        {
            gunData = gunAimData;
        }

        public void OnSightChanged(Transform newSight)
        {
            gunData.gunAimData.aimPoint = newSight;
        }
        
        public void SetCharData(CharAnimData data)
        {
            characterData = data;
        }

        public void SetRightHandIKWeight(float effector, float hint)
        {
            rightHandWeight = Tuple.Create(effector, hint);
        }
        
        public void SetLeftHandIKWeight(float effector, float hint)
        {
            leftHandWeight = Tuple.Create(effector, hint);
        }

        public void SetRightFootIKWeight(float effector, float hint)
        {
            rightFootWeight = Tuple.Create(effector, hint);
        }
        
        public void SetLeftFootIKWeight(float effector, float hint)
        {
            leftFootWeight = Tuple.Create(effector, hint);
        }
        
        // Editor utils
#if UNITY_EDITOR
        public void EnableEditorPreview()
        {
            if (ikRigData.animator == null)
            {
                ikRigData.animator = GetComponent<Animator>();
            }

            foreach (var layer in animLayers)
            {
                layer.OnEnable();
                layer.OnAnimStart();
            }

            animGraph.StartPreview();
            EditorApplication.QueuePlayerLoopUpdate();
            _updateInEditor = true;
        }

        public void DisableEditorPreview()
        {
            _updateInEditor = false;

            if (ikRigData.animator == null)
            {
                return;
            }

            animGraph.StopPreview();
            ikRigData.animator.Rebind();
            ikRigData.animator.Update(0f);

            ikRigData.weaponBone.localPosition = Vector3.zero;
            ikRigData.weaponBone.localRotation = Quaternion.identity;
        }
        
        private void OnDrawGizmos()
        {
            if (drawDebug)
            {
                Gizmos.color = Color.green;

                void DrawDynamicBone(ref DynamicBone bone, string boneName)
                {
                    if (bone.obj != null)
                    {
                        var loc = bone.obj.transform.position;
                        Gizmos.DrawWireSphere(loc, 0.06f);
                        Handles.Label(loc, boneName);
                    }
                }

                DrawDynamicBone(ref ikRigData.rightHand, "RightHandIK");
                DrawDynamicBone(ref ikRigData.leftHand, "LeftHandIK");
                DrawDynamicBone(ref ikRigData.rightFoot, "RightFootIK");
                DrawDynamicBone(ref ikRigData.leftFoot, "LeftFootIK");

                Gizmos.color = Color.blue;
                if (ikRigData.rootBone != null)
                {
                    var mainBone = ikRigData.rootBone.position;
                    Gizmos.DrawWireCube(mainBone, new Vector3(0.1f, 0.1f, 0.1f));
                    Handles.Label(mainBone, "rootBone");
                }
            }
        }
        
        public void SetupBones()
        {
            if (ikRigData.animator == null)
            {
                ikRigData.animator = GetComponent<Animator>();
            }
            
            if (ikRigData.rootBone == null)
            {
                var root = transform.Find("rootBone");

                if (root != null)
                {
                    ikRigData.rootBone = root.transform;
                }
                else
                {
                    var bone = new GameObject("rootBone");
                    bone.transform.parent = transform;
                    ikRigData.rootBone = bone.transform;
                    ikRigData.rootBone.localPosition = Vector3.zero;
                }
            }

            if (ikRigData.weaponBone == null)
            {
                var gunBone = transform.Find("WeaponBone");

                if (gunBone != null)
                {
                    ikRigData.weaponBone = gunBone.transform;
                }
                else
                {
                    var bone = new GameObject("WeaponBone");
                    bone.transform.parent = ikRigData.rootBone;
                    ikRigData.weaponBone = bone.transform;
                    ikRigData.weaponBone.localPosition = Vector3.zero;
                }
            }

            if (ikRigData.rightFoot.obj == null)
            {
                var bone = transform.Find("RightFootIK");

                if (bone != null)
                {
                    ikRigData.rightFoot.obj = bone.gameObject;
                }
                else
                {
                    ikRigData.rightFoot.obj = new GameObject("RightFootIK");
                    ikRigData.rightFoot.obj.transform.parent = transform;
                    ikRigData.rightFoot.obj.transform.localPosition = Vector3.zero;
                }
            }

            if (ikRigData.leftFoot.obj == null)
            {
                var bone = transform.Find("LeftFootIK");

                if (bone != null)
                {
                    ikRigData.leftFoot.obj = bone.gameObject;
                }
                else
                {
                    ikRigData.leftFoot.obj = new GameObject("LeftFootIK");
                    ikRigData.leftFoot.obj.transform.parent = transform;
                    ikRigData.leftFoot.obj.transform.localPosition = Vector3.zero;
                }
            }

            if (ikRigData.animator.isHuman)
            {
                ikRigData.pelvis = ikRigData.animator.GetBoneTransform(HumanBodyBones.Hips);
                ikRigData.spineRoot = ikRigData.animator.GetBoneTransform(HumanBodyBones.Spine);
                ikRigData.rightHand.target = ikRigData.animator.GetBoneTransform(HumanBodyBones.RightHand);
                ikRigData.rightHand.hintTarget = ikRigData.animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                ikRigData.leftHand.target = ikRigData.animator.GetBoneTransform(HumanBodyBones.LeftHand);
                ikRigData.leftHand.hintTarget = ikRigData.animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                ikRigData.rightFoot.target = ikRigData.animator.GetBoneTransform(HumanBodyBones.RightFoot);
                ikRigData.rightFoot.hintTarget = ikRigData.animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
                ikRigData.leftFoot.target = ikRigData.animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                ikRigData.leftFoot.hintTarget = ikRigData.animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
                
                Transform head = ikRigData.animator.GetBoneTransform(HumanBodyBones.Head);
                SetupIKBones(head);
                return;
            }

            var meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (meshRenderer == null)
            {
                Debug.LogWarning("Core: Skinned Mesh Renderer not found!");
                return;
            }

            var children = meshRenderer.bones;

            bool foundRightHand = false;
            bool foundLeftHand = false;
            bool foundRightFoot = false;
            bool foundLeftFoot = false;
            bool foundHead = false;
            bool foundPelvis = false;

            foreach (var bone in children)
            {
                if (bone.name.ToLower().Contains("ik"))
                {
                    continue;
                }

                bool bMatches = bone.name.ToLower().Contains("hips") || bone.name.ToLower().Contains("pelvis");
                if (!foundPelvis && bMatches)
                {
                    ikRigData.pelvis = bone;
                    foundPelvis = true;
                    continue;
                }

                bMatches = bone.name.ToLower().Contains("lefthand") || bone.name.ToLower().Contains("hand_l")
                                                                    || bone.name.ToLower().Contains("hand l")
                                                                    || bone.name.ToLower().Contains("l hand")
                                                                    || bone.name.ToLower().Contains("l.hand")
                                                                    || bone.name.ToLower().Contains("hand.l");
                if (!foundLeftHand && bMatches)
                {
                    ikRigData.leftHand.target = bone;

                    if (ikRigData.leftHand.hintTarget == null)
                    {
                        ikRigData.leftHand.hintTarget = bone.parent;
                    }

                    foundLeftHand = true;
                    continue;
                }

                bMatches = bone.name.ToLower().Contains("righthand") || bone.name.ToLower().Contains("hand_r")
                                                                     || bone.name.ToLower().Contains("hand r")
                                                                     || bone.name.ToLower().Contains("r hand")
                                                                     || bone.name.ToLower().Contains("r.hand")
                                                                     || bone.name.ToLower().Contains("hand.r");
                if (!foundRightHand && bMatches)
                {
                    ikRigData.rightHand.target = bone;

                    if (ikRigData.rightHand.hintTarget == null)
                    {
                        ikRigData.rightHand.hintTarget = bone.parent;
                    }

                    foundRightHand = true;
                }

                bMatches = bone.name.ToLower().Contains("rightfoot") || bone.name.ToLower().Contains("foot_r")
                                                                     || bone.name.ToLower().Contains("foot r")
                                                                     || bone.name.ToLower().Contains("r foot")
                                                                     || bone.name.ToLower().Contains("r.foot")
                                                                     || bone.name.ToLower().Contains("foot.r");
                if (!foundRightFoot && bMatches)
                {
                    ikRigData.rightFoot.target = bone;
                    ikRigData.rightFoot.hintTarget = bone.parent;

                    foundRightFoot = true;
                }

                bMatches = bone.name.ToLower().Contains("leftfoot") || bone.name.ToLower().Contains("foot_l")
                                                                    || bone.name.ToLower().Contains("foot l")
                                                                    || bone.name.ToLower().Contains("l foot")
                                                                    || bone.name.ToLower().Contains("l.foot")
                                                                    || bone.name.ToLower().Contains("foot.l");
                if (!foundLeftFoot && bMatches)
                {
                    ikRigData.leftFoot.target = bone;
                    ikRigData.leftFoot.hintTarget = bone.parent;

                    foundLeftFoot = true;
                }

                if (!foundHead && bone.name.ToLower().Contains("head"))
                {
                    SetupIKBones(bone);
                    foundHead = true;
                }
            }

            bool bFound = foundRightHand && foundLeftHand && foundRightFoot && foundLeftFoot && foundHead &&
                          foundPelvis;

            Debug.Log(bFound ? "All bones are found!" : "Some bones are missing!");
        }
        
        private void SetupIKBones(Transform head)
        {
            if (ikRigData.masterDynamic.obj == null)
            {
                var boneObject = head.transform.Find("MasterIK");

                if (boneObject != null)
                {
                    ikRigData.masterDynamic.obj = boneObject.gameObject;
                }
                else
                {
                    ikRigData.masterDynamic.obj = new GameObject("MasterIK");
                    ikRigData.masterDynamic.obj.transform.parent = head;
                    ikRigData.masterDynamic.obj.transform.localPosition = Vector3.zero;
                }
            }

            if (ikRigData.rightHand.obj == null)
            {
                var boneObject = head.transform.Find("RightHandIK");

                if (boneObject != null)
                {
                    ikRigData.rightHand.obj = boneObject.gameObject;
                }
                else
                {
                    ikRigData.rightHand.obj = new GameObject("RightHandIK");
                }

                ikRigData.rightHand.obj.transform.parent = ikRigData.masterDynamic.obj.transform;
                ikRigData.rightHand.obj.transform.localPosition = Vector3.zero;
            }

            
            if (ikRigData.leftHand.obj == null)
            {
                var boneObject = head.transform.Find("LeftHandIK");

                if (boneObject != null)
                {
                    ikRigData.leftHand.obj = boneObject.gameObject;
                }
                else
                {
                    ikRigData.leftHand.obj = new GameObject("LeftHandIK");
                }

                ikRigData.leftHand.obj.transform.parent = ikRigData.masterDynamic.obj.transform;
                ikRigData.leftHand.obj.transform.localPosition = Vector3.zero;
            }
            
            if (ikRigData.weaponBoneRight == null)
            {
                var weaponBone = new GameObject("WeaponBoneRight");
                ikRigData.weaponBoneRight = weaponBone.transform;
                ikRigData.weaponBoneRight.parent = ikRigData.rightHand.target;
            }
            
            if (ikRigData.weaponBoneLeft == null)
            {
                var weaponBone = new GameObject("WeaponBoneLeft");
                ikRigData.weaponBoneLeft = weaponBone.transform;
                ikRigData.weaponBoneLeft.parent = ikRigData.leftHand.target;
            }
        }

        public void AddLayer(AnimLayer newLayer)
        {
            animLayers.Add(newLayer);
        }

        public void RemoveLayer(int index)
        {
            if (index < 0 || index > animLayers.Count - 1)
            {
                return;
            }

            var toRemove = animLayers[index];
            animLayers.RemoveAt(index);
            DestroyImmediate(toRemove, true);
        }

        public bool IsLayerUnique(Type layer)
        {
            bool isUnique = true;
            foreach (var item in animLayers)
            {
                if (item.GetType() == layer)
                {
                    isUnique = false;
                    break;
                }
            }

            return isUnique;
        }

        public AnimLayer GetLayer(int index)
        {
            if (index < 0 || index > animLayers.Count - 1)
            {
                return null;
            }

            return animLayers[index];
        }
        
        public bool HasA(AnimLayer item)
        {
            return animLayers.Contains(item);
        }
#endif
    }
}