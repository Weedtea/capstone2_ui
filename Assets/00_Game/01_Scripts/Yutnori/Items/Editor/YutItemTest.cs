using UnityEngine;

/// <summary>
/// 아이템 시스템이 정상적으로 작동하는지 확인하기 위한 테스트 스크립트
/// </summary>
public class YutItemTest : MonoBehaviour
{
    public YutItem testItem;
    
    void Start()
    {
        YutInventory inventory = GetComponent<YutInventory>();
        if (inventory == null)
        {
            Debug.LogError($"[테스트] {gameObject.name}에 YutInventory가 없습니다!");
            return;
        }

        if (testItem == null)
        {
            Debug.LogError("[테스트] 테스트할 아이템 데이터가 할당되지 않았습니다!");
            return;
        }

        Debug.Log("[테스트] 아이템 11개 추가 시도 시작 (최대 10개 제한 확인)");
        
        for (int i = 1; i <= 11; i++)
        {
            bool success = inventory.AddItem(testItem);
            if (i <= 10)
            {
                if (!success) Debug.LogError($"[테스트] {i}번째 아이템 추가 실패! (오류)");
            }
            else
            {
                if (success) Debug.LogError("[테스트] 11번째 아이템이 추가되었습니다! (제한 로직 오류)");
                else Debug.Log("[테스트] 11번째 아이템 추가 차단됨 (정상)");
            }
        }
        
        Debug.Log($"[테스트] 최종 아이템 수: {inventory.items.Count}");
    }
}
