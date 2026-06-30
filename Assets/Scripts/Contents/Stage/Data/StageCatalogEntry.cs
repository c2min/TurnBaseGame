using System.Collections.Generic;
using SMDevLibrary.Localization;

// 스테이지 선택 표시 메타 — plan stage_catalog.json(dual-export) 소비 모델.
// 전투 init=서버 BattleSnapshot 권위 / 보상 지급 권위=서버 rewards. 여기는 표시 전용.
// JSON 직접 로드(Newtonsoft·case-insensitive 매칭) / name=LocalizedString(tolerant 컨버터).
public class StageCatalogEntry
{
    public LocalizedString Name;
    public string ThumbnailPath;
    public int UnlockStageId;
    public int SortOrder;
    public List<RewardPreview> RewardPreview;
}

public class RewardPreview
{
    public string ItemId;
    public int Amount;
}
