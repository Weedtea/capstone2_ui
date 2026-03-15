using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어별 아이템 인벤토리를 관리하는 컴포넌트
/// </summary>
public class YutInventory : MonoBehaviour
{
    public const int MAX_ITEM_COUNT = 10;
    
    [Header("소지 아이템 목록")]
    public List<YutItem> items = new List<YutItem>();

    /// <summary>
    /// 아이템을 인벤토리에 추가합니다. (최대 10개 제한)
    /// </summary>
    /// <param name="item">추가할 아이템</param>
    /// <returns>추가 성공 여부</returns>
    public bool AddItem(YutItem item)
    {
        if (items.Count >= MAX_ITEM_COUNT)
        {
            Debug.LogWarning($"[인벤토리] {gameObject.name}의 인벤토리가 가득 찼습니다! (최대 {MAX_ITEM_COUNT}개)");
            return false;
        }

        items.Add(item);
        Debug.Log($"[인벤토리] {gameObject.name} 아이템 획득: {item.itemName} (현재: {items.Count}/{MAX_ITEM_COUNT})");
        return true;
    }

    /// <summary>
    /// 아이템을 사용하고 인벤토리에서 제거합니다.
    /// </summary>
    /// <param name="index">사용할 아이템 인덱스</param>
    public void UseItem(int index)
    {
        if (index >= 0 && index < items.Count)
        {
            YutItem item = items[index];
            item.Use();
            items.RemoveAt(index);
        }
    }

    /// <summary>
    /// 아이템을 리스트에서 직접 제거합니다.
    /// </summary>
    public void RemoveItem(YutItem item)
    {
        if (items.Contains(item))
        {
            items.Remove(item);
            Debug.Log($"[인벤토리] {gameObject.name} 아이템 제거: {item.itemName}");
        }
    }
}
