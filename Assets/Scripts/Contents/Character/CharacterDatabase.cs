using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "TurnBase/Character/CharacterDatabase")]
public class CharacterDatabase : ScriptableObject
{
    [SerializeField] private CharacterData[] _characters;

    private Dictionary<int, CharacterData> _lookup;

    private void OnEnable()
    {
        _lookup = new Dictionary<int, CharacterData>();
        foreach (var data in _characters)
        {
            if (data != null)
                _lookup[data.TemplateId] = data;
        }
    }

    /// <summary>TemplateId(int·콘텐츠 정본)로 캐릭터 정의 조회. 미등록=null(graceful).</summary>
    public CharacterData Get(int templateId)
    {
        _lookup.TryGetValue(templateId, out var data);
        return data;
    }
}
