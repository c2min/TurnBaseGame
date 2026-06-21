using System.Collections.Generic;
using System.Linq;
using DataTool.Editor;          // 프레임워크: IDataToolPage·IDataToolToolbar·DataToolGUI·ValidationIssue
using UnityEditor;
using UnityEngine;

// skills.json 저작 — skillId → {Targeting, Effect, Range, Power, Chebyshev, AoeRadius, (Status)StatusType/Duration/Magnitude}.
namespace TurnRpg.DataTool.Editor
{
    internal sealed class TrSkillsPage : IDataToolPage, IDataToolToolbar
    {
        private static readonly string[] TargetingOpts = { "SelfOnly", "SingleEnemy", "SingleAlly", "AllEnemies", "AllAllies", "AreaEnemy" };
        private static readonly string[] EffectOpts    = { "Damage", "Heal", "Status" };

        private List<TrSkill> _rows = new List<TrSkill>();
        private string  _search = "";
        private Vector2 _scroll;
        private List<ValidationIssue> _issues;
        private bool _loaded;

        public string Title => "Skills";

        public void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("skills.json", EditorStyles.boldLabel, GUILayout.Width(120));
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
                "스킬 정의(skillId 키). Targeting·Effect 드롭다운. Range/Power=수치(Power=Atk×Power%−Def 기준). "
              + "AreaEnemy=AoeRadius>0·Chebyshev=대각거리. Effect=Status면 StatusType/지속/크기 노출.\n"
              + "Export → Export/Server/skills.json → 사용자가 TurnBasedServer/Config 배치.", MessageType.None);
            _search = DataToolGUI.DrawSearchBox(_search);
            EditorGUILayout.Space();

            int remove = -1;
            for (int i = 0; i < _rows.Count; i++)
            {
                var s = _rows[i];
                if (!DataToolGUI.SearchMatch(_search, s.SkillId.ToString(), s.Effect, s.Targeting)) continue;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("skillId", GUILayout.Width(54));
                        s.SkillId = EditorGUILayout.IntField(s.SkillId, GUILayout.Width(60));
                        EditorGUILayout.LabelField("targeting", GUILayout.Width(60));
                        s.Targeting = TargetingOpts[Mathf.Max(0, EditorGUILayout.Popup(System.Array.IndexOf(TargetingOpts, s.Targeting), TargetingOpts, GUILayout.Width(100)))];
                        EditorGUILayout.LabelField("effect", GUILayout.Width(44));
                        int ei = EditorGUILayout.Popup(Mathf.Max(0, System.Array.IndexOf(EffectOpts, s.Effect)), EffectOpts, GUILayout.Width(80));
                        s.Effect = EffectOpts[ei];
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("−", GUILayout.Width(24))) remove = i;
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("range", GUILayout.Width(44));  s.Range     = EditorGUILayout.IntField(s.Range, GUILayout.Width(50));
                        EditorGUILayout.LabelField("power", GUILayout.Width(44));  s.Power     = EditorGUILayout.IntField(s.Power, GUILayout.Width(50));
                        EditorGUILayout.LabelField("chebyshev", GUILayout.Width(64)); s.Chebyshev = EditorGUILayout.Toggle(s.Chebyshev, GUILayout.Width(20));
                        EditorGUILayout.LabelField("aoeRadius", GUILayout.Width(64)); s.AoeRadius = EditorGUILayout.IntField(s.AoeRadius, GUILayout.Width(40));
                    }
                    if (s.Effect == "Status")
                    {
                        s.StatusType ??= "Poison";
                        s.StatusDuration ??= 2;
                        s.StatusMagnitude ??= 10;
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("statusType", GUILayout.Width(70));
                            s.StatusType = EditorGUILayout.TextField(s.StatusType, GUILayout.Width(90));
                            EditorGUILayout.LabelField("dur", GUILayout.Width(28));  s.StatusDuration  = EditorGUILayout.IntField(s.StatusDuration ?? 0, GUILayout.Width(40));
                            EditorGUILayout.LabelField("mag", GUILayout.Width(28));  s.StatusMagnitude = EditorGUILayout.IntField(s.StatusMagnitude ?? 0, GUILayout.Width(40));
                        }
                    }
                    else { s.StatusType = null; s.StatusDuration = null; s.StatusMagnitude = null; }  // 비-Status=omit(Phase1 정합)
                }
            }
            if (remove >= 0) _rows.RemoveAt(remove);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ 스킬 추가", GUILayout.Width(110)))
                    _rows.Add(new TrSkill { SkillId = NextId() });
                EditorGUILayout.LabelField($"총 {_rows.Count}", EditorStyles.miniLabel);
            }
            if (_issues != null) { EditorGUILayout.Space(); EditorGUILayout.LabelField("검증 결과", EditorStyles.boldLabel); DataToolGUI.DrawIssues(_issues); }
            EditorGUILayout.EndScrollView();
        }

        private int NextId() { int m = 0; foreach (var s in _rows) if (s.SkillId > m) m = s.SkillId; return m + 1; }

        private void Load() { _rows = TrIO.Load<TrSkill>("skills.json"); _rows.Sort((a, b) => a.SkillId.CompareTo(b.SkillId)); _issues = null; }

        private void RunValidation()
        {
            var issues = new List<ValidationIssue>();
            var seen = new HashSet<int>();
            foreach (var s in _rows)
            {
                string tag = $"skill {s.SkillId}";
                if (s.SkillId <= 0) issues.Add(ValidationIssue.Error($"{tag}: skillId ≤ 0."));
                if (!seen.Add(s.SkillId)) issues.Add(ValidationIssue.Error($"{tag}: skillId 중복."));
                if (System.Array.IndexOf(TargetingOpts, s.Targeting) < 0) issues.Add(ValidationIssue.Error($"{tag}: targeting '{s.Targeting}' 무효."));
                if (System.Array.IndexOf(EffectOpts, s.Effect) < 0) issues.Add(ValidationIssue.Error($"{tag}: effect '{s.Effect}' 무효."));
                if (s.Range < 0) issues.Add(ValidationIssue.Warning($"{tag}: range < 0."));
                if (s.Effect != "Status" && s.Power <= 0) issues.Add(ValidationIssue.Warning($"{tag}: Damage/Heal인데 power ≤ 0."));
                if (s.Targeting == "AreaEnemy" && s.AoeRadius <= 0) issues.Add(ValidationIssue.Warning($"{tag}: AreaEnemy인데 aoeRadius ≤ 0."));
                if (s.Effect == "Status" && string.IsNullOrWhiteSpace(s.StatusType)) issues.Add(ValidationIssue.Error($"{tag}: Status인데 statusType 비어있음."));
            }
            _issues = issues;
        }

        private void Export()
        {
            RunValidation();
            if (_issues.Any(i => i.Severity == IssueSeverity.Error)) { EditorUtility.DisplayDialog("Export 차단", "검증 에러를 먼저 수정하세요.", "확인"); return; }
            TrIO.Export("skills.json", _rows.OrderBy(x => x.SkillId), x => x.SkillId);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Export 완료", $"{TrIO.ExportDir}/skills.json 저장됨.\n사용자가 TurnBasedServer/Config/ 로 배치하세요.", "확인");
        }
    }
}
