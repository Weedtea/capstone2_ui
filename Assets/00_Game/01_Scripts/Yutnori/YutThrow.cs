using UnityEngine;

/// <summary>
/// 윷 던지기 물리 처리 + 높이 제한
/// maxHeight를 초과하면 속도를 감쇠시켜 화면 밖으로 나가지 않도록 함
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class YutThrow : MonoBehaviour
{
    [Header("Setting")]
    public float throwPower = 50f;
    public float torquePower = 20f;

    [Header("Height Limit")]
    [Tooltip("윷이 올라갈 수 있는 최대 높이 (이 높이를 넘으면 아래로 당김)")]
    public float maxHeight = 15f;
    [Tooltip("최대 높이 초과 시 아래로 당기는 힘")]
    public float pullDownForce = 30f;

    private Rigidbody rb;
    private float initialY;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        initialY = transform.position.y;
    }

    void OnEnable()
    {
        if (rb != null)
        {
            rb.isKinematic = true; // 활성화 시 움직이지 못하게 (물리 끄기)
        }
    }

    public void ThrowYut(float multiplier = 1f)
    {
        rb.isKinematic = false; // 던지기 전 물리 켜기
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 throwDir = new Vector3(Random.Range(-0.1f, 0.1f), 1, Random.Range(-0.1f, 0.1f)).normalized;
        Vector3 randomTorque = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;

        rb.AddForce(throwDir * (throwPower * multiplier), ForceMode.Impulse);
        rb.AddTorque(randomTorque * (torquePower * multiplier), ForceMode.Impulse);
    }

    void FixedUpdate()
    {
        // 최대 높이를 초과하면 위쪽 속도를 제거하고 아래로 당김
        if (transform.position.y > maxHeight)
        {
            Vector3 vel = rb.linearVelocity;
            if (vel.y > 0)
            {
                vel.y = 0; // 올라가는 속도 제거
                rb.linearVelocity = vel;
            }
            // 아래로 당기는 추가 힘
            rb.AddForce(Vector3.down * pullDownForce, ForceMode.Acceleration);
        }
    }
}
