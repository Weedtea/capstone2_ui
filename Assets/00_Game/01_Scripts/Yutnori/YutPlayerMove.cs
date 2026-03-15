using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YutPlayerMove : MonoBehaviour
{
    [Header("setting")]
    public float moveSpeed = 10f;

    [Header("state")]
    public WayPoint currentWayPoint;
    public bool isMoving = false;

    public Vector3 initialPosition;
    public WayPoint initialWayPoint;

    void Start()
    {
        initialPosition = transform.position;
        initialWayPoint = currentWayPoint;
    }

    public void MoveAlongRoute(RouteInfo routeInfo)
    {
        if (isMoving) return;

        StartCoroutine(MoveRouteRoutine(routeInfo));
    }

    IEnumerator MoveRouteRoutine(RouteInfo routeInfo)
    {
        isMoving = true;
        WayPoint startPoint = currentWayPoint;
        transform.position = currentWayPoint.transform.position;
        
        if (startPoint != null)
        {
            ArrangePiecesAt(startPoint);
        }

        Yut_Player_Manager manager = GetComponent<Yut_Player_Manager>();

        foreach (WayPoint nextWayPoint in routeInfo.route)
        {
            if (nextWayPoint.isStartEndPoint && routeInfo.moveCount != -1)
            {
                yield return StartCoroutine(MoveToNextPoint(nextWayPoint.transform.position, manager));
                Debug.Log($"[YutPlayerMove] {gameObject.name} 도착점 도달!");
                currentWayPoint = nextWayPoint;
                isMoving = false;
                yield break;
            }

            yield return StartCoroutine(MoveToNextPoint(nextWayPoint.transform.position, manager));
            currentWayPoint = nextWayPoint;
        }
        isMoving = false;
    }

    IEnumerator MoveToNextPoint(Vector3 targetPos, Yut_Player_Manager manager)
    {
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

            // 업힌 말도 함께 이동
            if (manager != null)
            {
                for (int i = 0; i < manager.carriedPieces.Count; i++)
                {
                    var carried = manager.carriedPieces[i];
                    Vector3 stackOffset = Vector3.up * 0.5f * (i + 1);
                    carried.transform.position = transform.position + stackOffset;
                }
            }

            Vector3 lookTarget = new Vector3(targetPos.x, transform.position.y, targetPos.z);
            if (Vector3.Distance(transform.position, lookTarget) > 0.001f)
            {
                transform.LookAt(lookTarget);
            }
            yield return null;
        }
        transform.position = targetPos;

        // 업힌 말 최종 위치 동기화
        if (manager != null)
        {
            for (int i = 0; i < manager.carriedPieces.Count; i++)
            {
                var carried = manager.carriedPieces[i];
                Vector3 stackOffset = Vector3.up * 0.5f * (i + 1);
                carried.transform.position = targetPos + stackOffset;

                // 업힌 말의 currentWayPoint도 동기화
                var carriedMove = carried.GetComponent<YutPlayerMove>();
                if (carriedMove != null) carriedMove.currentWayPoint = currentWayPoint;
            }
        }
    }

    public void ResetToStart()
    {
        transform.position = initialPosition;
        currentWayPoint = initialWayPoint;
        isMoving = false;
        gameObject.SetActive(true);
    }

    public static void ArrangePiecesAt(WayPoint wp)
    {
        if (wp == null || wp.isStartEndPoint) return;

        YutPlayerMove[] allPieces = Object.FindObjectsByType<YutPlayerMove>(FindObjectsSortMode.None);
        List<YutPlayerMove> rootPieces = new List<YutPlayerMove>();
        List<Transform> teams = new List<Transform>();

        foreach (var p in allPieces)
        {
            if (p.currentWayPoint == wp && p.gameObject.activeInHierarchy && !p.isMoving)
            {
                var manager = p.GetComponent<Yut_Player_Manager>();
                if (manager != null && manager.carriedBy == null)
                {
                    rootPieces.Add(p);
                    if (!teams.Contains(p.transform.parent))
                    {
                        teams.Add(p.transform.parent);
                    }
                }
            }
        }

        if (teams.Count == 0) return;

        float offset = 0.5f;

        foreach (var p in rootPieces)
        {
            var manager = p.GetComponent<Yut_Player_Manager>();
            int teamIndex = teams.IndexOf(p.transform.parent);
            Vector3 newPos = wp.transform.position;

            if (teams.Count == 2)
            {
                // 옆으로 나란히
                if (teamIndex == 0) newPos += Vector3.left * offset;
                else if (teamIndex == 1) newPos += Vector3.right * offset;
            }
            else if (teams.Count == 3)
            {
                // 삼각형
                if (teamIndex == 0) newPos += new Vector3(0, 0, 1) * offset;
                else if (teamIndex == 1) newPos += new Vector3(-0.866f, 0, -0.5f) * offset;
                else if (teamIndex == 2) newPos += new Vector3(0.866f, 0, -0.5f) * offset;
            }
            else if (teams.Count == 4)
            {
                // 사각형
                if (teamIndex == 0) newPos += new Vector3(-1, 0, 1).normalized * offset;
                else if (teamIndex == 1) newPos += new Vector3(1, 0, 1).normalized * offset;
                else if (teamIndex == 2) newPos += new Vector3(-1, 0, -1).normalized * offset;
                else if (teamIndex == 3) newPos += new Vector3(1, 0, -1).normalized * offset;
            }

            p.transform.position = newPos;

            if (manager != null)
            {
                for (int i = 0; i < manager.carriedPieces.Count; i++)
                {
                    var carried = manager.carriedPieces[i];
                    carried.transform.position = newPos + Vector3.up * 0.5f * (i + 1);
                }
            }
        }
    }
}
