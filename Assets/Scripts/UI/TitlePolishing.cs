using UnityEngine;
using DG.Tweening;

public class TitlePolishing : MonoBehaviour
{
    [SerializeField] float _moveRange = 30f;
    [SerializeField] float _moveTime = 2f;
    private void Start()
    {
        transform.DOLocalMoveY(transform.localPosition.y + _moveRange, _moveTime)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);

    }
}
