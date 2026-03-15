using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ColorTilesManager : MonoBehaviour
{
    public static ColorTilesManager Instance { get; private set; }

    [Header("게임 설정")]
    public float gameDuration = 30f;
    public float preGameDelay = 3f;

    [Header("현재 상태")]
    public float remainingTime;
    public GameState currentState = GameState.Waiting;

    public enum GameState
    {
        Waiting,
        Playing,
        TimeOut,
        ScorePhase
    }

    private ColorTilesGrid gridManager;
    private List<ColorTilesPlayer> activePlayers = new List<ColorTilesPlayer>();

    [Header("플레이어 스폰 설정")]
    public GameObject playerPrefab;
    public Color[] playerColors = new Color[]
    {
        new Color(0.95f, 0.25f, 0.25f),  // Player1 빨강
        new Color(0.25f, 0.55f, 0.95f),  // Player2 파랑
        new Color(0.98f, 0.85f, 0.15f),  // Player3 노랑
        new Color(0.25f, 0.80f, 0.35f)   // Player4 초록
    };

    [Header("UI 연결")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI[] scoreTexts;   // 4개 (Player 1~4)
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI countdownText;

    [Header("플레이어 이름")]
    public string[] playerNames = { "P1", "P2", "P3", "P4" };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        gridManager = FindFirstObjectByType<ColorTilesGrid>();
        remainingTime = gameDuration;

        if (resultPanel != null) resultPanel.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(true);

        if (gridManager != null)
        {
            gridManager.GenerateGrid();
            SpawnPlayers();
            StartCoroutine(GameRoutine());
        }
    }

    private void SpawnPlayers()
    {
        if (playerPrefab == null) return;

        KeyCode[][] keys = new KeyCode[][]
        {
            new KeyCode[] { KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.Space },
            new KeyCode[] { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.RightControl },
            new KeyCode[] { KeyCode.I, KeyCode.K, KeyCode.J, KeyCode.L, KeyCode.O },
            new KeyCode[] { KeyCode.T, KeyCode.G, KeyCode.F, KeyCode.H, KeyCode.Y }
        };

        Vector3[] spawnPoints = gridManager.GetSpawnPoints();

        for (int i = 0; i < 4; i++)
        {
            Vector3 spawnPos = i < spawnPoints.Length ? spawnPoints[i] : Vector3.zero;
            GameObject pObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            pObj.name = $"Player_{i + 1}";

            ColorTilesPlayer pScript = pObj.GetComponent<ColorTilesPlayer>();
            if (pScript != null)
            {
                pScript.Initialize(i, playerColors[i], keys[i]);
            }
        }
    }

    public void RegisterPlayer(ColorTilesPlayer player)
    {
        if (!activePlayers.Contains(player))
            activePlayers.Add(player);
    }

    private void Update()
    {
        if (currentState == GameState.Playing)
        {
            UpdateTimerUI();
            UpdateScoreUI();
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int sec = Mathf.CeilToInt(remainingTime);
            timerText.text = sec.ToString();
            timerText.color = remainingTime <= 10f ? Color.red : Color.white;
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreTexts == null || gridManager == null) return;

        int[] scores = GetCurrentScores();
        for (int i = 0; i < scoreTexts.Length; i++)
        {
            if (scoreTexts[i] != null)
                scoreTexts[i].text = $"{playerNames[i]}: {scores[i]}";
        }
    }

    private int[] GetCurrentScores()
    {
        int[] scores = new int[4];
        if (gridManager == null) return scores;
        foreach (var tile in gridManager.GetAllTiles())
        {
            if (tile.OwnerID >= 0 && tile.OwnerID < 4)
                scores[tile.OwnerID]++;
        }
        return scores;
    }

    private IEnumerator GameRoutine()
    {
        // 카운트다운
        currentState = GameState.Waiting;
        for (int i = (int)preGameDelay; i > 0; i--)
        {
            if (countdownText != null)
                countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
        if (countdownText != null)
        {
            countdownText.text = "START!";
            yield return new WaitForSeconds(0.7f);
            // 텍스트와 부모 배경 패널 모두 숨김
            GameObject cdPanel = countdownText.transform.parent != null
                ? countdownText.transform.parent.gameObject
                : countdownText.gameObject;
            cdPanel.SetActive(false);
        }

        currentState = GameState.Playing;
        Debug.Log("게임 시작! 30초 진행");

        while (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            yield return null;
        }

        remainingTime = 0;
        currentState = GameState.TimeOut;
        Debug.Log("타임 오버! 조작 정지");

        if (timerText != null) timerText.text = "0";

        yield return new WaitForSeconds(1.5f);

        currentState = GameState.ScorePhase;
        CalculateScores();
    }

    private void CalculateScores()
    {
        int[] scores = GetCurrentScores();

        int maxScore = -1;
        int winnerID = -1;
        bool tie = false;

        for (int i = 0; i < 4; i++)
        {
            Debug.Log($"플레이어 {i + 1}의 타일 수: {scores[i]}");
            if (scores[i] > maxScore)
            {
                maxScore = scores[i];
                winnerID = i;
                tie = false;
            }
            else if (scores[i] == maxScore)
            {
                tie = true;
            }
        }

        // 최종 점수 UI 업데이트
        if (scoreTexts != null)
        {
            for (int i = 0; i < scoreTexts.Length; i++)
            {
                if (scoreTexts[i] != null)
                    scoreTexts[i].text = $"{playerNames[i]}: {scores[i]}";
            }
        }

        // 결과 패널 표시
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null)
        {
            if (tie)
                resultText.text = $"DRAW!\nTiles: {maxScore}";
            else
                resultText.text = $"{playerNames[winnerID]} WIN!\nTiles: {maxScore}";
        }

        if (tie)
            Debug.Log($"공동 우승! 타일 수: {maxScore}");
        else
            Debug.Log($"우승자: 플레이어 {winnerID + 1} (타일 수: {maxScore})");
    }
}
