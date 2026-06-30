using Cysharp.Threading.Tasks;
using SMDevLibrary.Command;
using SMDevLibrary.VFX;
using System.Threading;
using UnityEngine;

/// <summary>
/// 대상 위치에 VFX를 재생하고 완료까지 대기하는 커맨드.
/// VFXManager가 씬에 없으면 즉시 완료 처리합니다.
/// </summary>
public class PlayVFXCommand : ICommand
{
    private readonly VFXClip  _clip;
    private readonly Vector3  _position;

    public PlayVFXCommand(VFXClip clip, Vector3 position)
    {
        _clip     = clip;
        _position = position;
    }

    public UniTask ExecuteAsync(CancellationToken ct)
    {
        if (_clip == null || VFXManager.Instance == null)
            return UniTask.CompletedTask;

        return VFXManager.Instance.PlayAsync(_clip, _position, Quaternion.identity, ct);
    }
}
