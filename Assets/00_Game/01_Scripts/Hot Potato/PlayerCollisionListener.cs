using UnityEngine;

public class PlayerCollisionListener : MonoBehaviour
{
    private HotPotatoPlayerController myController;

    void Start()
    {
        myController = GetComponent<HotPotatoPlayerController>();
    }

    void OnCollisionEnter(Collision collision)
    {
        // 야바위 연출 중이거나 승패 확정된 상태라면 폭탄 떠넘기기 방지
        if (HotPotatoManager.Instance != null && (HotPotatoManager.Instance.isRouletteRunning || HotPotatoManager.Instance.isGameOver))
        {
            return;
        }

        // 부딪힌 대상이 플레이어일 경우
        if (collision.gameObject.CompareTag("Player"))
        {
            BombComponent bomb = HotPotatoManager.Instance.bombObject;

            if (bomb == null) return;

            // 만약 내가 폭탄을 가지고 있는 상태라면
            if (bomb.currentOwner == this.transform)
            {
                HotPotatoPlayerController otherPlayer = collision.gameObject.GetComponent<HotPotatoPlayerController>();

                if (otherPlayer != null)
                {
                    // 상대방에게 폭탄 이양
                    bomb.AssignToPlayer(otherPlayer.transform);
                }
            }
        }
    }
}