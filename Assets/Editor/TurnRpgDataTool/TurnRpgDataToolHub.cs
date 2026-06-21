using System.Collections.Generic;
using DataTool.Editor;          // UPM 프레임워크: DataToolHubBase·IDataToolPage
using UnityEditor;
using UnityEngine;

// turnrpg Data Tool 허브 — 프레임워크 DataToolHubBase 셸 + turnrpg 페이지 목록(per-game BuildNodes).
//   Phase 2(2026-06-20, datatool-framework 추출 후). 셸(레일·본문·레지스트리)=UPM com.pp.datatool-framework. 이 클래스=turnrpg 노드+[MenuItem]+Export 경로.
//   Assembly-CSharp-Editor 배치(asmdef 불요·프레임워크 autoReferenced 수령). export → TurnBase/Export/Server/ → 사용자 TurnBasedServer/Config 배치.
namespace TurnRpg.DataTool.Editor
{
    internal sealed class TurnRpgDataToolHub : DataToolHubBase
    {
        [MenuItem("Tools/TurnRpg Data Tool")]
        private static void Open()
        {
            var w = GetWindow<TurnRpgDataToolHub>("TurnRpg Data Tool");
            w.minSize = new Vector2(880, 540);
        }

        protected override string ExportFolderPath => TrIO.ExportDir;   // "Export/Server"(ProjectRoot 상대)

        protected override IEnumerable<Node> BuildNodes() => new[]
        {
            // ── 전투 콘텐츠 (turnrpg-server 소비 카탈로그 4종) ──
            Embed("전투 콘텐츠", new TrSkillsPage()),
            Embed("전투 콘텐츠", new TrCharactersPage()),
            Embed("전투 콘텐츠", new TrStagesPage()),
            Embed("전투 콘텐츠", new TrRewardsPage()),
        };
    }
}
