using UnityEngine;
using DG.Tweening;

public class ArrowMove : MonoBehaviour
{
    public float moveDistance = 0.5f;
    public float duration = 1f;
    private Tween bobbingTween;

    void Start()
    {
        // 위아래로 부드럽게 움직이는 애니메이션
        bobbingTween = transform.DOMoveY(transform.position.y + moveDistance, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    void OnDestroy()
    {
        if (bobbingTween != null && bobbingTween.IsActive())
        {
            bobbingTween.Kill();
        }
    }
}
