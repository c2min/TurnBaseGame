using SM.Contracts.TurnRPG;
// UnityEngine.CharacterInfo(레거시 글리프 구조체)와 모호 → 계약 타입으로 확정.
using CharacterInfo = SM.Contracts.TurnRPG.CharacterInfo;
using SMDevLibrary.Generics;
using System.Collections.Generic;
using UnityEngine;

public class Client : MonoSingleton<Client>
{
    public SceneManager SceneMgr;

    [SerializeField]
    private GameDataConfig _gameData;
    public GameDataConfig GameData => _gameData;

    public UserInfo UserInfo { get; private set; }

    /// <summary>현재 진행 중 전투의 서버 식별자(계약 BattleId). StageEnter/Snapshot 응답에서 설정.</summary>
    public long ActiveBattleId { get; set; }

    protected override void Awake()
    {
        SceneMgr = GetComponentInChildren<SceneManager>();
        base.Awake();
    }

    public void SetLoginData(LobbyLoginResponsePacket res)
    {
        UserInfo = new UserInfo { UniqueId = res.UserId };

        CacheCharacters(res.Characters);
        CacheParties(res.DefaultParty);
    }

    public void UpdateCharacters(List<CharacterInfo> characters)
    {
        if (UserInfo == null)
            UserInfo = new UserInfo();
        CacheCharacters(characters);
    }

    private void CacheCharacters(List<CharacterInfo> characters)
    {
        if (characters == null || characters.Count == 0)
        {
            UserInfo.HaveCharacters = new List<AllyInfo>();
            return;
        }

        var list = new List<AllyInfo>(characters.Count);
        for (int i = 0; i < characters.Count; i++)
        {
            list.Add(ToAllyInfo(characters[i]));
        }
        UserInfo.HaveCharacters = list;
    }

    /// <summary>
    /// 계약 CharacterInfo(ID 중심·서버권위)를 로컬 AllyInfo로 변환.
    /// 이름/속성/인내력 등 표시 데이터는 와이어 미운반 → TemplateId로 콘텐츠(CharacterDatabase) 조회.
    /// </summary>
    private AllyInfo ToAllyInfo(CharacterInfo c)
    {
        var ally = new AllyInfo
        {
            UnitId      = c.CharacterId.ToString(),
            Level       = c.Level,
            Hp          = c.Hp,
            Speed       = c.Speed,
            AttackPower = c.AttackPower,
            Defense     = c.Defense,
        };

        // TODO(id 규약 — plan/turnrpg-server 확정): CharacterData.CharacterId(string) == TemplateId.ToString() 가정.
        var data = GameData?.Characters?.Get(c.TemplateId.ToString());
        if (data != null)
        {
            ally.Name        = data.CharacterName;
            ally.Element     = data.Element;
            ally.MaxTenacity = data.BaseTenacity;
        }
        return ally;
    }

    private void CacheParties(PartyDto defaultParty)
    {
        var haveChars = UserInfo.HaveCharacters;
        var charLookup = new Dictionary<string, AllyInfo>(haveChars.Count);
        for (int i = 0; i < haveChars.Count; i++)
        {
            charLookup[haveChars[i].UnitId] = haveChars[i];
        }

        var setInfos = BuildAllySet(defaultParty, charLookup);

        // 계약은 단일 DefaultParty만 운반 → 활성 프리셋(0)에 반영.
        // ⚠️ 멀티 파티프리셋은 turnrpg 계약 미커버 — 로컬 프리셋 관리는 후속(스코프 결정 대기).
        PartyCache.Instance.SetPreset(0, setInfos);
        if (setInfos.Count > 0)
            PartyCache.Instance.SetValidatedParty(setInfos);
    }

    /// <summary>계약 PartyDto.Slots(CharacterId+TileIndex)를 로컬 AllySetInfo로 변환(보유 캐릭 조회).</summary>
    private static List<AllySetInfo> BuildAllySet(PartyDto party, Dictionary<string, AllyInfo> charLookup)
    {
        var setInfos = new List<AllySetInfo>();
        if (party?.Slots == null) return setInfos;

        foreach (var slot in party.Slots)
        {
            charLookup.TryGetValue(slot.CharacterId.ToString(), out var ally);
            setInfos.Add(new AllySetInfo { Ally = ally, TileIndex = slot.TileIndex });
        }
        return setInfos;
    }
}
