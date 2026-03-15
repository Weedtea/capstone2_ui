using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 부모(플레이어) 레벨에 배치 - 말 선택 및 이동 관리
/// 말 2개 체계 + 업기 + 자동 선택 지원
/// 
/// ★ 설정 방법: 각 말(자식)에서 기존 YutPieceSelector 컴포넌트를 제거하고,
///   부모 오브젝트(p1, p2)에 이 컴포넌트를 추가하세요.
/// </summary>
public class YutPieceSelector : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [Header("하이라이트 효과")]
    public GameObject arrowPrefab;
    public float arrowHeightOffset = 1.5f;
    private List<GameObject> selectableArrows = new List<GameObject>();

    private Yut_Player_Manager selectedPiece = null;

    void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update()
    {
        var activePieces = GetActivePieces();
        if (activePieces.Count == 0) return;

        // 턴 체크 (공유 상태이므로 첫 번째 말 기준)
        var firstPiece = activePieces[0];
        if (!firstPiece.isPlayerTurn || !firstPiece.isThrowed || firstPiece.moveCountList.Count == 0)
        {
            if (selectedPiece != null) DeselectPiece();
            ClearSelectableArrows();
            return;
        }

        // 이동 중인 말이 있으면 대기
        bool anyMoving = false;
        foreach (var p in activePieces)
        {
            if (p.GetComponent<YutPlayerMove>().isMoving) { anyMoving = true; break; }
        }
        if (anyMoving)
        {
            ClearSelectableArrows();
            return;
        }

        // 선택된 말이 없을 때만 선택 가능한 화살표 표시
        if (selectedPiece == null)
        {
            if (selectableArrows.Count == 0 && activePieces.Count > 1)
            {
                ShowSelectableArrows(activePieces);
            }
            
            // 활성 말이 1개뿐이면 자동 선택
            if (activePieces.Count == 1)
            {
                SelectPiece(activePieces[0]);
            }
        }
        else
        {
            ClearSelectableArrows();
        }

        // 마우스 클릭 처리
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleMouseClick(activePieces);
        }
    }

    void HandleMouseClick(List<Yut_Player_Manager> activePieces)
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit)) return;

        // 1) 말 또는 말 위의 화살표 클릭 확인 → 선택/변경
        Yut_Player_Manager clickedPiece = hit.collider.GetComponent<Yut_Player_Manager>();
        
        // 화살표 클릭 체크 (말 선택용)
        if (clickedPiece == null)
        {
            ArrowLink link = hit.collider.GetComponentInParent<ArrowLink>();
            if (link != null && link.targetPiece != null) clickedPiece = link.targetPiece;
        }

        if (clickedPiece != null && activePieces.Contains(clickedPiece))
        {
            if (selectedPiece != clickedPiece)
            {
                if (selectedPiece != null) DeselectPiece();
                SelectPiece(clickedPiece);
            }
            return;
        }

        // 2) WayPoint 또는 화살표 클릭 확인 → 이동 (말이 선택된 상태에서만)
        if (selectedPiece != null)
        {
            WayPoint hitWayPoint = hit.collider.GetComponent<WayPoint>();
            
            // 화살표 클릭 체크 (목적지 이동용)
            if (hitWayPoint == null)
            {
                ArrowLink link = hit.collider.GetComponentInParent<ArrowLink>();
                if (link != null && link.targetWayPoint != null) hitWayPoint = link.targetWayPoint;
            }

            var wpc = selectedPiece.GetComponent<YutWayPointColorChange>();

            if (hitWayPoint != null && wpc.targetPoints.ContainsKey(hitWayPoint))
            {
                MoveSelectedPiece(hitWayPoint, wpc);
            }
        }
    }

    /// <summary>
    /// 말을 선택하고 이동 가능한 WayPoint를 표시합니다.
    /// </summary>
    void SelectPiece(Yut_Player_Manager piece)
    {
        ClearSelectableArrows();
        selectedPiece = piece;
        piece.isSelected = true;

        var wpc = piece.GetComponent<YutWayPointColorChange>();
        wpc.CalculateTargetPoints();
        wpc.ShowHighlights();

        Debug.Log($"[선택] {piece.gameObject.name} 선택됨. 이동할 칸을 클릭하세요.");
    }

    /// <summary>
    /// 말 선택을 해제합니다.
    /// </summary>
    void DeselectPiece()
    {
        if (selectedPiece != null)
        {
            var wpc = selectedPiece.GetComponent<YutWayPointColorChange>();
            wpc.ClearHighlights();
            selectedPiece.isSelected = false;
            selectedPiece = null;
        }
    }

    private void ShowSelectableArrows(List<Yut_Player_Manager> pieces)
    {
        if (arrowPrefab == null) return;

        foreach (var p in pieces)
        {
            Vector3 spawnPos = p.transform.position + Vector3.up * arrowHeightOffset;
            GameObject arrow = Instantiate(arrowPrefab, spawnPos, Quaternion.Euler(90f, 0, 0));
            arrow.transform.localScale = new Vector3(150f, 150f, 150f); // [NEW] 크기 조정
            
            // 말의 색상과 맞춤
            Renderer pieceRenderer = p.GetComponentInChildren<Renderer>();
            Renderer arrowRenderer = arrow.GetComponentInChildren<Renderer>();
            if (pieceRenderer != null && arrowRenderer != null)
            {
                arrowRenderer.material = pieceRenderer.material;
            }

            // 클릭 링크 추가
            ArrowLink link = arrow.AddComponent<ArrowLink>();
            link.targetPiece = p;
            arrow.AddComponent<ArrowMove>(); // [NEW] 움직임 컴포넌트 추가

            if (arrow.GetComponent<Collider>() == null)
            {
                arrow.AddComponent<BoxCollider>();
            }

            selectableArrows.Add(arrow);
        }
    }

    private void ClearSelectableArrows()
    {
        foreach (var arrow in selectableArrows)
        {
            if (arrow != null) Destroy(arrow);
        }
        selectableArrows.Clear();
    }

    /// <summary>
    /// 선택된 말을 목표 WayPoint로 이동시킵니다.
    /// </summary>
    void MoveSelectedPiece(WayPoint target, YutWayPointColorChange wpc)
    {
        var routeInfo = wpc.targetPoints[target];
        int moveCount = routeInfo.moveCount;

        // moveCountList에서 사용한 이동 횟수 제거 (공유 레퍼런스이므로 한 번만)
        selectedPiece.moveCountList.Remove(moveCount);

        // currentMoveCount를 팀 전체에 설정 (YutCatchAndStack에서 빽도 체크용)
        foreach (var p in selectedPiece.GetTeamPieces())
            p.currentMoveCount = moveCount;

        // 이동 실행
        var playerMove = selectedPiece.GetComponent<YutPlayerMove>();
        playerMove.MoveAlongRoute(routeInfo);

        // 하이라이트 종료
        wpc.ClearHighlights();
        selectedPiece.isSelected = false;

        // 이동 후 처리 (업기, 데미지, 발판 효과, 승리 체크)
        var catchAndStack = selectedPiece.GetComponent<YutCatchAndStack>();
        if (catchAndStack != null) catchAndStack.StartPostMoveCheck();

        selectedPiece = null;
    }

    /// <summary>
    /// 활성 말(도착하지 않고, 업혀있지 않은) 목록 반환
    /// </summary>
    List<Yut_Player_Manager> GetActivePieces()
    {
        var all = GetComponentsInChildren<Yut_Player_Manager>(true);
        var active = new List<Yut_Player_Manager>();
        foreach (var p in all)
        {
            if (!p.hasFinished && p.carriedBy == null && p.gameObject.activeInHierarchy)
                active.Add(p);
        }
        return active;
    }
}
