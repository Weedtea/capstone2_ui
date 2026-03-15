using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemTargetSelector : MonoBehaviour
{
    public static ItemTargetSelector Instance { get; private set; }

    [Header("타겟팅 설정")]
    public GameObject arrowPrefab; // Assets/00_Game/03_Art/arrow.fbx 기반 프리팹 권장
    public float arrowHeightOffset = 1.5f;
    public Vector3 arrowScale = new Vector3(150f, 150f, 150f);
    public Vector3 arrowRotation = new Vector3(90f, 0f, 0f); // X축 90도 회전

    private bool isTargeting = false;
    private YutItem currentUsingItem;
    private GameObject itemUser; // 아이템을 사용하는 플레이어
    private List<GameObject> activeArrows = new List<GameObject>();

    private Camera mainCamera;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        mainCamera = Camera.main;
    }

    void Update()
    {
        if (!isTargeting) return;

        // 클릭으로 타겟 선택
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleTargetClick();
        }

        // 우클릭으로 타겟팅 취소
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelTargeting();
        }
    }

    /// <summary>
    /// 아이템 타겟팅 모드를 시작합니다.
    /// </summary>
    public void StartTargeting(YutItem item, GameObject user)
    {
        if (isTargeting) return;

        currentUsingItem = item;
        itemUser = user;
        isTargeting = true;

        if (arrowPrefab == null)
        {
            Debug.LogError("[ItemTargetSelector] arrowPrefab이 할당되지 않았습니다!");
            return;
        }

        // 해당 플레이어 소유의 활성화된 말(Piece) 위에만 화살표 생성
        var userPieces = user.GetComponentsInChildren<Yut_Player_Manager>(true);
        foreach (var p in userPieces)
        {
            // 아직 도착하지 않았고 맵 위에 있는 말만 대상
            if (!p.hasFinished && p.gameObject.activeInHierarchy && p.carriedBy == null)
            {
                CreateArrowOverPiece(p);
            }
        }

        Debug.Log($"[아이템] '{item.itemName}' 사용 대기. 타겟을 선택하세요.");
    }

    private void CreateArrowOverPiece(Yut_Player_Manager piece)
    {
        Vector3 spawnPos = piece.transform.position + Vector3.up * arrowHeightOffset;
        GameObject arrow = Instantiate(arrowPrefab, spawnPos, Quaternion.Euler(arrowRotation));
        arrow.transform.localScale = arrowScale;

        // 말의 색상과 동일하게 화살표 색상 맞춤
        Renderer pieceRenderer = piece.GetComponentInChildren<Renderer>();
        Renderer arrowRenderer = arrow.GetComponentInChildren<Renderer>();
        if (pieceRenderer != null && arrowRenderer != null)
        {
            arrowRenderer.material = pieceRenderer.material;
        }

        // 타겟 링크 추가
        ArrowTargetLink link = arrow.AddComponent<ArrowTargetLink>();
        link.targetPiece = piece;

        // 클릭 감지용 콜라이더 보장
        if (arrow.GetComponent<Collider>() == null)
        {
            arrow.AddComponent<BoxCollider>();
        }

        // 화살표 애니메이션(옵션)
        if (arrow.GetComponent<ArrowMove>() == null)
        {
            arrow.AddComponent<ArrowMove>();
        }

        activeArrows.Add(arrow);
    }

    private void HandleTargetClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Yut_Player_Manager selectedTarget = null;

            // 1. 화살표를 직접 클릭했는지
            ArrowTargetLink link = hit.collider.GetComponentInParent<ArrowTargetLink>();
            if (link != null)
            {
                selectedTarget = link.targetPiece;
            }
            // 2. 말(Piece)을 바로 클릭했는지
            else
            {
                Yut_Player_Manager piece = hit.collider.GetComponent<Yut_Player_Manager>();
                if (piece != null)
                {
                    // 활성화된 타겟 리스트에 포함된 말인지 간접 확인 (현재 띄워진 화살표의 타겟 중 하나인지)
                    foreach (var a in activeArrows)
                    {
                        if (a.GetComponent<ArrowTargetLink>().targetPiece == piece)
                        {
                            selectedTarget = piece;
                            break;
                        }
                    }
                }
            }

            if (selectedTarget != null)
            {
                ApplyItemEffect(selectedTarget);
            }
        }
    }

    private void ApplyItemEffect(Yut_Player_Manager target)
    {
        if (currentUsingItem != null)
        {
            // YutItem에서 실제 효과 실행 로직 호출
            currentUsingItem.UseItemTargeted(target.gameObject, itemUser);
            
            // 인벤토리에서 아이템 제거
            YutInventory inv = itemUser.GetComponentInParent<YutInventory>();
            if(inv != null) inv.RemoveItem(currentUsingItem);
        }

        ClearTargetingState();

        // 관리자 측에 아이템 사용 완료 통보 (윷 던지기 권한 복구)
        Yut_YutParent_Manager manager = FindAnyObjectByType<Yut_YutParent_Manager>();
        if (manager != null)
        {
            manager.OnItemUseCompleted();
        }
    }

    public void CancelTargeting()
    {
        if (!isTargeting) return;
        Debug.Log("[아이템] 타겟 지정을 취소했습니다.");
        ClearTargetingState();

        Yut_YutParent_Manager manager = FindAnyObjectByType<Yut_YutParent_Manager>();
        if (manager != null)
        {
            manager.OnItemUseCanceled();
        }
    }

    private void ClearTargetingState()
    {
        isTargeting = false;
        currentUsingItem = null;
        itemUser = null;

        foreach (var arrow in activeArrows)
        {
            if (arrow != null) Destroy(arrow);
        }
        activeArrows.Clear();
    }

    public bool IsTargeting()
    {
        return isTargeting;
    }
}

/// <summary>
/// 아이템 타겟 지정 화살표가 어떤 말을 가리키는지 저장하는 컴포넌트
/// </summary>
public class ArrowTargetLink : MonoBehaviour
{
    public Yut_Player_Manager targetPiece;
}
