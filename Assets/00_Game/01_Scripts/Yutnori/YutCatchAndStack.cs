using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이동 완료 후 업기, 상대 데미지, 발판 효과, 승리 체크를 처리
/// </summary>
[RequireComponent(typeof(Yut_Player_Manager))]
[RequireComponent(typeof(YutTurnReset))]
public class YutCatchAndStack : MonoBehaviour
{
    [SerializeField] private YutGameTurn yutGameTurn;
    private Yut_Player_Manager playerManager;
    private YutTurnReset turnReset;
    private YutPlayerMove playerMove;
    private Yut_YutParent_Manager yutParentManager;

    [Header("상대 데미지")]
    [Tooltip("상대와 같은 칸에 도착했을 때 주는 데미지")]
    public int collisionDamage = 10;

    void Awake()
    {
        playerManager = GetComponent<Yut_Player_Manager>();
        turnReset = GetComponent<YutTurnReset>();
        playerMove = GetComponent<YutPlayerMove>();
        if (yutGameTurn == null) yutGameTurn = FindAnyObjectByType<YutGameTurn>();
        yutParentManager = FindAnyObjectByType<Yut_YutParent_Manager>();
    }

    /// <summary>
    /// 이동 완료를 기다린 후 효과를 처리하는 코루틴을 시작합니다.
    /// </summary>
    public void StartPostMoveCheck()
    {
        StartCoroutine(WaitForMoveAndCheck());
    }

    IEnumerator WaitForMoveAndCheck()
    {
        // 이동 완료 대기
        while (playerMove.isMoving)
        {
            yield return null;
        }

        WayPoint destPoint = playerMove.currentWayPoint;
        if (destPoint == null) yield break;

        // 도착점 도달 시 (빽도(-1)로 온 경우는 제외)
        if (destPoint.isStartEndPoint && playerManager.currentMoveCount != -1)
        {
            // 이 말과 업힌 말 모두 완주 처리
            FinishPiece(playerManager);

            // 팀의 모든 말이 완주했는지 체크
            bool allFinished = true;
            foreach (var p in playerManager.GetTeamPieces())
            {
                if (!p.hasFinished) { allFinished = false; break; }
            }

            if (allFinished)
            {
                string playerName = transform.parent != null ? transform.parent.name : gameObject.name;
                Debug.Log($"★★★ 승리! {playerName}의 모든 말이 도착점에 도달했습니다! ★★★");
            }

            // 남은 이동이 있으면 계속 턴 진행
            if (playerManager.moveCountList.Count > 0)
            {
                yield break;
            }

            // 턴 종료
            if (yutParentManager != null) yutParentManager.ShowYuts();
            turnReset.ResetTurnState();
            yield break;
        }

        // 업기 체크: 같은 팀 말이 같은 칸에 있으면 업기
        CheckStacking(destPoint);

        // 상대 말 데미지 처리
        DamageOpponent(destPoint);

        // 발판 효과 처리
        ApplyTileEffect(destPoint);

        // 모든 이벤트 후 겹친 말 시각적 재배치
        YutPlayerMove.ArrangePiecesAt(destPoint);

        // 턴 처리
        if (playerManager.moveCountList.Count == 0)
        {
            if (yutParentManager != null) yutParentManager.ShowYuts();
            turnReset.ResetTurnState();
        }
    }

    /// <summary>
    /// 말(과 업힌 말)을 완주 처리합니다.
    /// </summary>
    void FinishPiece(Yut_Player_Manager piece)
    {
        piece.hasFinished = true;
        Debug.Log($"[완주] {piece.gameObject.name} 도착점 도달!");

        // 업힌 말들도 함께 완주
        foreach (var carried in piece.carriedPieces)
        {
            carried.hasFinished = true;
            carried.carriedBy = null;
            carried.gameObject.SetActive(false);
            Debug.Log($"[완주] {carried.gameObject.name}도 함께 도착점 도달!");
        }
        piece.carriedPieces.Clear();
        piece.gameObject.SetActive(false);
    }

    /// <summary>
    /// 같은 팀의 다른 말이 같은 칸에 있으면 업기 처리
    /// </summary>
    void CheckStacking(WayPoint destPoint)
    {
        var teamPieces = playerManager.GetTeamPieces();
        foreach (var p in teamPieces)
        {
            if (p == playerManager) continue;
            if (p.hasFinished || !p.gameObject.activeInHierarchy) continue;
            if (p.carriedBy != null) continue;

            var otherMove = p.GetComponent<YutPlayerMove>();
            if (otherMove != null && otherMove.currentWayPoint == destPoint)
            {
                // 업기! 이 말이 상대를 업음
                playerManager.carriedPieces.Add(p);
                p.carriedBy = playerManager;

                // 업힌 말이 기존에 업고 있던 말들도 이전
                foreach (var carried in p.carriedPieces)
                {
                    playerManager.carriedPieces.Add(carried);
                    carried.carriedBy = playerManager;
                }
                p.carriedPieces.Clear();

                Debug.Log($"[업기] {gameObject.name}이(가) {p.gameObject.name}을(를) 업었습니다!");
            }
        }
    }

    /// <summary>
    /// 같은 칸에 있는 상대 말에게 데미지를 줍니다.
    /// </summary>
    void DamageOpponent(WayPoint destPoint)
    {
        Transform myParent = transform.parent;
        YutPlayerMove[] allPieces = Object.FindObjectsByType<YutPlayerMove>(FindObjectsSortMode.None);

        foreach (var piece in allPieces)
        {
            if (piece.transform.parent != myParent && piece.gameObject.activeInHierarchy)
            {
                if (piece.currentWayPoint == destPoint)
                {
                    Yut_Player_Manager opponentManager = piece.GetComponent<Yut_Player_Manager>();
                    if (opponentManager != null)
                    {
                        Debug.Log($"[전투] {gameObject.name}이(가) {piece.gameObject.name}에게 데미지 {collisionDamage}!");
                        // 상대 측 업힌 말들도 모두 데미지
                        opponentManager.TakeDamage(collisionDamage);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 도착한 칸의 발판 효과를 적용합니다.
    /// </summary>
    void ApplyTileEffect(WayPoint destPoint)
    {
        if (destPoint.tileType == TileType.Heal)
        {
            playerManager.Heal(destPoint.tileEffectAmount);
            Debug.Log($"[발판] {gameObject.name} 힐 발판! +{destPoint.tileEffectAmount} HP");
        }
        else if (destPoint.tileType == TileType.Damage)
        {
            playerManager.TakeDamage(destPoint.tileEffectAmount);
            Debug.Log($"[발판] {gameObject.name} 데미지 발판! -{destPoint.tileEffectAmount} HP");
        }
    }
}
