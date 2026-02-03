using UnityEngine;
using TMPro;
using Photon.Pun;
using DG.Tweening;

public class PlayerCanvasSet : MonoBehaviourPun
{
    [Header("닉네임")]
    [SerializeField] TextMeshProUGUI _nameTxt;
    [SerializeField] Vector3 _offset = new Vector3(0, 0, 0);

    [Header("상태 아이콘")]
    [SerializeField] GameObject _stunIcon;
    [SerializeField] GameObject _slowIcon;
    [SerializeField] GameObject _protectIcon;

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

        float currentScale = _playerTransform.localScale.x;

        transform.position = _playerTransform.position + (currentScale * _offset);
    }

    public void SetStunVisual(bool isStun)
    {
        if (_stunIcon == null) return;

        _stunIcon.transform.DOKill();

        _stunIcon.SetActive(isStun);

        if (isStun)
        {
            _stunIcon.transform.localScale = Vector3.one; // 크기 초기화

            //펀치 효과: 0.5초 동안 기본 크기에서 1.2배 정도로 튕김 (Vibrato는 튕기는 횟수)
            _stunIcon.transform.DOPunchScale(Vector3.one * 0.2f, 2f, vibrato: 5, elasticity: 0.5f);
        }
    }

    public void SetSlowVisual(bool isSlow)
    {
        if (_slowIcon == null) return;

        _slowIcon.transform.DOKill();
        _slowIcon.SetActive(isSlow);

        if (isSlow)
        {
            _slowIcon.transform.localScale = Vector3.one;
            _slowIcon.transform.DOPunchScale(Vector3.one * 0.2f, 3f, vibrato: 5, elasticity: 0.5f);
        }
    }

    public void SetProtectVisual(bool isProtect)
    {
        if (_protectIcon == null) return;

        _protectIcon.transform.DOKill();
        _protectIcon.SetActive(isProtect);

        if (isProtect)
        {
            _protectIcon.transform.localScale = Vector3.one;
            _protectIcon.transform.DOPunchScale(Vector3.one * 0.2f, 10f, vibrato: 5, elasticity: 0.5f);
        }
    }

    private void OnDestroy()
    {
        if (_stunIcon != null) _stunIcon.transform.DOKill();
        if (_slowIcon != null) _slowIcon.transform.DOKill();
        if (_protectIcon != null) _protectIcon.transform.DOKill();
    }
}
