using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// turnrpg DataTool 공용 GUI 조각.
namespace TurnRpg.DataTool.Editor
{
    internal static class TrGui
    {
        // ── 로케일 3입력(ko/en/ja) — ARPG LocFieldGui 동형. JObject 직접 IO(LocalizedString 형상 보존) ──
        public static void DrawLoc(string field, TrLoc l)
        {
            EditorGUILayout.LabelField(field, EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(12);
                EditorGUILayout.LabelField("ko", GUILayout.Width(22)); l.Ko = EditorGUILayout.TextField(l.Ko);
                EditorGUILayout.LabelField("en", GUILayout.Width(22)); l.En = EditorGUILayout.TextField(l.En);
                EditorGUILayout.LabelField("ja", GUILayout.Width(22)); l.Ja = EditorGUILayout.TextField(l.Ja);
            }
        }

        // bare string(=ko) OR {ko,en,ja} 맵 둘 다 tolerant 로드(LocalizedStringConverter 동형).
        public static TrLoc LoadLoc(JToken tok)
        {
            var l = new TrLoc();
            if (tok == null || tok.Type == JTokenType.Null) return l;
            if (tok.Type == JTokenType.Object)
            {
                l.Ko = tok["ko"]?.Value<string>() ?? "";
                l.En = tok["en"]?.Value<string>() ?? "";
                l.Ja = tok["ja"]?.Value<string>() ?? "";
            }
            else l.Ko = tok.Value<string>() ?? "";
            return l;
        }

        // en/ja 미입력=bare string(ko)·입력={ko,en,ja(비면 생략)}·전부 비면 null. 누락 로케일=런타임 ko 폴백.
        public static JToken ExportLoc(TrLoc l)
        {
            string ko = (l.Ko ?? "").Trim(), en = (l.En ?? "").Trim(), ja = (l.Ja ?? "").Trim();
            bool hasMap = en.Length > 0 || ja.Length > 0;
            if (!hasMap) return string.IsNullOrEmpty(ko) ? null : new JValue(ko);
            var o = new JObject { ["ko"] = ko };
            if (en.Length > 0) o["en"] = en;
            if (ja.Length > 0) o["ja"] = ja;
            return o;
        }

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
