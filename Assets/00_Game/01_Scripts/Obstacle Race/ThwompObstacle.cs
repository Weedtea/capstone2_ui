using UnityEngine;
using System.Collections;

public class ThwompObstacle : MonoBehaviour
{
    [Header("쿵쿵이 설정 (2.5x 속도업)")]
    public float dropSpeed = 40f;      // 내려찍는 속도 (매우 빠름, 기존 15)
    public float riseSpeed = 8f;       // 올라가는 속도 (기존 3)
    public float waitTimeAtBottom = 0.5f; // 바닥에서 대기하는 시간 (단축)
    public float waitTimeAtTop = 0.75f;   // 위에서 대기하는 시간 (단축)
    public float dropDistance = 5f;    // 내려찍는 거리

    private Vector3 topPosition;
    private Vector3 bottomPosition;
    private bool isDropping = false;

    void Start()
    {
        topPosition = transform.position;
        // 바닥(Y=0)을 기준으로 쿵쿵이의 밑면이 정확히 닿도록 bottomPosition 설정
        // 쿵쿵이의 정중앙 좌표가 크기(Y)의 절반과 같으면 밑면이 Y=0에 닿게 됨 (큐브 기준)
        float targetY = transform.localScale.y / 2f; 
        bottomPosition = new Vector3(topPosition.x, targetY, topPosition.z); 
        
        StartCoroutine(ThwompRoutine());
    }

    // 쿵쿵이의 위아래 움직임 패턴
    IEnumerator ThwompRoutine()
    {
        while (true)
        {
            // 1. 위에서 대기
            yield return new WaitForSeconds(waitTimeAtTop);

            // 2. 빠르게 내리찍기
            isDropping = true;
            while (transform.position.y > bottomPosition.y)
            {
                transform.position = Vector3.MoveTowards(transform.position, bottomPosition, dropSpeed * Time.deltaTime);
                yield return null;
            }
            isDropping = false;

            // 3. 바닥에서 딜레이 (이때 플레이어가 지나가야 함)
            yield return new WaitForSeconds(waitTimeAtBottom);

            // 4. 천천히 제자리로 복귀
            while (transform.position.y < topPosition.y)
            {
                transform.position = Vector3.MoveTowards(transform.position, topPosition, riseSpeed * Time.deltaTime);
                yield return null;
            }
        }
    }

    // 충돌 판정 (물리적 벽 역할 수행)
    void OnCollisionEnter(Collision collision)
    {
        CheckSquash(collision.gameObject);
    }

    void OnCollisionStay(Collision collision)
    {
        CheckSquash(collision.gameObject);
    }

    void CheckSquash(GameObject obj)
    {
        if (obj.CompareTag("Player") && isDropping)
        {
            PlayerController player = obj.GetComponent<PlayerController>();
            // 이미 납작해진 상태이거나 무적 상태일 때는 무시
            if (player != null && !player.isSquashed && !player.isInvincible)
            {
                player.ApplyInvincibility(); // 피격 쿨타임 시작
                StartCoroutine(SquashPlayer(player));
            }
        }
    }

    // 플레이어를 납작하게 만드는 코루틴
    IEnumerator SquashPlayer(PlayerController player)
    {
        player.isSquashed = true;
        
        // 시각적으로 Y축을 눌러 납작하게 표현
        player.transform.localScale = new Vector3(1.5f, 0.2f, 1.5f);

        // 2초 동안 페널티 부여
        yield return new WaitForSeconds(2f); 

        // 2초 후 원상 복구
        player.isSquashed = false;
        player.transform.localScale = Vector3.one;
    }
}