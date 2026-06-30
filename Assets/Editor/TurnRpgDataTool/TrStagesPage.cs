using System.Collections.Generic;
using System.Linq;
using DataTool.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// stages.json 저작 — stageId → {GridWidth, GridHeight, Enemies[{UnitId, TemplateId, 스탯, TileIndex, SkillIds[]}]}.
//   TileIndex = y*GridWidth + x. 적 TemplateId NS=201+(아군 101 분리).
// + 클라 표시 메타(stage-list-data 2026-06-30): name(Loc)·thumbnail·unlock·sortOrder → 클라 stage_catalog.json dual-export.
//   rewardPreview = rewards.json 파생(이중저작0). 서버 stages.json(전투 init)=무변경.
namespace TurnRpg.DataTool.Editor
{
    internal sealed class TrStagesPage : IDataToolPage, IDataToolToolbar
    {
        private List<TrStage> _rows = new List<TrStage>();
        private string  _search = "";
        private Vector2 _scroll;
        private List<ValidationIssue> _issues;
        private bool _loaded;

        public string Title => "Stages";

        public void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("stages.json", EditorStyles.boldLabel, GUILayout.Width(120));
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
                "스테이지(stageId 키)·그리드+적 배치. TileIndex = y*GridWidth + x. 적 TemplateId=비주얼 키(201+).\n"
              + "Export → Export/Server/stages.json.", MessageType.None);
            _search = DataToolGUI.DrawSearchBox(_search);
            EditorGUILayout.Space();

