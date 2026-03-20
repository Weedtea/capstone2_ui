using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 개별 코스튬 슬롯의 UI 시각적 상태와 버튼 이벤트를 제어합니다.
/// 프리팹(Prefab)의 최상단 오브젝트에 이 스크립트를 부착해 주세요.
/// </summary>
public class LockerSlot : MonoBehaviour
{
    [Header("--- Components ---")]
    [SerializeField] private Image costumeImage; // 코스튬 아이콘 이미지
    [SerializeField] private Button slotButton;  // 터치/클릭을 받을 버튼 레이어
    [SerializeField] private Outline equipOutline; // "장착 중" 표시용 아웃라인

    private LockerManager manager;
    private string costumeID;
    private int price;
    private bool isOwned;

    /// <summary>
    /// 처음 그리드 생성 시 슬롯의 초기 데이터를 세팅합니다.
    /// </summary>
    public void Init(LockerManager mgr, string id, Sprite icon, int cost, bool owned, bool equipped)
    {
        manager = mgr;
        costumeID = id;
        price = cost;
        isOwned = owned;

        if (costumeImage != null)
            costumeImage.sprite = icon;

        // 기존 이벤트가 겹치지 않게 클리어 후 새로 등록 (안전 장치)
        slotButton.onClick.RemoveAllListeners();
        slotButton.onClick.AddListener(OnSlotClicked);

        // 시각적 상태 적용
        UpdateVisuals(equipped);
    }

    /// <summary>
    /// 보유 및 장착 상태에 따라 슬롯의 시각적 요소를 즉시 업데이트합니다.
    /// </summary>
    public void UpdateVisuals(bool isEquipped)
    {
        // 1. 보유/미보유에 따른 색상 및 알파값 조정 (기획 반영)
        if (isOwned)
        {
            costumeImage.color = Color.white; // 선명한 원본 색상
        }
        else
        {
            // 미보유 시 어둡게 Dim 처리. (RGB를 낮추고 알파를 0.5로 설정해 흐리게)
            costumeImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }

        // 2. 장착 중 여부에 따른 Outline 토글 (기획 반영)
        if (isEquipped)
        {
            equipOutline.enabled = true;
            equipOutline.effectColor = Color.yellow; // 눈에 확 띄는 노란색
            equipOutline.effectDistance = new Vector2(6f, -6f); // 굵기 강조
        }
        else
        {
            equipOutline.enabled = false;
        }
    }

    /// <summary>
    /// 유저가 슬롯을 클릭했을 때 트리거되는 액션
    /// </summary>
    private void OnSlotClicked()
    {
        if (isOwned)
        {
            // 보유 중 -> 즉시 장착
            manager.EquipItem(costumeID, this);
        }
        else
        {
            // 미보유 -> 구매 모달 팝업 오픈
            manager.OpenPurchaseModal(costumeID, price, this);
        }
    }

    /// <summary>
    /// 구매 완료 후 해당 슬롯을 '보유 상태'로 시각적 전환
    /// </summary>
    public void PurchaseSuccess()
    {
        isOwned = true;
        // 구매 완료 했다고 장착되는 건 아니므로 장착 여부는 false로 갱신하여 뚜렷하게만 만듭니다.
        UpdateVisuals(false); 
    }
}
