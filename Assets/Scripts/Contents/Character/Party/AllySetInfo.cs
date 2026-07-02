using System;

/// <summary>
/// 파티 편성 슬롯 — 아군 1명(AllyInfo) + 배치 타일 인덱스.
/// 파티 프리셋·검증·StageEnter 파티 구성(PartyDto 변환)에 사용. (구 StageInfo.cs서 분리·오프라인 무관)
/// </summary>
[Serializable]
public class AllySetInfo
{
    public AllyInfo Ally;
    public int TileIndex;
}
