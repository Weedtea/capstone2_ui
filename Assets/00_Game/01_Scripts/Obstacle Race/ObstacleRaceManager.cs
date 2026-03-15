using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro; // 진행도 UI용

public class ObstacleRaceManager : MonoBehaviour
{
    public static ObstacleRaceManager Instance;

    [Header("플레이어 설정")]
    public List<PlayerController> players = new List<PlayerController>();
    public PlayerController localPlayer; // 내 화면에 진행도를 표시할 기준 플레이어
    
    [Header("UI 및 진행도 설정")]
    public TextMeshProUGUI progressText; // 우측 상단 X% 텍스트
    public Transform finishLineTransform;
    private float startX; // 게임 시작 지점의 X 좌표

    [Header("게임 상태")]
    public bool isGameOver = false;

    // 동시 탈락 판정용 변수 (짧은 시간 안에 죽었는지 체크)
    private PlayerController firstDeadPlayer;
    private float deadTimer = 0f;
    private bool checkingDoubleDeath = false;
    private const float doubleDeathThreshold = 0.5f; // 0.5초 안에 둘 다 죽으면 무승부 로직 가동

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // 씬 내의 모든 PlayerController 수집
        PlayerController[] foundPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        players.AddRange(foundPlayers);

        // 로컬 플레이어 자동 할당 (할당 안 된 경우)
        if (localPlayer == null && players.Count > 0)
        {
            // 멀티플레이어 환경이므로 자신의 플레이어를 찾는 로직으로 대체할 수 있습니다.
            // 일단은 첫 번째 플레이어를 기준으로 삼습니다.
            localPlayer = players[0];
        }

        // 시작 위치 기록
        if (localPlayer != null)
        {
            startX = localPlayer.transform.position.x;
        }

        // 목적지 자동 탐색 (할당 안 된 경우)
        if (finishLineTransform == null)
        {
            FinishLine fl = Object.FindFirstObjectByType<FinishLine>();
            if (fl != null) finishLineTransform = fl.transform;
        }

        // UI 텍스트 자동 탐색
        if (progressText == null)
        {
            progressText = Object.FindFirstObjectByType<TextMeshProUGUI>();
        }
    }

    void Update()
    {
        if (isGameOver) return;

        UpdateProgressUI();

        // 동시 탈락 대기 타이머
        if (checkingDoubleDeath)
        {
            deadTimer += Time.deltaTime;
            
            if (deadTimer >= doubleDeathThreshold)
            {
                // 시간 초과 시 먼저 죽은 사람이 확실히 진 것으로 판정
                FinishRoundSingleDeath();
            }
        }
    }

    private void UpdateProgressUI()
    {
        if (progressText != null && localPlayer != null && finishLineTransform != null)
        {
            float currentX = localPlayer.transform.position.x;
            float totalDist = finishLineTransform.position.x - startX;
            float currentDist = currentX - startX;

            // X 퍼센트 계산 (0 ~ 100%)
            float progress = Mathf.Clamp01(currentDist / totalDist) * 100f;
            progressText.text = $"{Mathf.FloorToInt(progress)}%";
        }
    }

    public void OnPlayerDeath(PlayerController deadPlayer)
    {
        if (isGameOver) return;

        if (!checkingDoubleDeath)
        {
            // 한 명이 처음으로 떨어졌을 때
            firstDeadPlayer = deadPlayer;
            checkingDoubleDeath = true;
            deadTimer = 0f;
        }
        else
        {
            // 이미 한 명이 죽었는데, 또 한 명이 짧은 시간 안에 떨어진 경우 = 동시 탈락!
            checkingDoubleDeath = false;
            ResolveDrawByDistance(deadPlayer);
        }
    }

    private void FinishRoundSingleDeath()
    {
        checkingDoubleDeath = false;
        isGameOver = true;

        if (firstDeadPlayer == null) return;

        // 먼저 떨어진 사람을 제외한 남은 플레이어가 우승자
        PlayerController winner = null;
        foreach (var p in players)
        {
            if (p != firstDeadPlayer)
            {
                winner = p;
                break;
            }
        }

        if (winner != null)
        {
            Debug.Log($"[ObstacleRaceManager] {firstDeadPlayer.gameObject.name} 추락 탈락! 승자: {winner.gameObject.name}");
        }
    }

    private void ResolveDrawByDistance(PlayerController secondDeadPlayer)
    {
        isGameOver = true;
        
        // 두 탈락자의 X 보존 좌표(죽은 시점) 비교
        float x1 = firstDeadPlayer.transform.position.x;
        float x2 = secondDeadPlayer.transform.position.x;

        PlayerController winner = null;
        if (x1 > x2) winner = firstDeadPlayer;
        else if (x2 > x1) winner = secondDeadPlayer;

        if (winner != null)
        {
            Debug.Log($"[ObstacleRaceManager] 동시 추락 발생! 더 결승선에 가까웠던 {winner.gameObject.name} 우승 판정!");
        }
        else
        {
            Debug.Log("[ObstacleRaceManager] 완전히 동일한 위치에서 추락. 완전 무승부!");
        }
    }

    public void OnPlayerFinish(PlayerController winner)
    {
        if (isGameOver) return;

        isGameOver = true;
        
        Debug.Log($"[ObstacleRaceManager] 게임 종료! 결승선에 도착한 승자: {winner.gameObject.name}");
        
        // 다른 플레이어 조작 차단 등을 추후에 추가 가능
        foreach(var p in players)
        {
            p.isStunned = true; // 이동 불가 처리
        }
    }
}
