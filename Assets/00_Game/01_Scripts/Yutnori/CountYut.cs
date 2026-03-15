using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 윷 던지기 후 결과 판정 및 윷 리셋을 담당
/// 게임 시작 전 인원 선택 기능 포함 (키보드 2,3,4)
/// </summary>
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(YutResultHandler))]
public class CountYut : MonoBehaviour
{
    public YutFrontBack[] yuts;

    private Vector3[] initialYutPositions;
    private Quaternion[] initialYutRotations;

    private YutResultHandler resultHandler;
    private Yut_YutParent_Manager yutParentManager;
    public GameObject yutGround;

    private YutGameTurn yutGameTurn;

    void Awake()
    {
        yuts = GetComponentsInChildren<YutFrontBack>();
        initialYutPositions = new Vector3[yuts.Length];
        initialYutRotations = new Quaternion[yuts.Length];
        for (int i = 0; i < yuts.Length; i++)
        {
            initialYutPositions[i] = yuts[i].transform.position;
            initialYutRotations[i] = yuts[i].transform.rotation;
        }

        resultHandler = GetComponent<YutResultHandler>();
        yutParentManager = GetComponent<Yut_YutParent_Manager>();
        yutGameTurn = FindAnyObjectByType<YutGameTurn>();
    }

    /// <summary>
    /// Input System에서 "two" 액션 호출 시 → 2인 플레이 시작
    /// </summary>
    public void OnTwo()
    {
        if (!yutGameTurn.gameStarted)
        {
            yutGameTurn.StartGame(2);
        }
    }

    /// <summary>
    /// Input System에서 "three" 액션 호출 시 → 3인 플레이 시작
    /// </summary>
    public void OnThree()
    {
        if (!yutGameTurn.gameStarted)
        {
            yutGameTurn.StartGame(3);
        }
    }

    /// <summary>
    /// Input System에서 "four" 액션 호출 시 → 4인 플레이 시작
    /// </summary>
    public void OnFour()
    {
        if (!yutGameTurn.gameStarted)
        {
            yutGameTurn.StartGame(4);
        }
    }

    IEnumerator CountRoutine()
    {
        Debug.Log("[CountYut] CountRoutine 시작됨! 7초 대기...");
        yield return new WaitForSeconds(7f);

        // YutGround 비활성화
        if (yutGround != null) yutGround.SetActive(false);

        // 순수 계산
        int result = YutResultCalculator.Calculate(yuts);
        Debug.Log($"[CountYut] 윷 결과: {YutResultCalculator.GetResultName(result)} ({result})");

        // 결과에 따른 게임 상태 처리
        resultHandler.HandleResult(result);

        // 윷을 원래 위치로 되돌림
        for (int i = 0; i < yuts.Length; i++)
        {
            yuts[i].transform.position = initialYutPositions[i];
            yuts[i].transform.rotation = initialYutRotations[i];

            Rigidbody rb = yuts[i].GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // 윷 막대 즉시 비활성화
        if (yutParentManager != null)
        {
            yutParentManager.HideYuts();
        }

        // 추가 던지기(윷/모)인 경우 바로 다시 활성화
        if (YutResultCalculator.IsExtraThrow(result))
        {
            if (yutParentManager != null)
                yutParentManager.ShowYuts();
        }

        yield return null;
    }
}
