using System.Collections.Generic;
using UnityEngine;

public class RouteInfo
{
    public int moveCount;
    public List<WayPoint> route = new List<WayPoint>();
}

public class YutWayPointColorChange : MonoBehaviour
{
    private WayPoint currentWayPoint;
    public Dictionary<WayPoint, RouteInfo> targetPoints = new Dictionary<WayPoint, RouteInfo>();

    private YutPlayerMove playerMove;
    private Yut_Player_Manager playerManager;
    public Material blue;

    [Header("하이라이트 효과")]
    public GameObject arrowPrefab;
    private List<GameObject> activeArrows = new List<GameObject>();
    public float arrowHeightOffset = 1.5f;

    void Awake()
    {
        playerMove = GetComponent<YutPlayerMove>();
        playerManager = GetComponent<Yut_Player_Manager>();
    }

    /// <summary>
    /// 현재 말 위치 기준으로 이동 가능한 목적지를 계산합니다.
    /// YutPieceSelector에서 말이 선택될 때 호출됩니다.
    /// </summary>
    public void CalculateTargetPoints()
    {
        targetPoints.Clear();
        currentWayPoint = playerMove.currentWayPoint;

        if (currentWayPoint == null) return;
        if (playerManager.moveCountList.Count == 0) return;

        HashSet<int> uniqueMoves = new HashSet<int>(playerManager.moveCountList);

        foreach (int moveStep in uniqueMoves)
        {
            if (moveStep == 0) continue;

            if (moveStep == -1)
            {
                // 빽도: 시작지점에 있으면 도(1)로 변환
                if (currentWayPoint.isStartEndPoint)
                {
                    // 도(1)로 변환하여 전진 목적지 계산
                    FindRoutes(currentWayPoint, 1, new List<WayPoint>(), 1);
                }
                else if (currentWayPoint.backPoint != null)
                {
                    RouteInfo ri = new RouteInfo();
                    ri.moveCount = -1;
                    ri.route.Add(currentWayPoint.backPoint);
                    targetPoints[currentWayPoint.backPoint] = ri;
                }
                continue;
            }

            FindRoutes(currentWayPoint, moveStep, new List<WayPoint>(), moveStep);
        }
    }

    void FindRoutes(WayPoint currentPoint, int remainingSteps, List<WayPoint> currentRoute, int originalMoveCount)
    {
        if (currentRoute.Count > 0 && currentRoute[currentRoute.Count - 1].isStartEndPoint)
        {
            WayPoint finalPoint = currentRoute[currentRoute.Count - 1];
            if (!targetPoints.ContainsKey(finalPoint))
            {
                RouteInfo ri = new RouteInfo();
                ri.moveCount = originalMoveCount;
                ri.route = new List<WayPoint>(currentRoute);
                targetPoints[finalPoint] = ri;
            }
            return;
        }

        if (remainingSteps == 0)
        {
            WayPoint finalPoint = currentRoute[currentRoute.Count - 1];
            if (!targetPoints.ContainsKey(finalPoint))
            {
                RouteInfo ri = new RouteInfo();
                ri.moveCount = originalMoveCount;
                ri.route = new List<WayPoint>(currentRoute);
                targetPoints[finalPoint] = ri;
            }
            return;
        }

        bool isFirstStep = (remainingSteps == originalMoveCount);

        // 지름길 분기
        if (isFirstStep && currentPoint.shortcutPoint != null)
        {
            List<WayPoint> shortcutRoute = new List<WayPoint>(currentRoute);
            shortcutRoute.Add(currentPoint.shortcutPoint);
            FindRoutes(currentPoint.shortcutPoint, remainingSteps - 1, shortcutRoute, originalMoveCount);
        }

        // 직진
        if (currentPoint.nextPoint != null)
        {
            List<WayPoint> nextRoute = new List<WayPoint>(currentRoute);
            nextRoute.Add(currentPoint.nextPoint);
            FindRoutes(currentPoint.nextPoint, remainingSteps - 1, nextRoute, originalMoveCount);
        }
    }

    /// <summary>
    /// 이동 가능한 WayPoint들을 파란색으로 하이라이트합니다.
    /// </summary>
    public void ShowHighlights()
    {
        foreach (WayPoint wp in targetPoints.Keys)
        {
            if (wp != null)
            {
                /* [REMOVED] 발판 색상 변경 기능 제거
                Renderer renderer = wp.GetComponent<Renderer>();
                if (renderer != null) renderer.material = blue;
                */

                // 화살표 프리팹 생성
                if (arrowPrefab != null)
                {
                    Vector3 spawnPos = wp.transform.position + Vector3.up * arrowHeightOffset;
                    GameObject arrow = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
                    arrow.transform.localScale = new Vector3(150f, 150f, 150f); // [NEW] 크기 조정
                    
                    // 화살표가 아래를 향하도록 X축 90도 회전 설정
                    arrow.transform.rotation = Quaternion.Euler(90f, 0, 0); 
                    
                    // 말의 색상과 맞춤 (Renderer 탐색)
                    Renderer pieceRenderer = GetComponentInChildren<Renderer>();
                    Renderer arrowRenderer = arrow.GetComponentInChildren<Renderer>();
                    if (pieceRenderer != null && arrowRenderer != null)
                    {
                        arrowRenderer.material = pieceRenderer.material;
                    }

                    // [NEW] 화살표 클릭 시 목적지 정보를 알 수 있게 링크 추가
                    ArrowLink link = arrow.AddComponent<ArrowLink>();
                    link.targetWayPoint = wp;
                    arrow.AddComponent<ArrowMove>(); // [NEW] 움직임 컴포넌트 추가
                    
                    // 레이캐스트 충돌을 위해 MeshCollider 등이 없다면 추가 (arrow.fbx 구성에 따라 필요)
                    if (arrow.GetComponent<Collider>() == null)
                    {
                        arrow.AddComponent<BoxCollider>();
                    }

                    activeArrows.Add(arrow);
                }
            }
        }
    }

    /// <summary>
    /// 하이라이트된 WayPoint들을 원래 색상으로 복원합니다.
    /// </summary>
    public void ClearHighlights()
    {
        /* [REMOVED] 발판 색상 복원 기능 제거 (더 이상 변경하지 않음)
        foreach (WayPoint wp in targetPoints.Keys)
        {
            if (wp != null) wp.RestoreOriginalMaterial();
        }
        */

        // 생성된 화살표 모두 삭제
        foreach (GameObject arrow in activeArrows)
        {
            if (arrow != null) Destroy(arrow);
        }
        activeArrows.Clear();

        targetPoints.Clear();
    }
}
