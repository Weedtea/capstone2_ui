using UnityEngine;
using System.Collections;

public class BombComponent : MonoBehaviour
{
    [Header("폭탄 추적 설정")]
    public Transform currentOwner;
    public Vector3 floatingOffset = new Vector3(0, 2f, 0); // 머리 위 표시 위치
    public float smoothSpeed = 10f;
    [Header("폭탄 패스(무적) 설정")]
    public float passCooldown = 1f; // 폭탄을 넘긴 후 1초간은 다시 받지 않음
    private bool canBePassed = true;

    private Renderer meshRenderer;
    private bool isExploded = false;
    private float blinkTimer = 0f;

    void Start()
    {
        meshRenderer = GetComponent<Renderer>();
    }

    void Update()
    {
        // 룰렛 중이 아니고 정상 게임 진행 중일 때 폭탄 깜빡임 효과
        if (currentOwner != null && !isExploded && HotPotatoManager.Instance != null && !HotPotatoManager.Instance.isRouletteRunning && !HotPotatoManager.Instance.isGameOver)
        {
            float timeLeft = HotPotatoManager.Instance.totalGameTime;
            
            // 시간이 적게 남을수록 더 빨리 깜빡임 (0초에 가까워질수록 0.05초 주기, 15초 근방일 때 0.5초 주기)
            float t = Mathf.Clamp01(timeLeft / 15f); // 총 시간(15초) 기준 비율
            float blinkInterval = Mathf.Lerp(0.05f, 0.5f, t);

            blinkTimer -= Time.deltaTime;
            if (blinkTimer <= 0f)
            {
                if (meshRenderer != null)
                {
                    meshRenderer.enabled = !meshRenderer.enabled; // 깜빡임 토글
                }
                blinkTimer = blinkInterval;
            }
        }
        else if (HotPotatoManager.Instance != null && HotPotatoManager.Instance.isRouletteRunning && meshRenderer != null)
        {
            // 야바위 룰렛이 돌 때는 끄지 않고 항상 표시
            meshRenderer.enabled = true;
        }
    }

    void LateUpdate()
    {
        // 소유자가 있다면 지속적으로 머리 위를 따라다님 (폭발 전까지만)
        if (currentOwner != null && !isExploded)
        {
            Vector3 targetPos = currentOwner.position + floatingOffset;
            transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
        }
    }

    public void AssignToPlayer(Transform newOwner)
    {
        if (!canBePassed) return; // 쿨타임 중이면 넘기지 않음

        currentOwner = newOwner;
        StartCoroutine(PassCooldownRoutine());
    }

    IEnumerator PassCooldownRoutine()
    {
        // 일시적으로 다른 사람에게 넘길 수 없는 상태화 (연계 폭탄핑퐁 방지)
        canBePassed = false;
        yield return new WaitForSeconds(passCooldown);
        canBePassed = true;
    }

    public void Explode()
    {
        isExploded = true;
        // 본인(폭탄) 숨기기
        if (meshRenderer != null) meshRenderer.enabled = false;

        // 현재 소유자(탈락자) 시각화 (빨간색으로 변경 등)
        if (currentOwner != null)
        {
            Renderer ownerRenderer = currentOwner.GetComponent<Renderer>();
            if (ownerRenderer != null)
            {
                ownerRenderer.material.color = Color.red;
            }
            
            // 물리 효과 (위로 살짝 날려보냄)
            Rigidbody rb = currentOwner.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(Vector3.up * 10f, ForceMode.Impulse);
                rb.AddTorque(new Vector3(10f, 10f, 10f), ForceMode.Impulse);
            }
        }
    }

    public void ResetBomb()
    {
        isExploded = false;
        // 다음 라운드를 위해 폭탄 초기화
        if (meshRenderer != null) meshRenderer.enabled = true;
        currentOwner = null;
        canBePassed = true;
    }

    // 새 라운드 시작 시 엉뚱한 위치에서 날아오지 않도록 즉시 위치를 고정하는 메서드
    public void SnapToOwner()
    {
        if (currentOwner != null)
        {
            transform.position = currentOwner.position + floatingOffset;
        }
    }
}