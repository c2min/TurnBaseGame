using Cysharp.Threading.Tasks;
using SMDevLibrary.Managers;
using SMDevLibrary.Network.Utility;
// Core는 ENetworkStatusCode만 타깃 별칭(광역 import 시 로컬 동명 타입 충돌 회피). 로비 패킷은 TurnRPG 광역.
using ENetworkStatusCode = SM.Contracts.Core.ENetworkStatusCode;
using SM.Contracts.TurnRPG;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbySceneController : SceneController
{
    [SerializeField]
    private Button _characterButton;

    [SerializeField]
    private Button _partyEditButton;

    [SerializeField]
    private Button _stageButton;

   
    [SerializeField]
    private Button _inventoryButton;

    private UIInventoryPopup _inventoryPopup;

    protected override void Awake()
    {
        base.Awake();

        RegisterPacketHandler<LobbyLoginResponsePacket>(OnLogin);
        RegisterPacketHandler<PartyValidateResponsePacket>(OnPartyValidate);
        RegisterPacketHandler<CharacterListResponsePacket>(OnCharacterList);
        RegisterPacketHandler<ResponseInventoryList>(OnInventoryList);
        RegisterPacketHandler<ResponseSellItem>(OnSellItem);
        RegisterPacketHandler<ResponseEquipItem>(OnEquipItem);
        RegisterPacketHandler<ResponseUnequipItem>(OnUnequipItem);

        _inventoryButton?.onClick.AddListener(OnClickInventoryButton);
        _characterButton.onClick.AddListener(OnClickCharacterButton);
        _partyEditButton.onClick.AddListener(OnClickPartyEditButton);
        _stageButton.onClick.AddListener(OnClickStageButton);
    }

    private async void OnClickCharacterButton()
    {
        var characters = Client.Instance.UserInfo?.HaveCharacters ?? new System.Collections.Generic.List<AllyInfo>();
        var popup = await UIManager.Instance.Show<UICharacterSelectPopup>(p=> p.Open(-1, null, characters, null));
    }

    private void OnClickPartyEditButton()
    {
        UIManager.Instance.Show<UIPartyEditPopup>();
    }

    private void OnClickStageButton()
    {
        UIManager.Instance.Show<UIStageSelectPopup>(p => p.Open(OnStageSelected));
    }

    private void OnStageSelected(int stageId)
    {
        // StageId 정본=int(ADR-006). 선택 UI에서 받은 StageId로 전투 진입(전투 init=서버 BattleSnapshot 권위).
        var req = new StageEnterRequestPacket
        {
            StageId = stageId,
            Party = BuildPartyDto(PartyCache.Instance.AllySetInfos),
        };

        TurnRpgSceneFlow.Instance.LoadScene("InGameScene", req).Forget();
    }

    /// <summary>
    /// 편성 화면에서 파티 구성 완료 시 호출
    /// </summary>
    public void OnPartyConfirmed(List<AllySetInfo> allySetInfos)
    {
        // 클라이언트 캐시 저장
        PartyCache.Instance.SetParty(allySetInfos);

        // 서버에 검증 요청
        UnityNetworkBridge.Instance.SendPacket(new PartyValidateRequestPacket
        {
            Slots = BuildSlots(allySetInfos)
        });
    }

    /// <summary>로컬 편성(AllySetInfo)을 계약 PartyDto로 변환.</summary>
    private static PartyDto BuildPartyDto(IReadOnlyList<AllySetInfo> allySetInfos)
        => new PartyDto { PartyId = string.Empty, Slots = BuildSlots(allySetInfos) };

    /// <summary>AllySetInfo → PartySlotDto. CharacterId=long(레거시 string UnitId 파싱), TileIndex 직매핑.</summary>
    private static List<PartySlotDto> BuildSlots(IReadOnlyList<AllySetInfo> allySetInfos)
    {
        var slots = new List<PartySlotDto>();
        if (allySetInfos == null) return slots;
        foreach (var set in allySetInfos)
        {
            if (set?.Ally == null) continue;
            // 계약 CharacterId=인스턴스 id(long). UnitId(string)=인스턴스 id 표현 → long 파싱(Phase1: ==TemplateId).
            long.TryParse(set.Ally.UnitId, out var characterId);
            slots.Add(new PartySlotDto { CharacterId = characterId, TileIndex = set.TileIndex });
        }
        return slots;
    }

    private void OnLogin(LobbyLoginResponsePacket res)
    {
        if (res.Code != ENetworkStatusCode.Success)
        {
            Debug.LogWarning($"Login Failed: {res.Code}");
            return;
        }

        Client.Instance.SetLoginData(res);
        Debug.Log($"Login Success: {res.UserId}");
    }

    private void OnPartyValidate(PartyValidateResponsePacket res)
    {
        if (res.Code != ENetworkStatusCode.Success || !res.IsValid)
        {
            Debug.LogWarning($"파티 검증 실패: {string.Join(", ", res.InvalidCharacterIds)}");

            return;
        }

        PartyCache.Instance.OnValidated();
    }

    private void OnClickInventoryButton()
    {
        UnityNetworkBridge.Instance.SendPacket(RequestInventoryList.Shared);
    }

    private async void OnInventoryList(ResponseInventoryList res)
    {
        if (res.Code != ENetworkStatusCode.Success)
            return;

        InventoryCache.Instance.Set(res.Items, Client.Instance.GameData.Items, res.MaxCount);
        _inventoryPopup = await UIManager.Instance.Show<UIInventoryPopup>(p => p.Open());
    }

    private void OnSellItem(ResponseSellItem res)
    {
        if (_inventoryPopup != null)
            _inventoryPopup.OnSellResponse(res);
    }

    private void OnCharacterList(CharacterListResponsePacket res) { }

    private void OnEquipItem(ResponseEquipItem res)
    {
        if (_inventoryPopup != null)
            _inventoryPopup.OnEquipResponse(res);
    }

    private void OnUnequipItem(ResponseUnequipItem res)
    {
        if (_inventoryPopup != null)
            _inventoryPopup.OnUnequipResponse(res);
    }
}