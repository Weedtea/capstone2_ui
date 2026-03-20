using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(CountYut))]
public class Yut_YutParent_Manager : MonoBehaviour
{
    public YutThrow[] yutThrows;
    public CountYut countYut;
    public YutGameTurn yutGameTurn;
    public GameObject yutGround;

    private YutResultHandler resultHandler;
    private Coroutine throwTimerCoroutine;
    private float throwWaitTime = 10f; // 15초 대기

    [Header("던지기 게이지 설정")]
    public UnityEngine.UI.Slider powerGaugeSlider; // 에디터에서 할당
    public float chargeSpeed = 1f; // 게이지 차오르는 속도
    
    private PlayerInput playerInput;
    private float currentPowerMultiplier = 1f;
    private bool isCharging = false;
    private int chargeDirection = 1;

    [Header("아이템 상태")]
    public bool isItemTargeting = false;

    void Awake()
    {
        yutThrows = GetComponentsInChildren<YutThrow>();
        countYut = GetComponent<CountYut>();
        resultHandler = GetComponent<YutResultHandler>();
        playerInput = GetComponent<PlayerInput>();
        if (yutGameTurn == null) yutGameTurn = FindAnyObjectByType<YutGameTurn>();
    }

    void Update()
    {
        // 턴 상태 체크
        if (!yutGameTurn.gameStarted) return;
        if (yutGameTurn.isThrowedThisTurn) return;

        // 플레이어 턴 도중 타겟팅 모드 처리
        if (isItemTargeting)
        {
            // 타겟팅 중에 윷 던지기는 무시
            return;
        }

        // 아이템 사용 버튼 입력 검출
        if (playerInput != null && playerInput.actions != null)
        {
            var useItemAction = playerInput.actions["UseItem"];
            if (useItemAction != null && useItemAction.WasPressedThisFrame())
            {
                TryUseItem();
                if (isItemTargeting) return; // 성공적으로 아이템 사용 (타겟팅 모드 진입)
            }
        }

        // PlayerInput을 이용해버튼이 눌려 있는지 확인
        bool isPressing = false;
        if (playerInput != null && playerInput.actions != null)
        {
            var throwAction = playerInput.actions["ThrowYut"];
            //var clickAction = playerInput.actions["MouseLeftClick"];

            if ((throwAction != null && throwAction.IsPressed())/*  || (clickAction != null && clickAction.IsPressed()) */)
            {
                isPressing = true;
            }
        }

        // 윷이 활성화된 상태(ShowYuts로 인해)에서만 조작이 가능함
        // yutGround.activeSelf 가 true이거나 하는 식의 체크 가능
        if (yutThrows.Length > 0 && yutThrows[0].gameObject.activeSelf)
        {
            if (isPressing)
            {
                // 충전 시작
                if (!isCharging)
                {
                    isCharging = true;
                    currentPowerMultiplier = 1f;
                    chargeDirection = 1;

                    // 버튼이 눌리면 10초 카운트 종료
                    if (throwTimerCoroutine != null)
                    {
                        StopCoroutine(throwTimerCoroutine);
                        throwTimerCoroutine = null;
                        Debug.Log("[Yut_YutParent_Manager] 던지기 버튼 눌림: 10초 타이머 취소");
                    }
                    
                    if (powerGaugeSlider != null) 
                    {
                        powerGaugeSlider.gameObject.SetActive(true);
                        powerGaugeSlider.value = currentPowerMultiplier;
                    }
                }

                // 충전 진행 (핑퐁)
                currentPowerMultiplier += chargeSpeed * chargeDirection * Time.deltaTime;
                if (currentPowerMultiplier >= 2f)
                {
                    currentPowerMultiplier = 2f;
                    chargeDirection = -1;
                }
                else if (currentPowerMultiplier <= 1f)
                {
                    currentPowerMultiplier = 1f;
                    chargeDirection = 1;
                }

                if (powerGaugeSlider != null)
                {
                    powerGaugeSlider.value = currentPowerMultiplier;
                }
            }
            else if (isCharging)
            {
                // 충전이 끝난 상태 (키를 뗌) -> ThrowYut 수행
                isCharging = false;
                if (powerGaugeSlider != null) powerGaugeSlider.gameObject.SetActive(false);
                ExecuteThrow(currentPowerMultiplier);
            }
        }
    }


