using UnityEngine;

public class CameraScroller : MonoBehaviour
{
    [Header("추적 설정")]
    public Transform playerTarget;
    public float smoothSpeed = 7f; // 부드럽게 따라가는 속도
    public Vector3 offset = new Vector3(0, 5f, -10f); // 기본 오프셋 설정

    void Start()
    {
        // 플레이어 자동 탐색 (지정되지 않았을 경우)
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }
    }

    void LateUpdate()
    {
        if (playerTarget != null)
        {
            Vector3 desiredPosition = playerTarget.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
        }
    }
}
