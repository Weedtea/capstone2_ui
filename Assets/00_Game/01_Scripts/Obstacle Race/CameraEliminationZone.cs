using UnityEngine;
using System.Collections;

public class CameraEliminationZone : MonoBehaviour
{
    private bool isDead = false;

    void Update()
    {
        if (isDead) return;

        // 오직 바닥 아래(허공)로 떨어지면 탈락 (강제 스크롤 밀림 삭제)
        if (transform.position.y < -5f)
        {
            EliminatePlayer();
        }
    }

    void EliminatePlayer()
    {
        if (isDead || !gameObject.activeSelf) return;

        isDead = true;
        Debug.Log(gameObject.name + " 가 맵 아래로 추락하여 탈락했습니다!");

        // 1. 플레이어 조작 비활성화
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.isStunned = true; // 이동 불가 상태
            
            // 매니저에 탈락 알림 (동시 탈락/승리 판정용)
            if (ObstacleRaceManager.Instance != null)
            {
                ObstacleRaceManager.Instance.OnPlayerDeath(pc);
            }
        }

        // 2. 물리 정지 및 화면에서 지우기
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // 더 떨어지지 않게 고정
        }
        
        // 렌더러만 숨기기 (탈락 시점의 X 좌표 보존을 위해 오브젝트 자체는 비활성화 금지)
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            r.enabled = false;
        }
    }
}
