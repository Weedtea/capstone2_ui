using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class CostumeData
{
    public string costumeID;
    public Sprite icon;
    public int price;
}

/// <summary>
/// 옷장 슬롯을 인스턴스화하여 그리드에 뿌려주고 상호작용 및 재화(모달창 제어)를 담당합니다.
/// </summary>
public class LockerManager : MonoBehaviour
{
    [Header("--- User Data (Save/Load) ---")]
    public int currentGold = 1000; // 초기 테스트용 재화
    private string equippedCostumeID = ""; // 현재 장착 중인 ID
    private HashSet<string> ownedCostumes = new HashSet<string>();

    [Header("--- Locker UI Elements ---")]
    [SerializeField] private RectTransform scrollRectContent; // Grid Layout Group이 있는 Content 영역
    [SerializeField] private GameObject slotPrefab; // 조금 전 만든 코스튬 슬롯 프리팹

    [Header("--- Purchase Modal UI ---")]
    [SerializeField] private GameObject purchaseModalPanel; // Dim 패널 전체
    [SerializeField] private TextMeshProUGUI modalMessageText; // "구매하시겠습니까?" 텍스트
    [SerializeField] private TextMeshProUGUI modalPriceText; // 상품 가격 표시용
    [SerializeField] private Button buyButton;    // 구매 버튼 (Yes)
    [SerializeField] private Button cancelButton; // 취소 버튼 (No)

    [Header("--- Database (Test) ---")]
    [SerializeField] private List<CostumeData> costumeDatabase;

    // 현재 구동 중인 슬롯들의 참조
    private List<LockerSlot> spawnedSlots = new List<LockerSlot>();

    // 모달창 구매 진행 시 사용할 임시 데이터 보관
    private LockerSlot slotPendingPurchase;
    private string purchaseCostumeID;
    private int purchasePrice;

    private void Start()
    {
        // 1. 유저 보유 아이템 임시 데이터
        ownedCostumes.Add("Costume_Basic"); // 기본 코스튬은 이미 보유
        equippedCostumeID = "Costume_Basic"; // 기본 코스튬 강제 장착

        // 2. 모달창 이벤트 등록
        if (buyButton) buyButton.onClick.AddListener(ConfirmPurchase);
        if (cancelButton) cancelButton.onClick.AddListener(ClosePurchaseModal);

        // 3. UI 그리기 시작
        PopulateGrid();
    }

    /// <summary>
    /// 창이 열릴 때 호출되는 함수
    /// </summary>
    public void OpenLocker()
    {
        gameObject.SetActive(true);
        // 그리드나 데이터 최신화가 필요하다면 여기서 호출 가능합니다.
        // PopulateGrid(); 
    }

    /// <summary>
    /// Content 안에 Grid 형태로 코스튬 슬롯들을 찍어냅니다.
    /// </summary>
    private void PopulateGrid()
    {
        // 기존 생성된 임시 슬롯이 있다면 파괴 (오류 방지)
        foreach (Transform child in scrollRectContent)
        {
            Destroy(child.gameObject);
        }
        spawnedSlots.Clear();

        // 딕셔너리(리스트)에 있는 데이터를 기반으로 슬롯 생성
        foreach (CostumeData data in costumeDatabase)
        {
            GameObject slotObj = Instantiate(slotPrefab, scrollRectContent);
            slotObj.SetActive(true); // <--- 추가! (숨겨진 프리팹을 복사했으므로 켜줘야 함)
            LockerSlot lockerSlot = slotObj.GetComponent<LockerSlot>();

            bool isOwned = ownedCostumes.Contains(data.costumeID);
            bool isEquipped = (equippedCostumeID == data.costumeID);

            // 슬롯 내부의 Init 함수를 호출하여 상태값 세팅
            lockerSlot.Init(this, data.costumeID, data.icon, data.price, isOwned, isEquipped);
            
            spawnedSlots.Add(lockerSlot);
        }
    }

    /// <summary>
    /// 보유한 아이템을 누르면 장착을 시도합니다. (LockerSlot.cs에서 호출)
    /// </summary>
    public void EquipItem(string itemID, LockerSlot clickedSlot)
    {
        equippedCostumeID = itemID;

        // 전체 슬롯 UI들을 돌면서 '방금 클릭한 슬롯'만 Outline을 켜고, 나머지는 끕니다.
        foreach (LockerSlot slot in spawnedSlots)
        {
            bool isThisEquipped = (slot == clickedSlot);
            // 시각적 상태 다시 갱신
            slot.UpdateVisuals(isThisEquipped);
        }

        Debug.Log($"Equipped: {itemID}");
        // TODO: 실제 3D 캐릭터 모델 메쉬 변경 로직 호출!
    }

    /// <summary>
    /// 미보유 아이템을 누르면 모달창을 오픈합니다. (LockerSlot.cs에서 호출)
    /// </summary>
    public void OpenPurchaseModal(string id, int price, LockerSlot slot)
    {
        purchaseCostumeID = id;
        purchasePrice = price;
        slotPendingPurchase = slot;

        // UI 텍스트 갱신
        modalMessageText.text = "Buy this costume?";
        modalPriceText.text = $"Price: <color=yellow>{price}</color> G\n(My Money: {currentGold} G)";

        // 전체 화면 Dim + 팝업 활성화
        purchaseModalPanel.SetActive(true);
    }

    /// <summary>
    /// 모달 창의 "취소(No)" 버튼과 연결됩니다.
    /// </summary>
    public void ClosePurchaseModal()
    {
        purchaseModalPanel.SetActive(false);
    }

    /// <summary>
    /// 모달 창의 "구매(Yes)" 버튼과 연결됩니다.
    /// </summary>
    public void ConfirmPurchase()
    {
        // 돈이 충분하다면!
        if (currentGold >= purchasePrice)
        {
            // 재화 소모 연산
            currentGold -= purchasePrice;
            ownedCostumes.Add(purchaseCostumeID); // 미보유 -> 보유

            // 해당 슬롯 UI 단독 갱신 (선명하게 켜기)
            slotPendingPurchase.PurchaseSuccess();

            Debug.Log($"[System] Purchased {purchaseCostumeID}! Remaining Gold: {currentGold}");
            
            // 모달창 닫기
            ClosePurchaseModal();
        }
        else
        {
            // 돈 부족 시 피드백
            modalMessageText.text = "<color=red>Not enough Gold (G)!</color>";
        }
    }
}
