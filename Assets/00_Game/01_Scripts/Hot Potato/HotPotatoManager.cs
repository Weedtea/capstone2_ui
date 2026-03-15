using UnityEngine;
using TMPro; // TextMeshPro 사용

public class HotPotatoManager : MonoBehaviour
{
    public static HotPotatoManager Instance;

    [Header("게임 설정")]
    public float totalGameTime = 15f; // 초기 폭탄 카운트다운 시간
    public bool isGameOver = false;

    [Header("컴포넌트 연결")]
    public BombComponent bombObject;
    public TextMeshProUGUI timerText; // 화면에 남은 시간을 표시할 UI

    // 시작 시 모든 플레이어 수집
    private HotPotatoPlayerController[] allPlayers;
    public bool isRouletteRunning = false; // 룰렛 도중엔 플레이어 간 폭탄 넘기기 방지용

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        allPlayers = FindObjectsByType<HotPotatoPlayerController>(FindObjectsSortMode.None);
        
        // 폭탄 오브젝트 자동 수집
        if (bombObject == null)
        {
            bombObject = FindFirstObjectByType<BombComponent>();
        }

        // 타이머 UI 오브젝트 자동 수집
        if (timerText == null)
        {
            timerText = Object.FindFirstObjectByType<TextMeshProUGUI>();
        }

        // 코루틴으로 변경된 초기 게임 시작 함수 지연 호출
        StartCoroutine(DelayedStart());
    }

    System.Collections.IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(AssignInitialBomb());
    }

    void Update()
    {
        if (isGameOver || isRouletteRunning) return;

        // 카운트다운 타이머 감소
        totalGameTime -= Time.deltaTime;

        // 제한 시간이 0초가 되면
        if (totalGameTime <= 0f)
        {
            TriggerExplosion();
        }
    }

    System.Collections.IEnumerator AssignInitialBomb()
    {
        if (allPlayers.Length > 0 && bombObject != null)
        {
            // 게임 시작 전 모든 플레이어 이동 가능 상태로 활성화
            foreach (var p in allPlayers)
            {
                p.EnableInput(true);
            }

            // 처음 시작 룰렛 (List 변환)
            System.Collections.Generic.List<HotPotatoPlayerController> alivePlayers = new System.Collections.Generic.List<HotPotatoPlayerController>(allPlayers);
            if (timerText != null) timerText.text = "CHOOSING TARGET...";
            
            yield return StartCoroutine(BombRoulette(alivePlayers));

            // 실제 게임 타이머 카운트다운 시작
            totalGameTime = 15f; 
        }
    }

    void TriggerExplosion()
    {
        isGameOver = true; // Update에서 여러 번 호출되는 것(다 같이 죽는 현상) 방지
        
        if (bombObject != null && bombObject.currentOwner != null)
        {
            Transform loserInfo = bombObject.currentOwner;
            HotPotatoPlayerController loser = loserInfo.GetComponent<HotPotatoPlayerController>();
            
            // 폭발 연출
            bombObject.Explode();
            
            // 탈락한 플레이어 조작 불가 및 숨김
            if (loser != null)
            {
                loser.EnableInput(false);
                StartCoroutine(HideLoser(loserInfo.gameObject));
            }

            // 남은 플레이어 체킹 로직
            StartCoroutine(CheckRoundAndRestart());
        }
    }

    System.Collections.IEnumerator HideLoser(GameObject loserObj)
    {
        yield return new WaitForSeconds(1.5f);
        loserObj.SetActive(false); // 탈락자 아예 안보이게 처리
    }

    System.Collections.IEnumerator CheckRoundAndRestart()
    {
        // 잠시 지연 (바로 Active 처리를 기다림)
        yield return new WaitForSeconds(0.1f);
        
        // 생존 플레이어 리스트 갱신 (비활성화되지 않은 PlayerController)
        System.Collections.Generic.List<HotPotatoPlayerController> alivePlayers = new System.Collections.Generic.List<HotPotatoPlayerController>();
        foreach (var p in allPlayers)
        {
            // 방금 죽은 사람(currentOwner)은 제외
            if (p.gameObject.activeSelf && (bombObject.currentOwner == null || p.gameObject != bombObject.currentOwner.gameObject))
            {
                alivePlayers.Add(p);
            }
        }

        // 최후의 1인
        if (alivePlayers.Count <= 1)
        {
            isGameOver = true;
            string winnerName = alivePlayers.Count == 1 ? alivePlayers[0].gameObject.name : "무승부(Draw)";
            
            if (timerText != null) timerText.text = "WINNER: " + winnerName;
            
            // 사용자 확인용 명확한 디버그 로그
            Debug.Log($"[Hot Potato] 게임 종료! 최종 우승자: {winnerName}");
            yield break;
        }

        // 라운드 속행 대기 전에 야바위 룰렛 시작
        if (timerText != null) timerText.text = "CHOOSING TARGET...";
        yield return new WaitForSeconds(1f); // 잠깐 정지

        // 폭탄 룰렛 (야바위 연출) 
        yield return StartCoroutine(BombRoulette(alivePlayers));

        // 룰렛 종료 후 실제 시간 카운트다운 재시작 (기본 10초)
        totalGameTime = 10f; 
        isGameOver = false; 
    }

    // 0.1초~0.3초마다 대상을 바꿔가며 보여주는 야바위 연출 코루틴
    System.Collections.IEnumerator BombRoulette(System.Collections.Generic.List<HotPotatoPlayerController> alivePlayers)
    {
        isRouletteRunning = true; // 룰렛 중 폭탄 넘기기 방지 처리
        bombObject.ResetBomb();
        
        int rouletteBlinks = Random.Range(8, 12); // 깜빡이는 횟수 조정
        float currentBlinkDelay = 0.15f; // 육안으로 추적 가능하도록 초기 딜레이 상향
        
        int lastSelected = -1;

        for (int i = 0; i < rouletteBlinks; i++)
        {
            // 중복되지 않게 무작위 선택
            int nextIdx = Random.Range(0, alivePlayers.Count);
            if(alivePlayers.Count > 1 && nextIdx == lastSelected) 
            {
                nextIdx = (nextIdx + 1) % alivePlayers.Count;
            }

            // 폭탄 강제 이동 
            bombObject.AssignToPlayer(alivePlayers[nextIdx].transform);
            bombObject.SnapToOwner();
            lastSelected = nextIdx;

            // 점점 느려지는 연출 (눈에 보이게 증가폭 부여)
            yield return new WaitForSeconds(currentBlinkDelay);
            currentBlinkDelay += 0.05f; 
        }

        // 룰렛 종료, 최종 타겟 확정
        isRouletteRunning = false;
        if (timerText != null) timerText.text = ""; // 타이머를 보여주지 않고 빈 화면 유지
    }
}