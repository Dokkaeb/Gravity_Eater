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
                _player.OnHeadHitOhterPlayer();
            }
        }
    }
}
