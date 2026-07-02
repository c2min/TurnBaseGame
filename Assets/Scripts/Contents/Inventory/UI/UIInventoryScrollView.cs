using System;
using System.Collections.Generic;
using SMDevLibrary.UI.Layout;

public class UIInventoryScrollView : RecycleScrollView<ItemInstance, UIInventorySlot>
{
    private ItemInstance _selectedItem;
    private Action<ItemInstance> _onSelected;

    public void SetItems(IList<ItemInstance> items, Action<ItemInstance> onSelected)
    {
        _onSelected  = onSelected;
        _selectedItem = null;
        SetData(items);
    }

    public void ClearSelection()
    {
        _selectedItem = null;
        RefreshVisible();
    }

    protected override void OnBindElement(UIInventorySlot element, ItemInstance data, int index)
    {
        element.SetOnClicked(OnSlotClicked);
        element.SetSelected(data == _selectedItem);
    }

    private void OnSlotClicked(ItemInstance item)
    {
        _selectedItem = item;
        RefreshVisible();
        _onSelected?.Invoke(item);
    }
}
