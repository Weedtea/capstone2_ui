using UnityEngine;
using System.Collections;

public class SpinningClubObstacle : MonoBehaviour
{
    [Header("회전 설정 (2.5x 속도업)")]
    public float rotationSpeed = 350f; // 회전 속도 (도/초) (기존 150)
    public Vector3 rotationAxis = Vector3.right; // 회전 축 (기본값: X축)

    [Header("피격 설정")]
    public float stunDuration = 1.5f; // 기절 지속 시간
    public float knockbackForce = 15f; // 넉백 힘

    void Update()
    {
        // 지속적으로 지정된 축을 기준으로 회전
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();

            if (player != null && !player.isStunned && !player.isInvincible)
            {
                // 피격 쿨타임 시작
                player.ApplyInvincibility();

                // 넉백 방향 계산 (장애물 중심에서 플레이어 방향으로, Z축 무시)
                Vector3 knockbackDir = (collision.transform.position - transform.position).normalized;
                knockbackDir.z = 0; // Z축 밀림 방지
                knockbackDir.y = 0.5f; // 살짝 위로 띄워서 날아가는 느낌 부여
                knockbackDir = knockbackDir.normalized;

                // 플레이어에게 넉백 힘 적용
                if (playerRb != null)
                {
                    // 속도 초기화 후 튕겨나가게 함
                    playerRb.linearVelocity = Vector3.zero;
                    playerRb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
                }

                // 기절 코루틴 실행
                StartCoroutine(StunPlayer(player));
            }
        }
    }

    IEnumerator StunPlayer(PlayerController player)
    {
        // PlayerController에 있던 isStunned 상태를 true로 변경하여 이동 제한
        player.isStunned = true;

        // (선택) 시각적 피드백: 플레이어 색상을 잠시 빨간색으로 변경
        Renderer playerRenderer = player.GetComponent<Renderer>();
        Color originalColor = Color.white;

        if (playerRenderer != null)
        {
            originalColor = playerRenderer.material.color;
            playerRenderer.material.color = Color.red;
        }

        // stunDuration 만큼 대기
        yield return new WaitForSeconds(stunDuration);

        // 상태 및 색상 원상 복구
        if (playerRenderer != null)
        {
            playerRenderer.material.color = originalColor;
        }
        player.isStunned = false;
    }
}
