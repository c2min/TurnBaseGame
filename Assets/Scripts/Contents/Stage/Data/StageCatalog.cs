using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SMDevLibrary.Generics;
using SMDevLibrary.Localization;
using UnityEngine;

// stage_catalog.json(plan dual-export) 로드·캐시.
// JSON 직접 로드(LocalizedString=Newtonsoft tolerant 컨버터) · key=int stageId · SortOrder 정렬.
public class StageCatalog : LazySingleton<StageCatalog>
{
    private const string ResourcePath = "Data/stage_catalog";

    private readonly Dictionary<int, StageCatalogEntry> _entries = new();
    private readonly List<int> _sortedStageIds = new();

    public IReadOnlyList<int> SortedStageIds => _sortedStageIds;

    public void EnsureLoaded()
    {
        if (_entries.Count > 0) return;
        Load();
    }

    private void Load()
    {
        var asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            Debug.LogWarning($"<color=#26A69A>[Data/StageCatalog]</color> :> stage_catalog 미발견: Resources/{ResourcePath}");
            return;
        }

        var settings = new JsonSerializerSettings { Converters = { new LocalizedStringConverter() } };
        var map = JsonConvert.DeserializeObject<Dictionary<int, StageCatalogEntry>>(asset.text, settings);
        if (map == null) return;

        _entries.Clear();
        foreach (var pair in map)
            _entries[pair.Key] = pair.Value;

        _sortedStageIds.Clear();
        _sortedStageIds.AddRange(_entries.Keys.OrderBy(id => _entries[id].SortOrder));
    }

    public bool TryGet(int stageId, out StageCatalogEntry entry) => _entries.TryGetValue(stageId, out entry);
}
