using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MemoryFloorPlayer : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float fallThreshold = -5f;

    [Header("입력 키 설정")]
    public KeyCode upKey = KeyCode.W;
    public KeyCode downKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;

    private Rigidbody rb;
    public bool isEliminated = false;
    public bool isFinishedRound = false;

    // 진행 상황 추적용 변수 (전멸 시 승패 판정)
    public int highestRound = 0;
    public float maxZPosition = -999f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            PhysicsMaterial noFriction = new PhysicsMaterial("NoFriction");
            noFriction.dynamicFriction = 0f;
            noFriction.staticFriction = 0f;
            noFriction.frictionCombine = PhysicsMaterialCombine.Minimum;
            col.material = noFriction;
        }

        if (MemoryFloorManager.Instance != null)
        {
            MemoryFloorManager.Instance.RegisterPlayer(this);
        }
    }

    void Update()
    {
        if (isEliminated || isFinishedRound || MemoryFloorManager.Instance == null || !MemoryFloorManager.Instance.isGameActive || MemoryFloorManager.Instance.currentState != MemoryFloorManager.GameState.MovePhase) 
        {
            // 이동 대기 중에는 물리 속도 제거
            if (rb != null) rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        HandleMovement();
        CheckFall();
        UpdateProgress();
    }

    void HandleMovement()
    {
        float h = 0f;
        float v = 0f;

        if (Input.GetKey(upKey)) v = 1f;
        if (Input.GetKey(downKey)) v = -1f;
        if (Input.GetKey(leftKey)) h = -1f;
        if (Input.GetKey(rightKey)) h = 1f;

        Vector3 moveDir = new Vector3(h, 0, v).normalized;
        if (moveDir.magnitude >= 0.1f)
        {
            Vector3 targetPos = rb.position + moveDir * moveSpeed * Time.deltaTime;
            rb.MovePosition(targetPos);

            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 10f * Time.deltaTime));
        }
    }

    void UpdateProgress()
    {
        if (transform.position.z > maxZPosition)
        {
            maxZPosition = transform.position.z;
        }
        highestRound = MemoryFloorManager.Instance.currentRound;
    }

    void CheckFall()
    {
        // 떨어질 때 속도 더 빠르게 가속 (벽에 비비적거리는 현상 방지)
        if (rb != null && rb.linearVelocity.y < -0.1f)
        {
            rb.AddForce(Vector3.down * 40f, ForceMode.Acceleration);
        }

        if (transform.position.y < fallThreshold)
        {
            EliminatePlayer();
        }
    }

    public void EliminatePlayer()
    {
        if (isEliminated) return;
        isEliminated = true;
        Debug.Log($"{name} 탈락! (Round: {highestRound}, MaxZ: {maxZPosition})");
        MemoryFloorManager.Instance.OnPlayerFallen(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckFinish(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        CheckFinish(collision.gameObject);
    }

    private void OnCollisionStay(Collision collision)
    {
        CheckFinish(collision.gameObject);
    }

    private void CheckFinish(GameObject obj)
    {
        if (MemoryFloorManager.Instance != null && MemoryFloorManager.Instance.currentState == MemoryFloorManager.GameState.MovePhase)
        {
            if (obj.CompareTag("Finish") && !isFinishedRound && !isEliminated)
            {
                isFinishedRound = true;
                Debug.Log($"{name} 결승선 도달!");
                MemoryFloorManager.Instance.OnPlayerFinished(this);
                
                // 결승선 도달 시 캐릭터 숨기기
                gameObject.SetActive(false);
            }
        }
    }
}
