// Designed by Kinemation, 2023

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kinemation.FPSFramework.Editor.Attributes
{
    public class BoneSelectionPopup : EditorWindow
    {
        public delegate void BoneSelectedCallback(int index);
    
        private List<string> boneNames;
        private BoneSelectedCallback callback;
        private Vector2 scrollPosition;
        private string searchString = string.Empty;
        private List<int> filteredIndexes;

        public static void ShowWindow(string[] boneNames, BoneSelectedCallback callback)
        {
            BoneSelectionPopup window = CreateInstance<BoneSelectionPopup>();
            window.boneNames = new List<string>(boneNames);
            window.callback = callback;
            window.filteredIndexes = new List<int>(window.boneNames.Count);
            window.UpdateFilter();
            window.titleContent = new GUIContent("Bone Selection");
            window.ShowAuxWindow();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"));
            searchString = EditorGUILayout.TextField(searchString, EditorStyles.toolbarSearchField);
            EditorGUILayout.EndHorizontal();

            UpdateFilter();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < filteredIndexes.Count; i++)
            {
                if (GUILayout.Button(boneNames[filteredIndexes[i]], EditorStyles.label))
                {
                    callback.Invoke(filteredIndexes[i]);
                    Close();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void UpdateFilter()
        {
            if (filteredIndexes.Count == 0 || !string.IsNullOrEmpty(searchString))
            {
                filteredIndexes.Clear();

                for (int i = 0; i < boneNames.Count; i++)
                {
                    if (string.IsNullOrEmpty(searchString) || boneNames[i].ToLower().Contains(searchString.ToLower()))
                    {
                        filteredIndexes.Add(i);
                    }
                }
            }
        }
    }
}