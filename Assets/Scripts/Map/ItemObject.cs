using UnityEngine;
using Photon.Pun;
using DG.Tweening;

public class ItemObject : MonoBehaviourPun
{
    [Header("아이템 부유 설정")]
    [SerializeField] float _moveRange = 1f;
    [SerializeField] float _moveTime = 2f;

    ItemData _data;
    SpriteRenderer _spr;

    private void Awake()
    {
        _spr = GetComponent<SpriteRenderer>();
    }
    
    public void Setup(ItemData data)
    {
        _data = data;
        _spr.sprite = data.itemSprite;
    }
    private void Start()
    {
        transform.DOLocalMoveY(transform.localPosition.y + _moveRange, _moveTime)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            var player = collision.GetComponent<PlayerCtrl>();

            if(player != null && player.photonView.IsMine)
            {
                player.ApplyItemEffect(_data);
                
                MapGenerator.Instance.RequestDestroyItem(transform.position);
            }
        }
    }
}