    private void TryUseItem()
    {
        // 내 턴이 아니거나 던졌다면 사용 불가
        if (yutGameTurn.isThrowedThisTurn) return;

        // 현재 턴의 플레이어 가져오기
        GameObject currentPlayerObj = yutGameTurn.GetCurrentPlayer();
        if (currentPlayerObj == null) return;

        YutInventory inv = currentPlayerObj.GetComponent<YutInventory>();
        if (inv != null && inv.items.Count > 0)
        {
            // 가장 첫 번째 아이템을 무조건 사용하는 것으로 처리 (확장성 고려)
            YutItem itemToUse = inv.items[0];

            if (ItemTargetSelector.Instance != null)
            {
                isItemTargeting = true;
                
                // 10초 타이머 정지 (타겟팅 하는 동안엔 타이머 흘러가지 않음)
                if (throwTimerCoroutine != null)
                {
                    StopCoroutine(throwTimerCoroutine);
                    throwTimerCoroutine = null;
                }
                
                // 게이지 초기화
                isCharging = false;
                if (powerGaugeSlider != null) powerGaugeSlider.gameObject.SetActive(false);

                ItemTargetSelector.Instance.StartTargeting(itemToUse, currentPlayerObj);
            }
        }
        else
        {
            Debug.Log("[아이템 사용 불가] 인벤토리에 아이템이 존재하지 않습니다.");
        }
    }

    /// <summary>
    /// 아이템 타겟팅 및 사용이 완료되어 윷 던지기 상태로 다시 돌아올 때 호출됩니다.
    /// </summary>
    public void OnItemUseCompleted()
    {
        isItemTargeting = false;
        
        // 아이템 사용 후 다시 던질 수 있도록 10초 타이머 재시작
        StartThrowTimer();
    }

    /// <summary>
    /// 우클릭 등을 통해 타겟팅이 취소되어 돌아올 때 호출됩니다.
    /// </summary>
    public void OnItemUseCanceled()
    {
        isItemTargeting = false;
        
        // 다시 던질 수 있도록 10초 타이머 재시작
        StartThrowTimer();
    }

    public void OnThrowButtonClicked()
    {
        if (yutGameTurn.isThrowedThisTurn || isCharging) return;
        ExecuteThrow(1.5f); // Use a standard power multiplier for button clicks
    }

    private void ExecuteThrow(float multiplier)
    {
        if (yutGameTurn.isThrowedThisTurn) return;

        if (throwTimerCoroutine != null)
        {
            StopCoroutine(throwTimerCoroutine);
            throwTimerCoroutine = null;
        }

        if (yutGround != null) yutGround.SetActive(true);

        ShowYuts(false); // 타이머를 다시 시작하지 않음

        foreach (var yut in yutThrows)
        {
            yut.ThrowYut(multiplier);
        }
        countYut.StartCoroutine("CountRoutine");
        yutGameTurn.isThrowedThisTurn = true;
    }

    /// <summary>
    /// 윷 막대들을 활성화합니다.
    /// </summary>
    public void ShowYuts(bool startTimer = true)
    {
        foreach (var yut in yutThrows)
        {
            if (yut != null) yut.gameObject.SetActive(true);
        }

        // 윷이 활성화될 때마다 10초 타이머 시작 (게임이 시작된 이후에만)
        if (startTimer && yutGameTurn != null && yutGameTurn.gameStarted)
        {
            StartThrowTimer();
        }
    }

    private void StartThrowTimer()
    {
        if (throwTimerCoroutine != null)
        {
            StopCoroutine(throwTimerCoroutine);
        }
        throwTimerCoroutine = StartCoroutine(ThrowTimerRoutine());
    }

    private System.Collections.IEnumerator ThrowTimerRoutine()
    {
        Debug.Log($"[Yut_YutParent_Manager] 윷 던지기 대기 10초 시작!");
        yield return new WaitForSeconds(throwWaitTime);

        // 대기 중 이미 던졌거나 충전 중이라면 강제 '도' 처리 취소
        if (yutGameTurn.isThrowedThisTurn || isCharging) 
        {
            Debug.Log($"[Yut_YutParent_Manager] 10초 초과, 그러나 이미 던졌거나 충전 중이므로 무시합니다.");
            yield break;
        }

        Debug.Log($"[Yut_YutParent_Manager] 10초 초과! 강제로 '도'를 던진 것으로 처리합니다.");
        
        // 강제 처리: 윷 숨기기, 턴 진행됨 표시, 핸들러에 도(1) 결과 전달
        yutGameTurn.isThrowedThisTurn = true;
        HideYuts();
        if (resultHandler != null)
        {
            resultHandler.HandleResult(1); // 1 = 도
        }
    }

    /// <summary>
    /// 윷 막대들을 비활성화합니다.
    /// </summary>
    public void HideYuts()
    {
        foreach (var yut in yutThrows)
        {
            if (yut != null) yut.gameObject.SetActive(false);
        }
    }
}
