using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class ColorTilesPlayer : MonoBehaviour
{
    [Header("플레이어 정보")]
    public int playerID;
    public Color myColor;

    [Header("조작 키")]
    public KeyCode upKey;
    public KeyCode downKey;
    public KeyCode leftKey;
    public KeyCode rightKey;
    public KeyCode jumpKey;

    [Header("이동 설정")]
    public float moveSpeed = 10f;
    public float jumpForce = 7f;

    [Header("상태이상 설정")]
    public float squashDuration = 2f;
    public float iFrameDuration = 1.5f;

    private Rigidbody rb;
    private bool isGrounded = false;

    public bool isSquashed = false;
    public bool isInvincible = false;

    private Vector3 originalScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;
    }

    public void Initialize(int id, Color color, KeyCode[] keys)
    {
        playerID = id;
        myColor = color;
        upKey    = keys[0];
        downKey  = keys[1];
        leftKey  = keys[2];
        rightKey = keys[3];
        jumpKey  = keys[4];

        // 플레이어 색 적용
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.material.color = color;

        if (ColorTilesManager.Instance != null)
            ColorTilesManager.Instance.RegisterPlayer(this);

        // Rigidbody: 회전만 잠금 (PositionY는 절대 잠그면 안 됨 - 점프/낙하 불가해짐)
        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationY
                       | RigidbodyConstraints.FreezeRotationZ;

        // 마찰 없애기
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            PhysicsMaterial noFriction = new PhysicsMaterial("NoFriction")
            {
                dynamicFriction = 0f,
                staticFriction  = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };
            col.material = noFriction;
        }
    }

    private void Update()
    {
        if (ColorTilesManager.Instance == null ||
            ColorTilesManager.Instance.currentState != ColorTilesManager.GameState.Playing)
        {
            // 게임 대기/종료 상태에서는 수평 이동 즉시 강제 정지
            if (rb != null)
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        HandleMovement();
        HandleJump();
        ApplyFallGravity();
    }

    // 낙하 시 중력 30% 추가 (점프 상승 구간은 제외)
    private void ApplyFallGravity()
    {
        if (rb.linearVelocity.y < 0f)
        {
            rb.AddForce(Vector3.down * Physics.gravity.magnitude * 0.3f, ForceMode.Acceleration);
        }
    }

    private void HandleMovement()
    {
        Vector3 moveDir = Vector3.zero;
        if (Input.GetKey(upKey))    moveDir += Vector3.forward;
        if (Input.GetKey(downKey))  moveDir += Vector3.back;
        if (Input.GetKey(leftKey))  moveDir += Vector3.left;
        if (Input.GetKey(rightKey)) moveDir += Vector3.right;

        moveDir.Normalize();

        float currentSpeed = moveSpeed;
        if (isSquashed) currentSpeed *= 0.3f;

        Vector3 targetVelocity = moveDir * currentSpeed;
        targetVelocity.y = rb.linearVelocity.y; // 중력 유지
        rb.linearVelocity = targetVelocity;

        if (moveDir != Vector3.zero)
            transform.forward = moveDir;
    }

    private void HandleJump()
    {
        // 납작 상태일 때 점프 불가 (기획서 규칙)
        if (isSquashed) return;

        if (isGrounded && Input.GetKeyDown(jumpKey))
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            isGrounded = false;
        }
    }

    // ── 충돌 ──────────────────────────────────────────────

    private void OnCollisionEnter(Collision collision)
    {
        // 착지 판정 (타일 위)
        foreach (ContactPoint cp in collision.contacts)
        {
            if (cp.normal.y > 0.5f)
            {
                isGrounded = true;
                break;
            }
        }

        if (ColorTilesManager.Instance == null ||
            ColorTilesManager.Instance.currentState != ColorTilesManager.GameState.Playing)
            return;

        // 타일 색칠
        if (collision.gameObject.CompareTag("ColorTile"))
        {
            ColorTilesTile tile = collision.gameObject.GetComponent<ColorTilesTile>();
            if (tile != null) tile.ChangeColor(playerID, myColor);
        }

        // 플레이어 간 상호작용
        if (collision.gameObject.CompareTag("Player"))
        {
            ColorTilesPlayer other = collision.gameObject.GetComponent<ColorTilesPlayer>();
            if (other != null)
            {
                // 머리 밟기 (Stomp)
                if (transform.position.y > other.transform.position.y + 0.8f)
                {
                    other.TakeStomp();
                    // 밟은 플레이어는 살짝 튀어오름
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce * 0.8f, rb.linearVelocity.z);
                }
                else if (Mathf.Abs(transform.position.y - other.transform.position.y) <= 0.8f)
                {
                    // 옆 충돌 → 약한 넉백
                    Vector3 pushDir = (transform.position - other.transform.position).normalized;
                    pushDir.y = 0;
                    rb.AddForce(pushDir * 5f, ForceMode.Impulse);
                }
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // 착지 상태 유지
        foreach (ContactPoint cp in collision.contacts)
        {
            if (cp.normal.y > 0.5f)
            {
                isGrounded = true;
                break;
            }
        }

        if (ColorTilesManager.Instance == null ||
            ColorTilesManager.Instance.currentState != ColorTilesManager.GameState.Playing)
            return;

        // 타일 위에 서있는 동안 계속 색칠
        if (collision.gameObject.CompareTag("ColorTile"))
        {
            ColorTilesTile tile = collision.gameObject.GetComponent<ColorTilesTile>();
            if (tile != null) tile.ChangeColor(playerID, myColor);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // 지면에서 떨어지면 착지 해제
        // OnCollisionStay가 있을 때는 거기서 다시 true로 세팅됨
        isGrounded = false;
    }

    // ── Stomp 상태이상 ─────────────────────────────────────

    public void TakeStomp()
    {
        if (isInvincible || isSquashed) return;
        StartCoroutine(SquashRoutine());
    }

    private IEnumerator SquashRoutine()
    {
        isSquashed = true;
        transform.localScale = new Vector3(originalScale.x, originalScale.y * 0.3f, originalScale.z);
        yield return new WaitForSeconds(squashDuration);
        transform.localScale = originalScale;
        isSquashed = false;
        StartCoroutine(IFrameRoutine());
    }

    private IEnumerator IFrameRoutine()
    {
        isInvincible = true;
        MeshRenderer mr = GetComponent<MeshRenderer>();
        float timer = 0f;
        bool toggle = false;
        while (timer < iFrameDuration)
        {
            if (mr != null) mr.enabled = toggle;
            toggle = !toggle;
            timer += 0.15f;
            yield return new WaitForSeconds(0.15f);
        }
        if (mr != null) mr.enabled = true;
        isInvincible = false;
    }
}
