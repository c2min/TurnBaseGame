using System;
using System.Collections.Generic;
using System.IO;
using DataTool.Editor;          // UPM 프레임워크(com.pp.datatool-framework)
using DataTool.Editor.Server;   // DataToolJson(.Server NS — 구 ServerJson 위치 계승)
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;     // stage_catalog 클라 export(JObject 직접·Loc 형상 보존)
using UnityEngine;

// turnrpg 전투 콘텐츠 저작 미러 DTO — turnrpg-server 소비 스키마(skills/character_templates/stages/rewards) 1:1.
//   서버 정본=turnrpg-server(BattleSkill/CharacterData/StageDefinition/EnemySpawn). 본 도구는 그 JSON 형상 미러(Phase1 손저작 라운드트립).
//   export-only: TurnBase/Export/Server/ → 사용자/turnrpg-server가 TurnBasedServer/Config(env-var) 배치. 키=int id→정의(keyed object).
namespace TurnRpg.DataTool.Editor
{
    // ── 미러 DTO (필드명 PascalCase = 서버 스키마·Phase1 JSON 정합) ──
    internal sealed class TrSkill
    {
        public int    SkillId;
        public string Targeting = "SingleEnemy";   // SelfOnly·SingleEnemy·SingleAlly·AllEnemies·AllAllies·AreaEnemy
        public string Effect    = "Damage";        // Damage·Heal·Status
        public int    Range     = 1;
        public int    Power;
        public bool   Chebyshev;
        public int    AoeRadius;
        // 상태이상 — Status일 때만(비-Status=omit, Phase1 정합)
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string StatusType;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public int?   StatusDuration;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public int?   StatusMagnitude;
    }

    internal sealed class TrCharacter
    {
        public int       TemplateId;
        public int       MaxHp;
        public int       Speed;
        public int       AttackPower;
        public int       Defense;
        public List<int> SkillIds = new List<int>();
    }

    internal sealed class TrEnemy
    {
        public string    UnitId = "e1";
        public int       TemplateId;     // 적 비주얼 키(201+ NS·아군 101~ 분리, 2026-06-20 server-done)
        public int       MaxHp;
        public int       Speed;
        public int       AttackPower;
        public int       Defense;
        public int       TileIndex;      // = y * GridWidth + x
        public List<int> SkillIds = new List<int>();
    }

    internal sealed class TrStage
    {
        public int           StageId;
        public int           GridWidth  = 6;
        public int           GridHeight = 6;
        public List<TrEnemy> Enemies    = new List<TrEnemy>();

        // ── 클라 표시 메타(stage_catalog.json·turnrpg-client stage-list-data 2026-06-30) ──
        //   [JsonIgnore] = 서버 stages.json(전투 init) 미오염. 영속처=클라 stage_catalog.json(Load 시 오버레이 복원).
        [JsonIgnore] public TrLoc  Name          = new TrLoc();   // LocalizedString 형상(turnrpg 최초 i18n 소비)
        [JsonIgnore] public string ThumbnailPath = "";
        [JsonIgnore] public int    UnlockStageId;                 // 선행 클리어 스테이지(0=처음부터 해금)
        [JsonIgnore] public int    SortOrder;                     // 목록 정렬
    }

    // 로케일 3입력 — SMDevLibrary.Localization LocalizedString 형상 미러(bare string ∥ {ko,en,ja} 맵·tolerant).
    //   ARPG DataTool LocField 동형(turnrpg=별 프로젝트라 미러). 클라 JsonConvert+LocalizedStringConverter가 동일 형상 역직렬화.
    internal sealed class TrLoc
    {
        public string Ko = "";
        public string En = "";
        public string Ja = "";
    }

    internal sealed class TrRewardItem
    {
        public string ItemId = "gold";   // turnrpg 전용 NS(ARPG itemId 카탈로그와 분리)
        public int    Amount = 1;
    }

    internal sealed class TrReward
    {
        public int                StageId;
        public List<TrRewardItem> Rewards = new List<TrRewardItem>();
    }

    // ── 공용 IO — Export/Server(ProjectRoot 상대=TurnBase/Export/Server) keyed-object 입출력. 프레임워크 DataToolJson 위임 ──
    internal static class TrIO
    {
        public const string ExportDir = "Export/Server";   // DataToolJson.WriteToExport dir(ProjectRoot 상대)
        private static string Root => Directory.GetParent(Application.dataPath).FullName;

        // {id(string)→정의} keyed object 로드(Phase1 산출·라운드트립). 없으면 빈.
        public static List<T> Load<T>(string fileName)
        {
            string p = Path.Combine(Root, ExportDir, fileName);
            if (!File.Exists(p)) return new List<T>();
            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, T>>(File.ReadAllText(p), DataToolJson.Settings);
                return dict != null ? new List<T>(dict.Values) : new List<T>();
            }
            catch (Exception e) { Debug.LogError($"[TurnRpgDataTool] {fileName} 로드 실패: {e.Message}"); return new List<T>(); }
        }

        // 리스트 → {id→정의} keyed object export. keySelector=각 항목 id.
        public static void Export<T>(string fileName, IEnumerable<T> rows, Func<T, int> keySelector)
        {
            var dict = new Dictionary<string, T>();
            foreach (var r in rows) dict[keySelector(r).ToString()] = r;
            DataToolJson.WriteToExport(ExportDir, fileName, dict);
        }

        // ── 클라 export(→Resources/Data) — JObject 직접(Loc 형상 보존·stage_catalog 등). ExportDir(Server)과 분리 ──
        public const string ClientExportDir = "Export";   // ProjectRoot 상대(TurnBase/Export)
        public static void ExportClientJson(string fileName, JObject root)
        {
            string full = Path.Combine(Root, ClientExportDir);
            Directory.CreateDirectory(full);
            File.WriteAllText(Path.Combine(full, fileName), root.ToString(Formatting.Indented));
        }
        // 클라 export(또는 그 폴백) JObject 로드 — stage_catalog 표시 메타 오버레이용.
        public static JObject LoadClientJson(string fileName)
        {
            string p = Path.Combine(Root, ClientExportDir, fileName);
            if (!File.Exists(p)) return null;
            try { return JObject.Parse(File.ReadAllText(p)); }
            catch (Exception e) { Debug.LogError($"[TurnRpgDataTool] (client) {fileName} 로드 실패: {e.Message}"); return null; }
        }
    }
}
