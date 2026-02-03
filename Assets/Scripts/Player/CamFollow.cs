using UnityEngine;
using DG.Tweening;

public class CamFollow : MonoBehaviour
{
    public static CamFollow Instance { get; private set; }

    Transform _target; //추적할 플레이어

    [Header("따라오기 세팅")]
    [SerializeField] float _smoothSpeed = 10f;
    [SerializeField] Vector3 _offset = new Vector3(0, 0, -10);

    [Header("줌 세팅")]
    [SerializeField] float _baseSize = 5f; //기본크기
    [SerializeField] float _zoomSensitivity = 10f; // 성장당 줌 확대 배율
    [SerializeField] float _minSize = 5f;
    [SerializeField] float _maxSize = 100f;

    Camera _cam;

    private void Awake()
    {
        Instance = this;
        _cam = GetComponent<Camera>();
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    private void FixedUpdate()
    {
        if(_target == null) return;

        //부드러운 추적
        Vector3 desiredPos = _target.position + _offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.fixedDeltaTime * _smoothSpeed);

        // 플레이어 local스케일 대신 logic스케일 참조
        float targetScale = 1f;
        if (_target.TryGetComponent<PlayerCtrl>(out var ctrl))
        {
            targetScale = ctrl.LogicScale;
        }
        else
        {
            targetScale = _target.localScale.x;
        }
        //플레이어 스케일에 따른 줌크기 변경
        float targetZoom = _baseSize + (targetScale * _zoomSensitivity);
        targetZoom = Mathf.Clamp(targetZoom, _minSize, _maxSize);

        //부드럽게 줌 조절
        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, targetZoom, Time.fixedDeltaTime * _smoothSpeed);
    }

    public void ShakeCam(float duration, float strength)
    {
        transform.DOComplete();
        transform.DOShakePosition(duration, strength, vibrato: 10, randomness: 90, fadeOut: true).SetUpdate(UpdateType.Fixed);
    }

    public void FastMove(Vector3 targetPos)
    {
        // DOTween 애니메이션이 실행 중이라면 즉시 종료 (Shake 등 간섭 방지)
        transform.DOKill();

        // Lerp 없이 즉시 목표 위치 + 오프셋으로 이동
        transform.position = targetPos + _offset;

        Debug.Log("카메라 위치 즉시 갱신 (워프 대응)");
    }
}
