using UnityEngine;
using System.Collections;

public class MovingSpikeObstacle : MonoBehaviour
{
    [Header("이동 설정 (2.5x 속도업)")]
    public float moveSpeed = 12.5f; // 이동 속도 (기존 5)
    public float moveDistance = 5f; // 이동 거리
    public Vector3 moveDirection = Vector3.forward; // 이동 방향 (기본값: Z축)

    [Header("대기 시간")]
    public float waitTimeAtEnds = 0.2f; // 양 끝에 도달했을 때 대기 시간 (단축)

    [Header("피격 설정")]
    public float stunDuration = 1f; // 기절 시간
    public float knockbackForce = 10f; // 넉백 힘

    private Vector3 startPos;
    private Vector3 endPos;
    private bool isWaiting = false;

    void Start()
    {
        startPos = transform.position;
        // 주어진 방향과 거리만큼 끝점 설정
        endPos = startPos + (moveDirection.normalized * moveDistance);
        
        StartCoroutine(MoveRoutine());
    }

    IEnumerator MoveRoutine()
    {
        while (true)
        {
            // 1. 끝점으로 이동
            yield return MoveToPosition(endPos);
            
            // 2. 대기
            isWaiting = true;
            yield return new WaitForSeconds(waitTimeAtEnds);
            isWaiting = false;

            // 3. 시작점으로 이동
            yield return MoveToPosition(startPos);

            // 4. 대기
            isWaiting = true;
            yield return new WaitForSeconds(waitTimeAtEnds);
            isWaiting = false;
        }
    }

    IEnumerator MoveToPosition(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();

            if (player != null && !player.isStunned && !player.isInvincible)
            {
                // 피격 무적 처리
                player.ApplyInvincibility();

                // 플레이어가 부딪혔을 때 넉백 방향 계산 (Z축은 무시하여 2D로 고정)
                Vector3 knockbackDir = (collision.transform.position - transform.position).normalized;
                knockbackDir.z = 0; // Z축 밀림 방지
                knockbackDir.y = 0.5f;
                // 다시 정규화 (대각선 위로 일정한 힘을 받도록)
                knockbackDir = knockbackDir.normalized;

                if (playerRb != null)
                {
                    playerRb.linearVelocity = Vector3.zero;
                    playerRb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
                }

                StartCoroutine(StunPlayer(player));
            }
        }
    }

    IEnumerator StunPlayer(PlayerController player)
    {
        player.isStunned = true;

        // 시각적 피드백
        Renderer playerRenderer = player.GetComponent<Renderer>();
        Color originalColor = Color.white;
        if (playerRenderer != null)
        {
            originalColor = playerRenderer.material.color;
            playerRenderer.material.color = Color.red;
        }

        yield return new WaitForSeconds(stunDuration);

        if (playerRenderer != null)
        {
            playerRenderer.material.color = originalColor;
        }
        player.isStunned = false;
    }
}
