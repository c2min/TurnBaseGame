using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "TurnBase/Item/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField]
    private ItemData[] _items;

    private Dictionary<int, ItemData> _lookup;

    private void OnEnable()
    {
        if (_items == null)
            return;

        _lookup = new Dictionary<int, ItemData>();
        foreach (var data in _items)
        {
            if (data != null)
            {
                _lookup[data.TemplateId] = data;
            }
        }
    }

    public ItemData Get(int templateId)
    {
        if (_lookup == null)
            OnEnable();
        _lookup.TryGetValue(templateId, out var data);
        return data;
    }
}
