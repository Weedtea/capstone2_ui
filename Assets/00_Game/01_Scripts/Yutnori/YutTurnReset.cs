using UnityEngine;

/// <summary>
/// 턴 초기화 로직
/// 팀의 모든 말 상태를 리셋하고 다음 턴으로 넘깁니다.
/// </summary>
public class YutTurnReset : MonoBehaviour
{
    [SerializeField] private YutGameTurn yutGameTurn;

    void Awake()
    {
        if (yutGameTurn == null) yutGameTurn = FindAnyObjectByType<YutGameTurn>();
    }

    /// <summary>
    /// 현재 턴을 리셋하고 다음 턴으로 넘깁니다.
    /// 같은 팀의 모든 말 상태를 리셋합니다.
    /// </summary>
    public void ResetTurnState()
    {
        Yut_Player_Manager manager = GetComponent<Yut_Player_Manager>();
        if (manager == null) return;

        // 팀 전체 리셋
        var teamPieces = manager.GetTeamPieces();
        foreach (var p in teamPieces)
        {
            p.isPlayerTurn = false;
            p.isThrowed = false;
            p.currentMoveCount = 0;
            p.isSelected = false;

            // 각 말의 이동 가능 구역 하이라이트 제거
            YutWayPointColorChange wpc = p.GetComponent<YutWayPointColorChange>();
            if (wpc != null)
            {
                wpc.ClearHighlights();
            }
        }

        // moveCountList 클리어 (공유 레퍼런스이므로 한 번만 클리어)
        manager.moveCountList.Clear();

        yutGameTurn.TurnCount++;
        yutGameTurn.isThrowedThisTurn = false;
    }
}
