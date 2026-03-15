using UnityEngine;
using System.Collections;

public class FinishLine : MonoBehaviour
{
    private bool isFinished = false;

    void OnTriggerEnter(Collider other)
    {
        // 태그가 Player인 오브젝트가 결승선에 닿았는지 확인
        if (other.CompareTag("Player") && !isFinished)
        {
            // 이미 게임이 종료되었는지 매니저를 통해 확인
            if (ObstacleRaceManager.Instance != null && ObstacleRaceManager.Instance.isGameOver) return;

            isFinished = true;

            // 플레이어 조작 비활성화 및 컴포넌트 처리
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.isStunned = true; // 이동 불가능하도록 처리
                
                Rigidbody rb = pc.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                
                // 게임 매니저에 결과 전달
                if (ObstacleRaceManager.Instance != null)
                {
                    ObstacleRaceManager.Instance.OnPlayerFinish(pc);
                }
            }
        }
    }
}
