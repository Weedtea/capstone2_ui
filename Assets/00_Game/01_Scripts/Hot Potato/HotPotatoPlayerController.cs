using UnityEngine;

public class HotPotatoPlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 8f;
    public float rotationSpeed = 15f;

    [Header("입력 설정")]
    public KeyCode upKey = KeyCode.W;
    public KeyCode downKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public bool isAI = false;

    private Rigidbody rb;
    private bool isInputEnabled = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // 넘어지지 않게 회전 잠금
        if(rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    void FixedUpdate()
    {
        if (!isInputEnabled)
        {
            if (rb != null) rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        if (!isAI)
        {
            HandlePlayerMovement();
        }
        else
        {
            HandleAIMovement();
        }
    }

    void HandlePlayerMovement()
    {
        float moveX = 0f;
        float moveZ = 0f;

        if (Input.GetKey(leftKey)) moveX = -1f;
        if (Input.GetKey(rightKey)) moveX = 1f;
        if (Input.GetKey(downKey)) moveZ = -1f;
        if (Input.GetKey(upKey)) moveZ = 1f;

        Vector3 moveDir = new Vector3(moveX, 0f, moveZ).normalized;

        if (moveDir.sqrMagnitude > 0f)
        {
            float currentSpeed = moveSpeed;

            // 내가 폭탄을 들고 있다면 이동 속도 1.1배 상향
            if (HotPotatoManager.Instance != null && 
                HotPotatoManager.Instance.bombObject != null && 
                HotPotatoManager.Instance.bombObject.currentOwner == this.transform)
            {
                currentSpeed *= 1.1f;
            }

            Vector3 targetPosition = rb.position + moveDir * currentSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPosition);

            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    [Header("AI 설정")]
    public float changeDirTimeMin = 1f;
    public float changeDirTimeMax = 3f;
    private float aiTimer = 0f;
    private Vector3 currentAIMoveDir;

    void HandleAIMovement()
    {
        aiTimer -= Time.fixedDeltaTime;

        if (aiTimer <= 0f)
        {
            // 새로운 랜덤 방향 설정 (상하좌우 대각선 등 평면 Z,X축 이동)
            float randX = Random.Range(-1f, 1f);
            float randZ = Random.Range(-1f, 1f);
            
            // 중앙(투기장 밖)으로 나가지 않도록 살짝 보정 (선택적)
            Vector3 centerDir = (-transform.position).normalized;
            centerDir.y = 0;
            
            // 중심 방향으로 일정 확률로 방향 보정 (탈선 방지)
            if(Vector3.Distance(transform.position, Vector3.zero) > 10f)
            {
                 currentAIMoveDir = centerDir;
            }
            else
            {
                 currentAIMoveDir = new Vector3(randX, 0f, randZ).normalized;
            }

            aiTimer = Random.Range(changeDirTimeMin, changeDirTimeMax);
        }

        if (currentAIMoveDir.sqrMagnitude > 0f)
        {
            Vector3 targetPosition = rb.position + currentAIMoveDir * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPosition);

            Quaternion targetRotation = Quaternion.LookRotation(currentAIMoveDir);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    public void EnableInput(bool enable)
    {
        isInputEnabled = enable;
    }
}