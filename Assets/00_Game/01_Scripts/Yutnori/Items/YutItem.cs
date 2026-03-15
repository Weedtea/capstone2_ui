using UnityEngine;

/// <summary>
/// 윷놀이 아이템의 데이터 구조를 정의하는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "New YutItem", menuName = "Yutnori/Item")]
public class YutItem : ScriptableObject
{
    public string itemName;
    public string description;
    public Sprite icon;
    
    // 나중에 효과 로직을 추가하기 위한 가상 메서드
    public virtual void Use()
    {
        Debug.Log($"[아이템 사용] {itemName}: {description}");
    }

    // 타겟 지정을 포함한 아이템 사용 가상 메서드
    public virtual bool UseItemTargeted(GameObject target, GameObject user)
    {
        Debug.Log($"[{itemName} 사용 완료] 사용자: {user.name} -> 타겟: {target.name}");
        return true;
    }
}
