using UnityEngine;
using Photon.Pun;

public class HeadDetector : MonoBehaviour
{
    PlayerCtrl _player;

    private void Awake()
    {
        _player = GetComponentInParent<PlayerCtrl>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_player.photonView.IsMine) return;

        if (collision.CompareTag("Player"))
        {
            PhotonView targetPV = collision.GetComponent<PhotonView>();
            if (targetPV != null && !targetPV.IsMine)
            {
                //상대 커스텀 프로퍼티에 무적여부 확인
                if (targetPV.Owner.CustomProperties.TryGetValue("IsInvincible", out object isInvincible))
                {
                    if ((bool)isInvincible)
                    {
                        Debug.Log($"상대방({targetPV.Owner.NickName})이 무적임");
                        return;
                    }
                }

                PlayerCtrl otherPlayer = collision.GetComponent<PlayerCtrl>();
                _player.OnHeadHitOhterPlayer(otherPlayer);
            }
        }
    }
}
