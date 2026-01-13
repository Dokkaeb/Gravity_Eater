using UnityEngine;
using Photon.Pun;

public class CamFollow : MonoBehaviour
{
    Transform _target; //추적할 플레이어

    [Header("따라오기 세팅")]
    [SerializeField] float _smoothSpeed = 5f;
    [SerializeField] Vector3 _offset = new Vector3(0, 0, -10);

    [Header("줌 세팅")]
    [SerializeField] float _baseSize = 5f; //기본크기
    [SerializeField] float _zoomSensitivity = 1.2f; // 성장당 줌 확대 배율
    [SerializeField] float _minSize = 5f;
    [SerializeField] float _maxSize = 25f;

    Camera _cam;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
    }
    private void LateUpdate()
    {
        if(_target == null)
        {
            FindLocalPlayer();
            return;
        }

        //부드러운 추적
        Vector3 desiredPos = _target.position + _offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * _smoothSpeed);

        //플레이어 스케일에 따른 줌크기 변경
        float targetZoom = _baseSize + (_target.localScale.x * _zoomSensitivity);
        targetZoom = Mathf.Clamp(targetZoom, _minSize, _maxSize);

        //부드럽게 줌 조절
        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, targetZoom, Time.deltaTime * _smoothSpeed);
    }

    private void FindLocalPlayer()
    {
        //플레이어중 나 찾기
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach(GameObject p in players)
        {
            PhotonView pv = p.GetComponent<PhotonView>();
            if(pv != null && pv.IsMine)
            {
                _target = p.transform;
                break;
            }
        }
    }
}
