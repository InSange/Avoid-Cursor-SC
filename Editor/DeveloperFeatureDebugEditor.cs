using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(DeveloperFeatureDebug))]
public class DeveloperFeatureDebugEditor : Editor
{
    private string _searchText = "";
    private Vector2 _scrollPos;
    private bool _showAll = true;

    public override void OnInspectorGUI()
    {
        DeveloperFeatureDebug script = (DeveloperFeatureDebug)target;

        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🕵️ Developer Unlock Tool", headerStyle);
        EditorGUILayout.HelpBox("체크된 항목은 게임 시작 시(또는 플레이 중) 강제로 해금됩니다.", MessageType.Info);

        // --- 기능 버튼 ---
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All"))
        {
            script.FeaturesToUnlock = Enum.GetValues(typeof(UnlockID))
                .Cast<UnlockID>()
                .Where(x => x != UnlockID.None)
                .ToList();
            EditorUtility.SetDirty(script);
        }
        if (GUILayout.Button("Deselect All"))
        {
            script.FeaturesToUnlock.Clear();
            EditorUtility.SetDirty(script);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // --- 💥 검색창 스타일 수정 ---
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        // 1. 검색 텍스트 필드 (ToolbarSearchField 스타일 사용)
        _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);

        // 2. 취소 버튼 (스타일 이름을 직접 찾아서 사용)
        // "ToolbarSeachCancelButton"은 대부분의 유니티 버전에 존재함
        GUIStyle cancelStyle = GUI.skin.FindStyle("ToolbarSeachCancelButton");
        if (cancelStyle == null) cancelStyle = GUI.skin.FindStyle("ToolbarSearchCancelButton"); // 오타 대응
        if (cancelStyle == null) cancelStyle = EditorStyles.miniButton; // 정 없으면 미니버튼으로 대체

        if (GUILayout.Button("", cancelStyle))
        {
            _searchText = "";
            GUI.FocusControl(null); // 포커스 해제 (키보드 닫기)
        }

        EditorGUILayout.EndHorizontal();

        // --- Enum 목록 그리기 ---
        _showAll = EditorGUILayout.Foldout(_showAll, "Unlock List Checkboxes");

        if (_showAll)
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(400));

            var allUnlocks = Enum.GetValues(typeof(UnlockID)).Cast<UnlockID>();

            foreach (var id in allUnlocks)
            {
                if (id == UnlockID.None) continue;

                if (!string.IsNullOrEmpty(_searchText) &&
                    !id.ToString().ToLower().Contains(_searchText.ToLower()))
                {
                    continue;
                }

                bool isIncluded = script.FeaturesToUnlock.Contains(id);

                string label = id.ToString();
                if (Application.isPlaying && AvoidCursorGameManager.Instance != null)
                {
                    if (AvoidCursorGameManager.Instance.IsUnlocked(id))
                        label += " [✅Unlocked]";
                }

                bool toggleState = EditorGUILayout.ToggleLeft(label, isIncluded);

                if (toggleState != isIncluded)
                {
                    Undo.RecordObject(script, "Toggle Unlock");

                    if (toggleState)
                    {
                        script.FeaturesToUnlock.Add(id);
                        if (Application.isPlaying && AvoidCursorGameManager.Instance != null)
                            AvoidCursorGameManager.Instance.ForceUnlock(id);
                    }
                    else
                    {
                        script.FeaturesToUnlock.Remove(id);
                    }

                    EditorUtility.SetDirty(script);
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}