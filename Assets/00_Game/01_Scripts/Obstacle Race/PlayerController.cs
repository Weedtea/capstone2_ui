using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    public float speed = 5f;
    public float jumpForce = 7f;
    private Rigidbody rb;
    private bool isGrounded;
    private Renderer meshRenderer;

    [Header("상태 이상")]
    public bool isStunned = false;   // 회전 몽둥이에 맞았을 때
    public bool isSquashed = false;  // 쿵쿵이에 깔렸을 때

    [Header("무적 쿨타임 설정")]
    public bool isInvincible = false;
    public float invincibilityDuration = 1.5f; // 피격 후 무적 시간
    public float blinkInterval = 0.1f; // 깜빡이는 간격

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<Renderer>();
    }

    void Update()
    {
        // 기절 상태면 조작 불가
        if (isStunned) return; 

        Move();
        Jump();
    }

    void Move()
    {
        // 좌우(X축) 이동만 허용
        float moveX = Input.GetAxis("Horizontal");

        // 쿵쿵이에 깔려 납작해지면 이동 속도가 30%로 감소
        float currentSpeed = isSquashed ? speed * 0.3f : speed;

        Vector3 movement = new Vector3(moveX, 0, 0) * currentSpeed * Time.deltaTime;
        transform.Translate(movement, Space.World);
    }

    void Jump()
    {
        // 스페이스바를 누르고, 바닥에 닿아있고, 납작해진 상태가 아닐 때만 점프 가능
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isSquashed)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    // 바닥 착지 판정
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    // 무적 쿨타임 및 깜빡임 효과 시작
    public void ApplyInvincibility()
    {
        if (!isInvincible)
        {
            StartCoroutine(InvincibilityRoutine());
        }
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        float elapsed = 0f;

        // 지정된 무적 시간 동안 깜빡임 반복
        while (elapsed < invincibilityDuration)
        {
            if (meshRenderer != null)
            {
                meshRenderer.enabled = !meshRenderer.enabled;
            }
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        // 끝날 때 렌더러가 무조건 켜지게 고정
        if (meshRenderer != null)
        {
            meshRenderer.enabled = true;
        }
        isInvincible = false;
    }
}