            int removeS = -1;
            for (int i = 0; i < _rows.Count; i++)
            {
                var st = _rows[i];
                if (!DataToolGUI.SearchMatch(_search, st.StageId.ToString())) continue;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("stageId", GUILayout.Width(54)); st.StageId = EditorGUILayout.IntField(st.StageId, GUILayout.Width(50));
                        EditorGUILayout.LabelField("grid W×H", GUILayout.Width(58));
                        st.GridWidth  = EditorGUILayout.IntField(st.GridWidth, GUILayout.Width(40));
                        st.GridHeight = EditorGUILayout.IntField(st.GridHeight, GUILayout.Width(40));
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("스테이지 삭제", GUILayout.Width(95))) removeS = i;
                    }
                    DrawDisplayMeta(st);
                    DrawEnemies(st);
                }
            }
            if (removeS >= 0) _rows.RemoveAt(removeS);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ 스테이지 추가", GUILayout.Width(120)))
                    _rows.Add(new TrStage { StageId = NextId() });
                EditorGUILayout.LabelField($"총 {_rows.Count}", EditorStyles.miniLabel);
            }
            if (_issues != null) { EditorGUILayout.Space(); EditorGUILayout.LabelField("검증 결과", EditorStyles.boldLabel); DataToolGUI.DrawIssues(_issues); }
            EditorGUILayout.EndScrollView();
        }

        // 클라 표시 메타(stage_catalog dual-export·서버 stages.json 미포함). rewardPreview=rewards.json 파생(export 시).
        private void DrawDisplayMeta(TrStage st)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("표시 메타 (클라 stage_catalog)", EditorStyles.miniBoldLabel);
            TrGui.DrawLoc("name", st.Name);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("thumbnailPath", GUILayout.Width(90)); st.ThumbnailPath = EditorGUILayout.TextField(st.ThumbnailPath);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("unlockStageId", GUILayout.Width(90)); st.UnlockStageId = EditorGUILayout.IntField(st.UnlockStageId, GUILayout.Width(50));
                EditorGUILayout.LabelField("(0=처음)", EditorStyles.miniLabel, GUILayout.Width(54));
                EditorGUILayout.LabelField("sortOrder", GUILayout.Width(64));     st.SortOrder = EditorGUILayout.IntField(st.SortOrder, GUILayout.Width(50));
            }
            EditorGUILayout.LabelField("rewardPreview = rewards.json 파생(export 시 자동)", EditorStyles.miniLabel);
            EditorGUI.indentLevel--;
        }

        private void DrawEnemies(TrStage st)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("적 (Enemies)", EditorStyles.miniBoldLabel);
            int removeE = -1;
            for (int j = 0; j < st.Enemies.Count; j++)
            {
                var e = st.Enemies[j];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("unitId", GUILayout.Width(44));     e.UnitId     = EditorGUILayout.TextField(e.UnitId, GUILayout.Width(56));
                        EditorGUILayout.LabelField("templateId", GUILayout.Width(70));  e.TemplateId = EditorGUILayout.IntField(e.TemplateId, GUILayout.Width(56));
                        EditorGUILayout.LabelField("tile", GUILayout.Width(28));        e.TileIndex  = EditorGUILayout.IntField(e.TileIndex, GUILayout.Width(50));
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("−", GUILayout.Width(22))) removeE = j;
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("hp", GUILayout.Width(20));  e.MaxHp       = EditorGUILayout.IntField(e.MaxHp, GUILayout.Width(50));
                        EditorGUILayout.LabelField("spd", GUILayout.Width(26)); e.Speed       = EditorGUILayout.IntField(e.Speed, GUILayout.Width(40));
                        EditorGUILayout.LabelField("atk", GUILayout.Width(26)); e.AttackPower = EditorGUILayout.IntField(e.AttackPower, GUILayout.Width(40));
                        EditorGUILayout.LabelField("def", GUILayout.Width(26)); e.Defense     = EditorGUILayout.IntField(e.Defense, GUILayout.Width(40));
                    }
                    TrGui.DrawIntList("skillIds", e.SkillIds);
                }
            }
            if (removeE >= 0) st.Enemies.RemoveAt(removeE);
            if (GUILayout.Button("+ 적 추가", GUILayout.Width(90))) st.Enemies.Add(new TrEnemy { UnitId = $"e{st.Enemies.Count + 1}" });
            EditorGUI.indentLevel--;
        }

        private int NextId() { int m = 0; foreach (var s in _rows) if (s.StageId > m) m = s.StageId; return m + 1; }

        private void Load()
        {
            _rows = TrIO.Load<TrStage>("stages.json");
            _rows.Sort((a, b) => a.StageId.CompareTo(b.StageId));
            // 표시 메타 오버레이(클라 stage_catalog.json — [JsonIgnore]라 서버 stages.json엔 없음·dungeon OverlayServerSpawns 동형)
            var cat = TrIO.LoadClientJson("stage_catalog.json");
            if (cat != null)
                foreach (var st in _rows)
                    if (cat[st.StageId.ToString()] is JObject o)
                    {
                        st.Name          = TrGui.LoadLoc(o["name"]);
                        st.ThumbnailPath  = o["thumbnailPath"]?.Value<string>() ?? "";
                        st.UnlockStageId  = o["unlockStageId"]?.Value<int>() ?? 0;
                        st.SortOrder      = o["sortOrder"]?.Value<int>() ?? 0;
                    }
            _issues = null;
        }

        private void RunValidation()
        {
            var issues = new List<ValidationIssue>();
            var seen = new HashSet<int>();
            var allIds = new HashSet<int>(_rows.Select(s => s.StageId));
            foreach (var st in _rows)
            {
                string head = $"stage {st.StageId}";
                if (st.StageId <= 0) issues.Add(ValidationIssue.Error($"{head}: stageId ≤ 0."));
                if (!seen.Add(st.StageId)) issues.Add(ValidationIssue.Error($"{head}: stageId 중복."));
                if (st.GridWidth <= 0 || st.GridHeight <= 0) issues.Add(ValidationIssue.Error($"{head}: grid 크기 ≤ 0."));
                if (st.Enemies.Count == 0) issues.Add(ValidationIssue.Warning($"{head}: 적 없음."));
                // 표시 메타(클라 stage_catalog)
                if (string.IsNullOrWhiteSpace(st.Name.Ko)) issues.Add(ValidationIssue.Warning($"{head}: name ko 비어있음(목록 표시 fallback)."));
                if (st.UnlockStageId != 0 && !allIds.Contains(st.UnlockStageId)) issues.Add(ValidationIssue.Warning($"{head}: unlockStageId {st.UnlockStageId} 미존재 스테이지(0=처음부터)."));
                int cells = st.GridWidth * st.GridHeight;
                var tiles = new HashSet<int>();
                foreach (var e in st.Enemies)
                {
                    string tag = $"{head} {e.UnitId}";
                    if (e.TileIndex < 0 || e.TileIndex >= cells) issues.Add(ValidationIssue.Error($"{tag}: tileIndex {e.TileIndex} 그리드 밖(0~{cells - 1})."));
                    else if (!tiles.Add(e.TileIndex)) issues.Add(ValidationIssue.Warning($"{tag}: tileIndex {e.TileIndex} 중복(겹친 적)."));
                    if (e.MaxHp <= 0) issues.Add(ValidationIssue.Warning($"{tag}: maxHp ≤ 0."));
                    if (e.TemplateId <= 0) issues.Add(ValidationIssue.Warning($"{tag}: templateId ≤ 0(적 비주얼 미해소)."));
                }
            }
            _issues = issues;
        }

        private void Export()
        {
            RunValidation();
            if (_issues.Any(i => i.Severity == IssueSeverity.Error)) { EditorUtility.DisplayDialog("Export 차단", "검증 에러를 먼저 수정하세요.", "확인"); return; }

            // ① 서버 stages.json — 전투 init(표시 메타=[JsonIgnore] 자동 제외·무변경)
            TrIO.Export("stages.json", _rows.OrderBy(x => x.StageId), x => x.StageId);

            // ② 클라 stage_catalog.json — 표시 메타 + rewardPreview(rewards.json 파생·이중저작0)
            var rewardByStage = TrIO.Load<TrReward>("rewards.json").ToDictionary(r => r.StageId, r => r.Rewards);
            var root = new JObject();
            foreach (var st in _rows.OrderBy(x => x.SortOrder).ThenBy(x => x.StageId))
            {
                var o = new JObject
                {
                    ["thumbnailPath"] = st.ThumbnailPath ?? "",
                    ["unlockStageId"] = st.UnlockStageId,
                    ["sortOrder"]     = st.SortOrder,
                };
                var name = TrGui.ExportLoc(st.Name);
                if (name != null) o["name"] = name;
                var rp = new JArray();
                if (rewardByStage.TryGetValue(st.StageId, out var rlist))
                    foreach (var ri in rlist) rp.Add(new JObject { ["itemId"] = ri.ItemId, ["amount"] = ri.Amount });
                o["rewardPreview"] = rp;
                root[st.StageId.ToString()] = o;
            }
            TrIO.ExportClientJson("stage_catalog.json", root);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Export 완료",
                $"서버: {TrIO.ExportDir}/stages.json(전투) → TurnBasedServer/Config/\n"
              + $"클라: {TrIO.ClientExportDir}/stage_catalog.json(표시+보상미리보기) → Assets/Resources/Data/\n사용자가 각 위치에 배치하세요.", "확인");
        }
    }
}
