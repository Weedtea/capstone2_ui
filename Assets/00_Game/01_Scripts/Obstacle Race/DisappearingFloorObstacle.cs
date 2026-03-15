using UnityEngine;
using System.Collections;

public class DisappearingFloorObstacle : MonoBehaviour
{
    [Header("동작 설정 (2.5x 속도업)")]
    public float waitBeforeDisappear = 0.4f; // 밟고 사라지기 전 대기 시간 (기존 1.0)
    public float blinkInterval = 0.05f; // 점멸 간격 (기존 0.15)
    public float reappearDelay = 1.2f; // 사라진 뒤 다시 나타나는 시간 (기존 3.0)

    private Renderer meshRenderer;
    private Collider floorCollider;
    private bool isTriggered = false;

    void Start()
    {
        meshRenderer = GetComponent<Renderer>();
        floorCollider = GetComponent<Collider>();
    }

    void OnCollisionEnter(Collision collision)
    {
        // 바닥을 플레이어가 밟았고, 아직 발동되지 않은 경우
        if (collision.gameObject.CompareTag("Player") && !isTriggered)
        {
            // 플레이어가 발판의 위에서 밟았는지 대략적으로 확인 (플레이어의 Y 위치가 발판보다 높을 때)
            if (collision.contacts[0].normal.y < -0.5f)
            {
                StartCoroutine(DisappearRoutine());
            }
        }
    }

    IEnumerator DisappearRoutine()
    {
        isTriggered = true;

        // 1. 깜빡임 효과 (사라지기 직전)
        float elapsedTime = 0f;
        bool isVisible = true;
        
        while (elapsedTime < waitBeforeDisappear)
        {
            // 렌더러 점멸
            isVisible = !isVisible;
            if (meshRenderer != null) meshRenderer.enabled = isVisible;
            
            yield return new WaitForSeconds(blinkInterval);
            elapsedTime += blinkInterval;
        }

        // 2. 발판 완전히 비활성화 (보이지 않고 충돌체 제거)
        if (meshRenderer != null) meshRenderer.enabled = false;
        if (floorCollider != null) floorCollider.enabled = false;

        // 3. 재활성화 대기
        yield return new WaitForSeconds(reappearDelay);

        // 4. 발판 복귀
        if (meshRenderer != null) meshRenderer.enabled = true;
        if (floorCollider != null) floorCollider.enabled = true;
        isTriggered = false;
    }
}
