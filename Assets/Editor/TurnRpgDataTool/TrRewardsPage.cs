using System.Collections.Generic;
using System.Linq;
using DataTool.Editor;
using UnityEditor;
using UnityEngine;

// rewards.json 저작 — stageId → {Rewards[{ItemId(string), Amount}]}. 클리어 보상(클리어 조건=서버 권위).
namespace TurnRpg.DataTool.Editor
{
    internal sealed class TrRewardsPage : IDataToolPage, IDataToolToolbar
    {
        private List<TrReward> _rows = new List<TrReward>();
        private string  _search = "";
        private Vector2 _scroll;
        private List<ValidationIssue> _issues;
        private bool _loaded;

        public string Title => "Rewards";

        public void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("rewards.json", EditorStyles.boldLabel, GUILayout.Width(120));
                if (GUILayout.Button("로드", GUILayout.Width(50)))   Load();
                if (GUILayout.Button("검증", GUILayout.Width(50)))   RunValidation();
                if (GUILayout.Button("Export", GUILayout.Width(70))) Export();
            }
        }

        public void OnGUI()
        {
            if (!_loaded) { Load(); _loaded = true; }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.HelpBox(
                "스테이지 클리어 보상(stageId 키). ItemId=turnrpg 전용 NS(string)·Amount. 클리어 조건(아군생존+적전멸)=서버 권위.\n"
              + "Export → Export/Server/rewards.json.", MessageType.None);
            _search = DataToolGUI.DrawSearchBox(_search);
            EditorGUILayout.Space();

            int removeR = -1;
            for (int i = 0; i < _rows.Count; i++)
            {
                var r = _rows[i];
                if (!DataToolGUI.SearchMatch(_search, r.StageId.ToString())) continue;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("stageId", GUILayout.Width(54)); r.StageId = EditorGUILayout.IntField(r.StageId, GUILayout.Width(50));
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("−", GUILayout.Width(24))) removeR = i;
                    }
                    EditorGUI.indentLevel++;
                    int removeItem = -1;
                    for (int j = 0; j < r.Rewards.Count; j++)
                    {
                        var it = r.Rewards[j];
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("itemId", GUILayout.Width(44));  it.ItemId = EditorGUILayout.TextField(it.ItemId, GUILayout.Width(120));
                            EditorGUILayout.LabelField("amount", GUILayout.Width(50));  it.Amount = EditorGUILayout.IntField(it.Amount, GUILayout.Width(60));
                            if (GUILayout.Button("−", GUILayout.Width(22))) removeItem = j;
                        }
                    }
                    if (removeItem >= 0) r.Rewards.RemoveAt(removeItem);
                    if (GUILayout.Button("+ 보상 아이템", GUILayout.Width(110))) r.Rewards.Add(new TrRewardItem());
                    EditorGUI.indentLevel--;
                }
            }
            if (removeR >= 0) _rows.RemoveAt(removeR);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ 스테이지 보상 추가", GUILayout.Width(150)))
                    _rows.Add(new TrReward { StageId = NextId() });
                EditorGUILayout.LabelField($"총 {_rows.Count}", EditorStyles.miniLabel);
            }
            if (_issues != null) { EditorGUILayout.Space(); EditorGUILayout.LabelField("검증 결과", EditorStyles.boldLabel); DataToolGUI.DrawIssues(_issues); }
            EditorGUILayout.EndScrollView();
        }

        private int NextId() { int m = 0; foreach (var r in _rows) if (r.StageId > m) m = r.StageId; return m + 1; }

        private void Load() { _rows = TrIO.Load<TrReward>("rewards.json"); _rows.Sort((a, b) => a.StageId.CompareTo(b.StageId)); _issues = null; }

        private void RunValidation()
        {
            var issues = new List<ValidationIssue>();
            var seen = new HashSet<int>();
            foreach (var r in _rows)
            {
                string head = $"reward stage {r.StageId}";
                if (r.StageId <= 0) issues.Add(ValidationIssue.Error($"{head}: stageId ≤ 0."));
                if (!seen.Add(r.StageId)) issues.Add(ValidationIssue.Error($"{head}: stageId 중복."));
                if (r.Rewards.Count == 0) issues.Add(ValidationIssue.Warning($"{head}: 보상 없음."));
                foreach (var it in r.Rewards)
                {
                    if (string.IsNullOrWhiteSpace(it.ItemId)) issues.Add(ValidationIssue.Error($"{head}: itemId 비어있음."));
                    if (it.Amount <= 0) issues.Add(ValidationIssue.Warning($"{head} {it.ItemId}: amount ≤ 0."));
                }
            }
            _issues = issues;
        }

        private void Export()
        {
            RunValidation();
            if (_issues.Any(i => i.Severity == IssueSeverity.Error)) { EditorUtility.DisplayDialog("Export 차단", "검증 에러를 먼저 수정하세요.", "확인"); return; }
            TrIO.Export("rewards.json", _rows.OrderBy(x => x.StageId), x => x.StageId);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Export 완료", $"{TrIO.ExportDir}/rewards.json 저장됨.\n사용자가 TurnBasedServer/Config/ 로 배치하세요.", "확인");
        }
    }
}
