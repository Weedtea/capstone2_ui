using UnityEngine;

/// <summary>
/// 턴 관리 - TurnCount에 따라 플레이어 턴 상태를 업데이트
/// 최대 4인 플레이 지원
/// </summary>
public class YutGameTurn : MonoBehaviour
{
    [Header("플레이어 목록 (최대 4명)")]
    public GameObject[] players = new GameObject[4];

    [Header("게임 설정")]
    public int playerCount = 2;
    public bool gameStarted = false;

    [Header("플레이어 순서 (미니게임 결과 적용됨)")]
    public int[] playerOrder; // 순서를 저장할 배열 (0-indexed: 0, 1, 2, 3...)

    private int _turnCount = 1;
    public int TurnCount
    {
        get => _turnCount;
        set
        {
            if (_turnCount != value)
            {
                // 라운드 종료 조건 검사 (모든 플레이어가 한 번씩 조작을 마친 경우 즉, 새로운 바퀴 시작 전)
                // playerCount만큼 턴이 돌면 한 라운드가 끝난 것으로 간주. 
                // 예: 2인 플레이 시 TurnCount가 1, 2 진행 후 3이 되려고 할 때 (value - 1) % 2 == 0 성립.
                if (value > 1 && (value - 1) % playerCount == 0)
                {
                    Debug.Log($"[YutGameTurn] {(value - 1) / playerCount}라운드 종료. 미니게임 시작!");
                    
                    // 미니게임 상태로 전환 (TurnCount를 섣불리 늘리지 않음. 미니게임 끝나고 반영)
                    if (MiniGameTransitionManager.Instance != null)
                    {
                        MiniGameTransitionManager.Instance.StartMiniGame();
                    }
                    else
                    {
                        Debug.LogWarning("[YutGameTurn] MiniGameTransitionManager가 없습니다. 미니게임을 건너뜁니다.");
                        _turnCount = value;
                        UpdatePlayerTurnStates();
                    }
                }
                else
                {
                    _turnCount = value;
                    UpdatePlayerTurnStates();
                }
            }
        }
    }

    public bool isThrowedThisTurn = false;

    void Awake()
    {
        // 게임 시작 전에는 턴 업데이트 하지 않음
        if (gameStarted)
            UpdatePlayerTurnStates();
    }

    /// <summary>
    /// 게임을 시작합니다. 플레이어 수를 설정하고 비활성 플레이어를 끕니다.
    /// </summary>
    public void StartGame(int numPlayers)
    {
        playerCount = Mathf.Clamp(numPlayers, 2, 4);
        gameStarted = true;
        
        // 초기 플레이어 순서 세팅 (1,2,3... 순서대로)
        playerOrder = new int[playerCount];
        for (int i = 0; i < playerCount; i++)
        {
            playerOrder[i] = i; 
        }

        _turnCount = 1;

        // 참여하지 않는 플레이어 비활성화
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null)
            {
                players[i].SetActive(i < playerCount);
            }
        }

        Debug.Log($"★ 게임 시작! {playerCount}인 플레이 ★");
        UpdatePlayerTurnStates();

        // 게임 시작 시 첫 번째 플레이어를 위해 윷 활성화 (10초 타이머 시작)
        Yut_YutParent_Manager yutParentManager = FindAnyObjectByType<Yut_YutParent_Manager>();
        if (yutParentManager != null)
        {
            yutParentManager.ShowYuts();
        }
    }

    /// <summary>
    /// TurnCount에 따라 현재 차례인 플레이어를 결정합니다.
    /// </summary>
    public int GetCurrentPlayerIndex()
    {
        int roundTurnIndex = (_turnCount - 1) % playerCount;

        // playerOrder 배열이 제대로 초기화되었다면 해당 순서를 가져옴
        if (playerOrder != null && playerOrder.Length == playerCount)
        {
            return playerOrder[roundTurnIndex];
        }

        return roundTurnIndex; // 기본 작동 방식 백업
    }

    /// <summary>
    /// 현재 턴의 플레이어 부모 오브젝트를 반환합니다.
    /// </summary>
    public GameObject GetCurrentPlayer()
    {
        int idx = GetCurrentPlayerIndex();
        if (idx >= 0 && idx < players.Length)
            return players[idx];
        return null;
    }

    /// <summary>
    /// TurnCount에 따라 모든 플레이어 말의 턴 상태를 업데이트합니다.
    /// </summary>
    void UpdatePlayerTurnStates()
    {
        int currentIdx = GetCurrentPlayerIndex();

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;

            bool isThisPlayerTurn = (i == currentIdx);
            Yut_Player_Manager[] managers = players[i].GetComponentsInChildren<Yut_Player_Manager>(true);
            foreach (var m in managers)
            {
                m.isPlayerTurn = isThisPlayerTurn;
            }
        }

        Debug.Log($"[YutGameTurn] 턴 {_turnCount}: 플레이어{currentIdx + 1} 차례");
        
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateTurn($"Player {currentIdx + 1}");
        }
    }

    /// <summary>
    /// 미니게임이 끝나고 새로운 순서가 적용된 뒤, 다음 라운드를 시작할 때 호출됩니다.
    /// </summary>
    public void StartNextRoundWithNewOrder()
    {
        Debug.Log("[YutGameTurn] 미니게임 종료 후 새로운 라운드 턴을 시작합니다.");
        // (value-1) % playerCount == 0 에서 막혀있던 TurnCount를 여기서 넘겨줌.
        // 현재 내부 _turnCount 값은 방금 전 라운드의 마지막 차례였음.
        // 강제로 올려주고 상태 업데이트
        
        // 주의: setter 를 거치지 않고 직접 올려주고 업데이트
        _turnCount++; 
        
        // 윷 던지기 권한 리셋
        isThrowedThisTurn = false;
        
        UpdatePlayerTurnStates();
        
        // 윷 다시 보여주기
        Yut_YutParent_Manager yutParentManager = FindAnyObjectByType<Yut_YutParent_Manager>();
        if (yutParentManager != null)
        {
            yutParentManager.ShowYuts();
        }
    }
}
