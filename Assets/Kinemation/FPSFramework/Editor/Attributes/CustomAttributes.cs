// Designed by Kinemation, 2023

using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

using Kinemation.FPSFramework.Runtime.Core.Types;

namespace Kinemation.FPSFramework.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(BoneAttribute))]
    public class BoneDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            SkinnedMeshRenderer skinnedMeshRenderer = GetSkinnedMeshRenderer(property);

            if (skinnedMeshRenderer == null)
            {
                Debug.Log("Mesh renderer is null!");
            }

            if (skinnedMeshRenderer != null && property.propertyType == SerializedPropertyType.ObjectReference)
            {
                float buttonWidthRatio = 0.5f;

                // Calculate label width
                float labelWidth = EditorGUIUtility.labelWidth;
                float indentLevel = EditorGUI.indentLevel;

                // Calculate button width and property field width
                float totalWidth = position.width - indentLevel - labelWidth;
                float buttonWidth = totalWidth * buttonWidthRatio;
                float propertyFieldWidth = totalWidth * (1 - buttonWidthRatio);

                // Display the default property field
                Rect propertyFieldRect = new Rect(position.x + indentLevel, position.y,
                    labelWidth + propertyFieldWidth, position.height);
                EditorGUI.PropertyField(propertyFieldRect, property, label, true);

                // Display the bone selection button
                Rect buttonRect = new Rect(position.x + indentLevel + labelWidth + propertyFieldWidth, position.y,
                    buttonWidth, EditorGUIUtility.singleLineHeight);
                Transform currentBone = property.objectReferenceValue as Transform;
                int currentIndex = currentBone != null
                    ? System.Array.IndexOf(skinnedMeshRenderer.bones, currentBone)
                    : -1;
                var boneOptions = skinnedMeshRenderer.bones.Select(b => b.name).ToArray();
                string currentBoneName = currentIndex >= 0 ? boneOptions[currentIndex] : "Select a Bone";

                if (GUI.Button(buttonRect, currentBoneName))
                {
                    BoneSelectionPopup.ShowWindow(boneOptions, selectedIndex =>
                    {
                        property.objectReferenceValue = skinnedMeshRenderer.bones[selectedIndex];
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
            }

            EditorGUI.EndProperty();
        }

        private SkinnedMeshRenderer GetSkinnedMeshRenderer(SerializedProperty property)
        {
            Object targetObject = property.serializedObject.targetObject;

            Component targetComponent = targetObject as Component;
            if (targetComponent != null)
            {
                return targetComponent.GetComponentInChildren<SkinnedMeshRenderer>();
            }

            return null;
        }
    }

    [CustomPropertyDrawer(typeof(FoldAttribute))]
    public class FoldDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            FoldAttribute attribute = (FoldAttribute) fieldInfo.GetCustomAttribute(typeof(FoldAttribute));
            bool useDefaultDisplay = attribute != null && attribute.useDefaultDisplay;

            if (useDefaultDisplay)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
            else
            {
                if (property.propertyType == SerializedPropertyType.Generic && !property.isArray)
                {
                    SerializedProperty iterator = property.Copy();
                    bool enterChildren = true;
                    while (iterator.NextVisible(enterChildren))
                    {
                        if (SerializedProperty.EqualContents(iterator, property.GetEndProperty()))
                        {
                            break;
                        }

                        enterChildren = false;

                        EditorGUI.PropertyField(position, iterator, new GUIContent(iterator.displayName), true);
                        position.y +=
                            EditorGUI.GetPropertyHeight(iterator, new GUIContent(iterator.displayName), true) +
                            EditorGUIUtility.standardVerticalSpacing;
                    }
                }
                else
                {
                    EditorGUI.PropertyField(position, property, GUIContent.none, false);
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            FoldAttribute attribute = (FoldAttribute) fieldInfo.GetCustomAttribute(typeof(FoldAttribute));
            bool useDefaultDisplay = attribute != null && attribute.useDefaultDisplay;

            if (useDefaultDisplay)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
            else
            {
                if (property.propertyType == SerializedPropertyType.Generic && !property.isArray)
                {
                    float totalHeight = 0;
                    SerializedProperty iterator = property.Copy();
                    bool enterChildren = true;
                    while (iterator.NextVisible(enterChildren))
                    {
                        if (SerializedProperty.EqualContents(iterator, property.GetEndProperty()))
                        {
                            break;
                        }

                        enterChildren = false;
                        totalHeight +=
                            EditorGUI.GetPropertyHeight(iterator, new GUIContent(iterator.displayName), true) +
                            EditorGUIUtility.standardVerticalSpacing;
                    }

                    return totalHeight;
                }
                else
                {
                    return EditorGUI.GetPropertyHeight(property, GUIContent.none, false);
                }
            }
        }
    }
    
    [CustomPropertyDrawer(typeof(AnimCurveName))]
    public class AnimCurveDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            int index = CurveLib.AnimCurveNames.IndexOf(property.stringValue);
            index = EditorGUI.Popup(position, label.text, index, CurveLib.AnimCurveNames.ToArray());
            if (index >= 0) property.stringValue = CurveLib.AnimCurveNames[index];
        }
    }
}