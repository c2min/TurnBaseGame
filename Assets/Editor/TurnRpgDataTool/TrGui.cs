using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// turnrpg DataTool 공용 GUI 조각.
namespace TurnRpg.DataTool.Editor
{
    internal static class TrGui
    {
        // int 리스트 인라인 편집(skillIds 등). 한 줄: 라벨 + 값들 + 항목별 − + 끝 +.
        public static void DrawIntList(string label, List<int> list)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(60));
                int remove = -1;
                for (int i = 0; i < list.Count; i++)
                {
                    list[i] = EditorGUILayout.IntField(list[i], GUILayout.Width(40));
                    if (GUILayout.Button("−", GUILayout.Width(20))) remove = i;
                }
                if (remove >= 0) { list.RemoveAt(remove); GUIUtility.ExitGUI(); }
                if (GUILayout.Button("+", GUILayout.Width(22))) list.Add(0);
            }
        }
    }
}
