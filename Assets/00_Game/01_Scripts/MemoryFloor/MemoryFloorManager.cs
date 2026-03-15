using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemoryFloorManager : MonoBehaviour
{
    public static MemoryFloorManager Instance { get; private set; }

    [Header("게임 설정")]
    public int maxRounds = 3;
    public float patternShowTime = 3f;
    public float moveTime = 10f;
    public float dropTime = 2f;
    public float roundDelay = 2f;

    [Header("현재 상태")]
    public int currentRound = 0;
    public bool isGameActive = false;
    public GameState currentState = GameState.Waiting;

    public enum GameState
    {
        Waiting,
        PatternShow,
        MovePhase,
        RoundEnd,
        GameOver
    }

    private MemoryFloorGrid gridManager;
    private List<MemoryFloorPlayer> activePlayers = new List<MemoryFloorPlayer>();
    private List<MemoryFloorPlayer> finishedPlayers = new List<MemoryFloorPlayer>();
    private List<MemoryFloorPlayer> allRegisteredPlayers = new List<MemoryFloorPlayer>();

    [Header("플레이어 스폰 설정")]
    public GameObject playerPrefab;
    public Vector3[] baseSpawnOffsets = new Vector3[] {
        new Vector3(-3, 2, 0), new Vector3(3, 2, 0),
        new Vector3(-1, 2, 0), new Vector3(1, 2, 0)
    };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        gridManager = FindFirstObjectByType<MemoryFloorGrid>();
        SpawnPlayers();
        // 임시 로직: 시작 직후 일정 시간 뒤 게임 시작
        Invoke(nameof(StartGame), 2f);
    }

    private void SpawnPlayers()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("playerPrefab is null in MemoryFloorManager!");
            return;
        }

        KeyCode[][] keys = new KeyCode[][] {
            new KeyCode[] { KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D },
            new KeyCode[] { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow },
            new KeyCode[] { KeyCode.I, KeyCode.K, KeyCode.J, KeyCode.L },
            new KeyCode[] { KeyCode.T, KeyCode.G, KeyCode.F, KeyCode.H }
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject pObj = Instantiate(playerPrefab, baseSpawnOffsets[i], Quaternion.identity);
            pObj.name = "Player_" + (i + 1);
            MemoryFloorPlayer pScript = pObj.GetComponent<MemoryFloorPlayer>();
            if (pScript != null)
            {
                pScript.upKey = keys[i][0];
                pScript.downKey = keys[i][1];
                pScript.leftKey = keys[i][2];
                pScript.rightKey = keys[i][3];
            }
        }
    }
    
    public void RegisterPlayer(MemoryFloorPlayer player)
    {
        if (!activePlayers.Contains(player))
        {
            activePlayers.Add(player);
            allRegisteredPlayers.Add(player);
        }
    }

    public void OnPlayerFallen(MemoryFloorPlayer player)
    {
        if (activePlayers.Contains(player))
        {
            activePlayers.Remove(player);
            player.gameObject.SetActive(false);
            CheckWinConditionEarly();
        }
    }

    public void OnPlayerFinished(MemoryFloorPlayer player)
    {
        if (!finishedPlayers.Contains(player))
        {
            finishedPlayers.Add(player);
        }
    }

    public void StartGame()
    {
        isGameActive = true;
        currentRound = 0;
        StartNextRound();
    }

    private void StartNextRound()
    {
        if (activePlayers.Count <= 1)
        {
            EndGame();
            return;
        }

        currentRound++;
        if (currentRound > maxRounds)
        {
            EndGame();
            return;
        }

        Debug.Log($"--- 라운드 {currentRound} 시작 ---");
        StartCoroutine(RoundRoutine());
    }

    private IEnumerator RoundRoutine()
    {
        currentState = GameState.Waiting;
        finishedPlayers.Clear();

        // 1. 그리드 생성 및 경로 확보
        gridManager.GenerateGrid(currentRound);
        gridManager.SelectSafePath();

        // 2. 플레이어를 시작 지점으로 텔레포트
        Vector3 startZonePos = gridManager.GetStartZonePosition();
        for (int i = 0; i < activePlayers.Count; i++)
        {
            MemoryFloorPlayer p = activePlayers[i];
            p.gameObject.SetActive(true); // 결승선 도달 시 숨겨졌던 캐릭터 다시 표시
            p.isFinishedRound = false;
            p.transform.position = startZonePos + baseSpawnOffsets[i % baseSpawnOffsets.Length];
            if (p.TryGetComponent(out Rigidbody rb))
            {
                rb.linearVelocity = Vector3.zero;
            }
        }

        yield return new WaitForSeconds(1f);

        // 3. 패턴 제시 (정답 발광)
        currentState = GameState.PatternShow;
        Debug.Log("정답 확인 시간!");
        gridManager.ShowSafeTiles(true);
        yield return new WaitForSeconds(patternShowTime);
        gridManager.ShowSafeTiles(false);

        // 4. 이동 페이즈
        currentState = GameState.MovePhase;
        Debug.Log($"이동 시작! ({moveTime}초)");
        
        float timer = moveTime;
        while (timer > 0)
        {
            if (activePlayers.Count > 0 && finishedPlayers.Count >= activePlayers.Count)
            {
                Debug.Log("모든 생존자가 결승선에 도달했습니다!");
                break;
            }
            timer -= Time.deltaTime;
            yield return null;
        }

        // 5. 시간 종료 후, 도달하지 못한 플레이어 강제 탈락
        for (int i = activePlayers.Count - 1; i >= 0; i--)
        {
            if (!finishedPlayers.Contains(activePlayers[i]))
            {
                activePlayers[i].EliminatePlayer();
            }
        }

        // 라운드 종료
        currentState = GameState.RoundEnd;
        CheckWinConditionRoundEnd();

        if (isGameActive)
        {
            yield return new WaitForSeconds(roundDelay);
            StartNextRound();
        }
    }

    private void CheckWinConditionEarly()
    {
        if (!isGameActive) return;
        
        if (activePlayers.Count <= 1 && currentState == GameState.MovePhase)
        {
            // 이동 페이즈 도중 즉시 판정을 수행하지 않는 것은, 시간 종료 로직에 맡기기 위함.
        }
    }

    private void CheckWinConditionRoundEnd()
    {
        if (!isGameActive) return;

        if (activePlayers.Count == 0)
        {
            Debug.Log("모두 탈락! 최후의 승자 판정");
            FindWinnerByProgress();
        }
        else if (activePlayers.Count == 1)
        {
            Debug.Log($"우승자 발생! {activePlayers[0].name}");
            EndGame();
        }
        else if (currentRound >= maxRounds)
        {
            Debug.Log("최대 라운드 도달! 생존자 다수 (공동 우승)");
            EndGame();
        }
    }

    private void FindWinnerByProgress()
    {
        MemoryFloorPlayer bestPlayer = null;
        int bestRound = -1;
        float bestZ = -9999f;

        foreach (var p in allRegisteredPlayers)
        {
            if (p == null) continue;
            
            if (p.highestRound > bestRound)
            {
                bestRound = p.highestRound;
                bestZ = p.maxZPosition;
                bestPlayer = p;
            }
            else if (p.highestRound == bestRound)
            {
                if (p.maxZPosition > bestZ)
                {
                    bestZ = p.maxZPosition;
                    bestPlayer = p;
                }
            }
        }

        if (bestPlayer != null)
        {
            Debug.Log($"우승자: {bestPlayer.name} (최고 도달 라운드: {bestRound}, 최대 Z: {bestZ})");
        }
        EndGame();
    }

    private void EndGame()
    {
        isGameActive = false;
        currentState = GameState.GameOver;
        Debug.Log("게임 종료!");
    }
}
