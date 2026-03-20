using UnityEngine;

/// <summary>
/// 윷 결과에 따른 게임 상태 변경 처리
/// moveCountList 추가, 추가 던지기, 낙 처리, 이동 가능 상태 전환
/// 최대 4인 플레이 지원
/// </summary>
public class YutResultHandler : MonoBehaviour
{
    [SerializeField] private YutGameTurn yutGameTurn;

    void Awake()
    {
        if (yutGameTurn == null) yutGameTurn = FindAnyObjectByType<YutGameTurn>();
    }

    /// <summary>
    /// 현재 턴의 첫 번째 플레이어 말 매니저를 가져옵니다.
    /// </summary>
    Yut_Player_Manager GetCurrentPlayerManager()
    {
        GameObject currentPlayer = yutGameTurn.GetCurrentPlayer();
        if (currentPlayer == null) return null;

        Yut_Player_Manager[] managers = currentPlayer.GetComponentsInChildren<Yut_Player_Manager>(true);
        if (managers.Length > 0) return managers[0];
        return null;
    }

    /// <summary>
    /// 윷 결과를 처리합니다.
    /// </summary>
    public void HandleResult(int result)
    {
        Debug.Log($"[YutResultHandler] HandleResult 호출됨! result={result}, TurnCount={yutGameTurn.TurnCount}");

        Yut_Player_Manager manager = GetCurrentPlayerManager();
        if (manager == null)
        {
            Debug.LogError("[YutResultHandler] 현재 턴 플레이어의 매니저를 찾을 수 없습니다!");
            return;
        }

        // 빽도 처리: 모든 활성 말이 시작지점에 있으면 빽도 → 도(1)로 변환
        if (result == -1)
        {
            var activePieces = manager.GetActivePieces();
            bool allAtStart = true;
            foreach (var p in activePieces)
            {
                YutPlayerMove pm = p.GetComponent<YutPlayerMove>();
                if (pm != null && pm.currentWayPoint != null && !pm.currentWayPoint.isStartEndPoint)
                {
                    allAtStart = false;
                    break;
                }
            }
            if (allAtStart)
            {
                Debug.Log("[YutResultHandler] 모든 말이 시작지점 → 빽도를 도(1)로 변환!");
                result = 1;
            }
        }

        // moveCountList에 결과 추가 (공유 레퍼런스이므로 한 번만 추가)
        if (result != 0)
        {
            manager.moveCountList.Add(result);
        }

        // 윷(4), 모(5)이면 추가 던지기 허용
        if (YutResultCalculator.IsExtraThrow(result))
        {
            if (yutGameTurn != null)
                yutGameTurn.isThrowedThisTurn = false;
        }
        // 낙인 경우
        else if (result == 0)
        {
            if (manager.moveCountList.Count > 0)
            {
                Debug.Log($"낙이지만 이전 결과({manager.moveCountList.Count}개)가 남아있어 이동할 수 있습니다!");
                manager.SetTeamIsThrowed(true);
                if (yutGameTurn != null)
                    yutGameTurn.isThrowedThisTurn = true;
            }
            else
            {
                Debug.Log("낙입니다! 턴이 넘어갑니다.");
                manager.SetTeamIsThrowed(false);
                foreach (var p in manager.GetTeamPieces()) p.isSelected = false;

                Yut_YutParent_Manager yutParentManager = FindAnyObjectByType<Yut_YutParent_Manager>();
                if (yutParentManager != null) yutParentManager.ShowYuts();

                if (yutGameTurn != null)
                {
                    yutGameTurn.TurnCount++;
                    yutGameTurn.isThrowedThisTurn = false;
                }
            }
        }
        // 도/개/걸/빽도 → 이동 가능 상태로 전환
        else
        {
            int playerIdx = yutGameTurn.GetCurrentPlayerIndex() + 1;
            Debug.Log($"[YutResultHandler] 이동 가능 상태로 전환! 플레이어{playerIdx}의 isThrowed=true 설정");
            manager.SetTeamIsThrowed(true);
        }

        // HUD Result display
        if (HUDManager.Instance != null)
        {
            string resultName = YutResultCalculator.GetResultName(result);
            HUDManager.Instance.ShowResult(resultName);
        }
    }
}
