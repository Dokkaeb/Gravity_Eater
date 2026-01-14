using UnityEngine;
using TMPro;
using Photon.Pun;

public class NickNameSet : MonoBehaviourPun
{
    [SerializeField] TextMeshProUGUI _nameTxt;
    [SerializeField] Vector3 _offset = new Vector3(0, 0, 0);

    PhotonView _pv;
    Transform _playerTransform;

    private void Start()
    {
        _pv = GetComponentInParent<PhotonView>();

        if (_pv != null) _playerTransform = _pv.transform;

        if (_pv != null && _pv.Owner != null)
        {
            _nameTxt.text = _pv.Owner.NickName;

            if(_pv.IsMine) _nameTxt.color = Color.green; //내이름은 녹색
        }
    }

    private void LateUpdate()
    {
        if (_playerTransform == null) return;

        transform.rotation = Quaternion.identity;

        transform.position = _playerTransform.position + _offset;
    }
}
