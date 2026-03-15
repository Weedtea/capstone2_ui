using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 윷놀이 씬과 미니게임 씬 사이의 전환을 관리합니다.
/// </summary>
public class MiniGameTransitionManager : MonoBehaviour
{
    public static MiniGameTransitionManager Instance { get; private set; }

    [Header("비활성화할 윷놀이 씬 오브젝트 (Camera, UI 등)")]
    public List<GameObject> objectsToDisable = new List<GameObject>();

    [Header("미니게임 씬 이름 목록")]
    public List<string> miniGameSceneNames = new List<string> { "miniGameSceneTest" };

    private YutGameTurn yutGameTurn;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 이 매니저는 윷놀이 씬에 종속되므로 생략
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        yutGameTurn = FindAnyObjectByType<YutGameTurn>();
    }

    /// <summary>
    /// 미니게임을 무작위로 하나 선택하여 로딩을 시작합니다.
    /// </summary>
    public void StartMiniGame()
    {
        Debug.Log("[MiniGameTransitionManager] 무작위 미니게임을 시작합니다!");

        // 윷놀이 씬 오브젝트 비활성화 (보이지 않게)
        foreach (var obj in objectsToDisable)
        {
            if (obj != null) obj.SetActive(false);
        }

        // 미니게임 무작위 선택
        string selectedScene = miniGameSceneNames[0];
        if (miniGameSceneNames.Count > 1)
        {
            selectedScene = miniGameSceneNames[Random.Range(0, miniGameSceneNames.Count)];
        }

        // Additive 방식으로 미니게임 로드
        SceneManager.LoadScene(selectedScene, LoadSceneMode.Additive);
    }

    /// <summary>
    /// 미니게임이 끝나 결과를 반환합니다. 윷놀이 씬을 복구하고 순서를 적용합니다.
    /// </summary>
    /// <param name="newOrder">결정된 다음 턴 순서 배열 (예: 4인 기준 {3, 2, 1, 0})</param>
    /// <param name="miniGameSceneName">언로드할 미니게임 씬 이름</param>
    public void EndMiniGame(int[] newOrder, string miniGameSceneName)
    {
        Debug.Log($"[MiniGameTransitionManager] 미니게임 종료. 새 순서 적용 중... ({string.Join(", ", newOrder)})");
        
        // 순서 변경 적용
        if (yutGameTurn != null)
        {
            yutGameTurn.playerOrder = newOrder;
        }

        // 미니게임 씬 언로드
        SceneManager.UnloadSceneAsync(miniGameSceneName).completed += (AsyncOperation op) =>
        {
            // 윷놀이 씬 오브젝트 재활성화
            foreach (var obj in objectsToDisable)
            {
                if (obj != null) obj.SetActive(true);
            }

            // 첫 번째 순서 플레이어의 턴을 시작하기 위해 상태 강제 업데이트
            if (yutGameTurn != null)
            {
                // 라운드가 종료된 직후라 턴을 리셋할 필요가 있을 수 있음.
                yutGameTurn.StartNextRoundWithNewOrder();
            }
        };
    }
}
