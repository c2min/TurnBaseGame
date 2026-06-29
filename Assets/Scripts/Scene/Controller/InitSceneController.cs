using Cysharp.Threading.Tasks;
using SM.Contracts.TurnRPG;
using UnityEngine;

public class InitSceneController : SceneController
{
    private const string GuestIdKey = "GuestId";

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        string guestId = PlayerPrefs.GetString(GuestIdKey, "");
        if (string.IsNullOrEmpty(guestId))
        {
            guestId = System.Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(GuestIdKey, guestId);
            PlayerPrefs.Save();
        }

        // 계약 LobbyLoginRequestPacket는 Token만 운반(서버 HandleLogin=토큰 stub).
        // 간이: 기기 guestId를 Token으로 전달. 실 소셜인증(Firebase UID/토큰)은 O7 identity 착지 시 교체.
        var req = new LobbyLoginRequestPacket
        {
            Token = guestId,
        };

        TurnRpgSceneFlow.Instance.LoadScene("LobbyScene", req).Forget();
    }
}
