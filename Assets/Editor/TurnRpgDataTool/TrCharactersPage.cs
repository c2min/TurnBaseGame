using System.Collections.Generic;
using System.Linq;
using DataTool.Editor;
using UnityEditor;
using UnityEngine;

// character_templates.json 저작 — templateId → {MaxHp, Speed, AttackPower, Defense, SkillIds[]}. 아군 NS=101+.
namespace TurnRpg.DataTool.Editor
{
    internal sealed class TrCharactersPage : IDataToolPage, IDataToolToolbar
    {
        private List<TrCharacter> _rows = new List<TrCharacter>();
        private string  _search = "";
        private Vector2 _scroll;
        private List<ValidationIssue> _issues;
        private bool _loaded;

        public string Title => "Character Templates";

        public void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("character_templates.json", EditorStyles.boldLabel, GUILayout.Width(190));
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
                "캐릭터 템플릿(아군·templateId 키, NS 101+). 불변 정의(인스턴스/레벨/로스터=server DB). 스탯=명시값.\n"
              + "SkillIds=보유 스킬(skills.json). Export → Export/Server/character_templates.json.", MessageType.None);
            _search = DataToolGUI.DrawSearchBox(_search);
            EditorGUILayout.Space();

            int remove = -1;
            for (int i = 0; i < _rows.Count; i++)
            {
                var c = _rows[i];
                if (!DataToolGUI.SearchMatch(_search, c.TemplateId.ToString())) continue;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("templateId", GUILayout.Width(72));
                        c.TemplateId = EditorGUILayout.IntField(c.TemplateId, GUILayout.Width(60));
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("−", GUILayout.Width(24))) remove = i;
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("maxHp", GUILayout.Width(44));  c.MaxHp       = EditorGUILayout.IntField(c.MaxHp, GUILayout.Width(54));
                        EditorGUILayout.LabelField("speed", GUILayout.Width(44));  c.Speed       = EditorGUILayout.IntField(c.Speed, GUILayout.Width(44));
                        EditorGUILayout.LabelField("atk", GUILayout.Width(28));    c.AttackPower = EditorGUILayout.IntField(c.AttackPower, GUILayout.Width(44));
                        EditorGUILayout.LabelField("def", GUILayout.Width(28));    c.Defense     = EditorGUILayout.IntField(c.Defense, GUILayout.Width(44));
                    }
                    TrGui.DrawIntList("skillIds", c.SkillIds);
                }
            }
            if (remove >= 0) _rows.RemoveAt(remove);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ 캐릭터 추가", GUILayout.Width(120)))
                    _rows.Add(new TrCharacter { TemplateId = NextId() });
                EditorGUILayout.LabelField($"총 {_rows.Count}", EditorStyles.miniLabel);
            }
            if (_issues != null) { EditorGUILayout.Space(); EditorGUILayout.LabelField("검증 결과", EditorStyles.boldLabel); DataToolGUI.DrawIssues(_issues); }
            EditorGUILayout.EndScrollView();
        }

        private int NextId() { int m = 100; foreach (var c in _rows) if (c.TemplateId > m) m = c.TemplateId; return m + 1; }

        private void Load() { _rows = TrIO.Load<TrCharacter>("character_templates.json"); _rows.Sort((a, b) => a.TemplateId.CompareTo(b.TemplateId)); _issues = null; }

        private void RunValidation()
        {
            var issues = new List<ValidationIssue>();
            var seen = new HashSet<int>();
            var skillIds = new HashSet<int>(TrIO.Load<TrSkill>("skills.json").Select(s => s.SkillId));   // 크로스 조인(degrade)
            foreach (var c in _rows)
            {
                string tag = $"char {c.TemplateId}";
                if (c.TemplateId <= 0) issues.Add(ValidationIssue.Error($"{tag}: templateId ≤ 0."));
                if (!seen.Add(c.TemplateId)) issues.Add(ValidationIssue.Error($"{tag}: templateId 중복."));
                if (c.MaxHp <= 0) issues.Add(ValidationIssue.Warning($"{tag}: maxHp ≤ 0."));
                if (c.Speed <= 0) issues.Add(ValidationIssue.Warning($"{tag}: speed ≤ 0."));
                foreach (var sid in c.SkillIds)
                    if (skillIds.Count > 0 && !skillIds.Contains(sid))
                        issues.Add(ValidationIssue.Warning($"{tag}: skillId {sid} skills.json에 없음(드리프트)."));
            }
            _issues = issues;
        }

        private void Export()
        {
            RunValidation();
            if (_issues.Any(i => i.Severity == IssueSeverity.Error)) { EditorUtility.DisplayDialog("Export 차단", "검증 에러를 먼저 수정하세요.", "확인"); return; }
            TrIO.Export("character_templates.json", _rows.OrderBy(x => x.TemplateId), x => x.TemplateId);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Export 완료", $"{TrIO.ExportDir}/character_templates.json 저장됨.\n사용자가 TurnBasedServer/Config/ 로 배치하세요.", "확인");
        }
    }
}
